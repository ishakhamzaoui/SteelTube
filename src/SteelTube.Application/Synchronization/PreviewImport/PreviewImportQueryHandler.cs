using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Application.Common;
using SteelTube.Domain.Entities;
using SteelTube.Domain.Enums;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Synchronization.PreviewImport
{
    /// <summary>
    /// Implements SAD 40 / SAD 41's "Preview" step. Strictly read-only: it
    /// never calls a GetOrCreate method and never persists anything, only
    /// Find (SAD 41 diagram runs Preview *before* Apply, and the two must
    /// stay independent so looking at a preview can never itself change
    /// the database).
    /// </summary>
    public sealed class PreviewImportQueryHandler
    {
        private readonly ISynchronizationSerializer _serializer;
        private readonly IInventoryOperationRepository _operations;
        private readonly ITubeSpecificationRepository _specifications;
        private readonly IInventoryBalanceRepository _balances;

        public PreviewImportQueryHandler(
            ISynchronizationSerializer serializer, IInventoryOperationRepository operations,
            ITubeSpecificationRepository specifications, IInventoryBalanceRepository balances)
        {
            _serializer = serializer;
            _operations = operations;
            _specifications = specifications;
            _balances = balances;
        }

        public async Task<PreviewImportResult> HandleAsync(PreviewImportQuery query, CancellationToken ct = default)
        {
            var package = _serializer.Deserialize(query.PackageJson);

            if (package.FormatVersion != SynchronizationPackage.CurrentFormatVersion)
                throw new SynchronizationException(
                    $"This synchronization file was created by an unsupported version of SteelTube (format {package.FormatVersion}).");

            var deltasByMaterial = new Dictionary<(decimal Diameter, decimal Thickness), decimal>();
            var newCount = 0;
            var knownCount = 0;

            foreach (var dto in package.Operations)
            {
                if (await _operations.ExistsAsync(dto.OperationId, ct))
                {
                    knownCount++;
                    continue;
                }

                newCount++;
                var key = (dto.DiameterMm, dto.ThicknessMm);
                var signedLength = SignedLengthOf(dto);
                deltasByMaterial[key] = deltasByMaterial.TryGetValue(key, out var existing) ? existing + signedLength : signedLength;
            }

            var affected = new List<AffectedMaterialPreview>();
            foreach (var kvp in deltasByMaterial)
            {
                var (diameterMm, thicknessMm) = kvp.Key;
                var delta = kvp.Value;

                var specification = await _specifications.FindAsync(
                    Diameter.FromMillimeters(diameterMm), Thickness.FromMillimeters(thicknessMm), ct);

                decimal currentQuantity = 0m;
                if (specification != null)
                {
                    var balance = await _balances.GetAsync(specification.Id, ct);
                    currentQuantity = balance?.QuantityLengthMeters ?? 0m;
                }

                var resulting = currentQuantity + delta;
                affected.Add(new AffectedMaterialPreview
                {
                    DiameterMm = diameterMm,
                    ThicknessMm = thicknessMm,
                    CurrentQuantityLengthMeters = currentQuantity,
                    DeltaLengthMeters = delta,
                    ResultingQuantityLengthMeters = resulting,
                    WouldResultInNegativeStock = resulting < 0m
                });
            }

            return new PreviewImportResult
            {
                PackageId = package.PackageId,
                SourceDeviceId = package.SourceDeviceId,
                SourceDeviceName = package.SourceDeviceName,
                CreatedAtUtc = package.CreatedAtUtc,
                NewOperationsCount = newCount,
                AlreadyKnownCount = knownCount,
                AffectedMaterials = affected
            };
        }

        /// <summary>
        /// Reuses InventoryOperation.SignedLengthMeters (SAD 3.6, SAD 67)
        /// via a throwaway in-memory entity rather than re-encoding the
        /// Purchase/Sale/Adjustment sign convention here. Rehydrate has no
        /// side effects, so this stays read-only.
        /// </summary>
        private static decimal SignedLengthOf(SynchronizedOperationDto dto)
        {
            if (!Enum.TryParse(dto.OperationType, out OperationType operationType))
                throw new SynchronizationException($"Unrecognized operation type \"{dto.OperationType}\" in the synchronization file.");

            var temp = InventoryOperation.Rehydrate(
                dto.OperationId, operationType, Guid.NewGuid(), Length.FromMeters(dto.LengthMeters),
                dto.WeightKilograms is decimal w ? Weight.FromKilograms(w) : (Weight?)null,
                dto.WeightPerMeterUsed is decimal k ? KgPerMeter.FromValue(k) : (KgPerMeter?)null,
                dto.PieceCount, null, dto.OperationDate, dto.InsertedAt, dto.OriginDeviceId, dto.OriginSequenceNumber, dto.Note);

            return temp.SignedLengthMeters;
        }
    }
}