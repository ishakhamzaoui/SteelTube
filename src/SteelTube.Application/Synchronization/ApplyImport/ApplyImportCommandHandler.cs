using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Application.Common;
using SteelTube.Domain.Entities;
using SteelTube.Domain.Enums;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Synchronization.ApplyImport
{
    /// <summary>
    /// Implements the Import Transaction from SAD 39 and the Merge
    /// Algorithm from SAD 33: for each incoming operation, skip if its
    /// OperationId is already known (SAD 35 idempotency), otherwise
    /// resolve/create the local TubeSpecification and BusinessPartner by
    /// their business identity and insert the operation with its original
    /// identity preserved. The whole package is one transaction (SAD 72:
    /// "atomic rejection of the entire package is preferable to partially
    /// applying") -- any failure rolls back everything, leaving the local
    /// database exactly as it was (SAD 39).
    /// </summary>
    public sealed class ApplyImportCommandHandler
    {
        private readonly ISynchronizationSerializer _serializer;
        private readonly IInventoryOperationRepository _operations;
        private readonly ITubeSpecificationRepository _specifications;
        private readonly IBusinessPartnerRepository _partners;
        private readonly IInventoryBalanceRepository _balances;
        private readonly IBackupService _backupService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public ApplyImportCommandHandler(
            ISynchronizationSerializer serializer, IInventoryOperationRepository operations,
            ITubeSpecificationRepository specifications, IBusinessPartnerRepository partners,
            IInventoryBalanceRepository balances, IBackupService backupService, IUnitOfWork unitOfWork, IClock clock)
        {
            _serializer = serializer;
            _operations = operations;
            _specifications = specifications;
            _partners = partners;
            _balances = balances;
            _backupService = backupService;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<ApplyImportResult> HandleAsync(ApplyImportCommand command, CancellationToken ct = default)
        {
            var package = _serializer.Deserialize(command.PackageJson);

            if (package.FormatVersion != SynchronizationPackage.CurrentFormatVersion)
                throw new SynchronizationException(
                    $"This synchronization file was created by an unsupported version of SteelTube (format {package.FormatVersion}).");

            // SAD 41 -- safety backup happens BEFORE the import transaction,
            // as its own file-level operation, so a bad import can always
            // be undone even though the SQL transaction below already
            // guarantees a clean rollback on failure.
            await _backupService.CreateBackupAsync(ct: ct);

            return await _unitOfWork.ExecuteInTransactionAsync(txCt => ApplyAsync(package, txCt), ct);
        }

        private async Task<ApplyImportResult> ApplyAsync(SynchronizationPackage package, CancellationToken ct)
        {
            var utcNow = _clock.UtcNow;
            var newCount = 0;
            var skipCount = 0;
            var touchedSpecifications = new Dictionary<Guid, TubeSpecification>();

            foreach (var dto in package.Operations)
            {
                if (await _operations.ExistsAsync(dto.OperationId, ct))
                {
                    skipCount++;
                    continue;
                }

                if (!Enum.TryParse(dto.OperationType, out OperationType operationType))
                    throw new SynchronizationException($"Unrecognized operation type \"{dto.OperationType}\" in the synchronization file.");

                var diameter = Diameter.FromMillimeters(dto.DiameterMm);
                var thickness = Thickness.FromMillimeters(dto.ThicknessMm);
                var specification = await _specifications.GetOrCreateAsync(diameter, thickness, utcNow, ct);
                touchedSpecifications[specification.Id] = specification;

                Guid? partnerId = null;
                if (!string.IsNullOrWhiteSpace(dto.BusinessPartnerName))
                {
                    var partner = await _partners.GetOrCreateByNameAsync(dto.BusinessPartnerName, utcNow, ct);
                    partnerId = partner.Id;
                }

                var operation = InventoryOperation.Rehydrate(
                    dto.OperationId,
                    operationType,
                    specification.Id,
                    Length.FromMeters(dto.LengthMeters),
                    dto.WeightKilograms is decimal w ? Weight.FromKilograms(w) : (Weight?)null,
                    dto.WeightPerMeterUsed is decimal k ? KgPerMeter.FromValue(k) : (KgPerMeter?)null,
                    dto.PieceCount,
                    partnerId,
                    dto.OperationDate,
                    dto.InsertedAt,
                    dto.OriginDeviceId,
                    dto.OriginSequenceNumber,
                    dto.Note);

                await _operations.AddAsync(operation, ct);

                var balance = await _balances.GetAsync(specification.Id, ct) ?? InventoryBalance.Create(specification.Id, utcNow);
                balance.Apply(operation.SignedLengthMeters, utcNow);
                await _balances.UpsertAsync(balance, ct);

                newCount++;
            }

            // SAD 38 -- after merging, check every material this import touched for a discrepancy.
            var warnings = new List<NegativeStockWarning>();
            foreach (var specification in touchedSpecifications.Values)
            {
                var finalBalance = await _balances.GetAsync(specification.Id, ct);
                if (finalBalance != null && finalBalance.IsNegative)
                {
                    warnings.Add(new NegativeStockWarning
                    {
                        DiameterMm = specification.Diameter.Millimeters,
                        ThicknessMm = specification.Thickness.Millimeters,
                        ResultingQuantityLengthMeters = finalBalance.QuantityLengthMeters
                    });
                }
            }

            return new ApplyImportResult
            {
                NewOperationsInserted = newCount,
                AlreadyKnownSkipped = skipCount,
                NegativeStockWarnings = warnings
            };
        }
    }
}