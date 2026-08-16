using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;

namespace SteelTube.Application.Synchronization.Export
{
    /// <summary>SAD 30-32: turns the local operation ledger into a JSON package another device can import.</summary>
    public sealed class ExportOperationsCommandHandler
    {
        private readonly IInventoryOperationRepository _operations;
        private readonly ITubeSpecificationRepository _specifications;
        private readonly IBusinessPartnerRepository _partners;
        private readonly IDeviceContext _device;
        private readonly ISynchronizationSerializer _serializer;
        private readonly Common.IClock _clock;

        public ExportOperationsCommandHandler(
            IInventoryOperationRepository operations, ITubeSpecificationRepository specifications,
            IBusinessPartnerRepository partners, IDeviceContext device, ISynchronizationSerializer serializer,
            Common.IClock clock)
        {
            _operations = operations;
            _specifications = specifications;
            _partners = partners;
            _device = device;
            _serializer = serializer;
            _clock = clock;
        }

        public async Task<ExportOperationsResult> HandleAsync(ExportOperationsCommand command, CancellationToken ct = default)
        {
            var allOperations = await _operations.GetAllAsync(ct);

            var specifications = (await _specifications.GetAllAsync(ct)).ToDictionary(s => s.Id);
            var partners = (await _partners.GetAllAsync(ct)).ToDictionary(p => p.Id);

            var dtos = new List<SynchronizedOperationDto>(allOperations.Count);
            foreach (var operation in allOperations)
            {
                if (!specifications.TryGetValue(operation.TubeSpecificationId, out var specification))
                    continue; // Should not happen (FK-enforced) -- defensive skip rather than a hard failure on export.

                string partnerName = null;
                if (operation.BusinessPartnerId != null && partners.TryGetValue(operation.BusinessPartnerId.Value, out var partner))
                    partnerName = partner.Name;

                dtos.Add(new SynchronizedOperationDto
                {
                    OperationId = operation.OperationId,
                    OriginDeviceId = operation.OriginDeviceId,
                    OriginSequenceNumber = operation.OriginSequenceNumber,
                    OperationType = operation.OperationType.ToString(),
                    DiameterMm = specification.Diameter.Millimeters,
                    ThicknessMm = specification.Thickness.Millimeters,
                    LengthMeters = operation.Length.Meters,
                    WeightKilograms = operation.Weight?.Kilograms,
                    WeightPerMeterUsed = operation.WeightPerMeterUsed?.Value,
                    PieceCount = operation.PieceCount,
                    BusinessPartnerName = partnerName,
                    OperationDate = operation.OperationDate,
                    InsertedAt = operation.InsertedAt,
                    Note = operation.Note
                });
            }

            var package = new Synchronization.SynchronizationPackage
            {
                PackageId = Guid.NewGuid(),
                SourceDeviceId = _device.DeviceId,
                SourceDeviceName = _device.DeviceName,
                CreatedAtUtc = _clock.UtcNow,
                Operations = dtos
            };

            var json = _serializer.Serialize(package);
            var safeDeviceName = string.Concat(_device.DeviceName.Split(System.IO.Path.GetInvalidFileNameChars()));
            var fileName = $"SteelTube-Sync-{safeDeviceName}-{package.CreatedAtUtc:yyyyMMdd-HHmmss}.json";

            return new ExportOperationsResult
            {
                PackageJson = json,
                SuggestedFileName = fileName,
                PackageId = package.PackageId,
                OperationCount = dtos.Count
            };
        }
    }
}