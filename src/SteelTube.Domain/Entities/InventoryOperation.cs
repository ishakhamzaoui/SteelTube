using System;
using SteelTube.Domain.Common;
using SteelTube.Domain.Enums;
using SteelTube.Domain.Exceptions;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Domain.Entities
{
    /// <summary>
    /// A single immutable event that changes inventory (SAD 10, SRS 6,
    /// ADR-007). Operations are the source of truth: current stock is a
    /// derived projection (SAD 3.3). Once committed, an operation is never
    /// edited in place — corrections are new operations (SAD 3.5, SRS
    /// Rule 9).
    ///
    /// The Length quantity is always positive; direction comes from
    /// OperationType via <see cref="SignedLengthMeters"/> (SAD 67).
    /// </summary>
    public sealed class InventoryOperation
    {
        public Guid OperationId { get; private set; }
        public OperationType OperationType { get; private set; }
        public Guid TubeSpecificationId { get; private set; }
        public Length Length { get; private set; }

        /// <summary>Optional — set when the transaction was conducted or displayed by weight (SRS 7).</summary>
        public Weight? Weight { get; private set; }

        /// <summary>
        /// Historical snapshot of the kg/m value used for this operation's
        /// conversion, if any (SAD 17, ADR-010). Not a replacement for the
        /// live catalogue — it exists purely so the operation remains
        /// reproducible even if the catalogue changes later.
        /// </summary>
        public KgPerMeter? WeightPerMeterUsed { get; private set; }

        /// <summary>Optional historical metadata only — never used in stock calculations (SRS 3.2, SRS Rule 3/4).</summary>
        public int? PieceCount { get; private set; }

        public Guid? BusinessPartnerId { get; private set; }
        public DateTime OperationDate { get; private set; }
        public DateTime InsertedAt { get; private set; }
        public Guid OriginDeviceId { get; private set; }
        public long OriginSequenceNumber { get; private set; }
        public string Note { get; private set; }

        private InventoryOperation() { }

        /// <summary>
        /// Signed length applied to the inventory projection (SAD 67):
        /// Purchase / AdjustmentIncrease are positive; Sale /
        /// AdjustmentDecrease are negative.
        /// </summary>
        public decimal SignedLengthMeters
        {
            get
            {
                switch (OperationType)
                {
                    case OperationType.Purchase:
                    case OperationType.AdjustmentIncrease:
                        return Length.Meters;
                    case OperationType.Sale:
                    case OperationType.AdjustmentDecrease:
                        return -Length.Meters;
                    default:
                        throw new DomainValidationException($"Unknown operation type '{OperationType}'.");
                }
            }
        }

        public static InventoryOperation CreatePurchase(
            Guid tubeSpecificationId, Length length, Weight? weight, KgPerMeter? weightPerMeterUsed,
            int? pieceCount, Guid? businessPartnerId, DateTime operationDate, DateTime insertedAt,
            Guid originDeviceId, long originSequenceNumber, string note) =>
            Create(OperationType.Purchase, tubeSpecificationId, length, weight, weightPerMeterUsed,
                pieceCount, businessPartnerId, operationDate, insertedAt, originDeviceId, originSequenceNumber, note);

        public static InventoryOperation CreateSale(
            Guid tubeSpecificationId, Length length, Weight? weight, KgPerMeter? weightPerMeterUsed,
            int? pieceCount, Guid? businessPartnerId, DateTime operationDate, DateTime insertedAt,
            Guid originDeviceId, long originSequenceNumber, string note) =>
            Create(OperationType.Sale, tubeSpecificationId, length, weight, weightPerMeterUsed,
                pieceCount, businessPartnerId, operationDate, insertedAt, originDeviceId, originSequenceNumber, note);

        /// <summary>Adjustments should always carry an explanatory note (SRS 4.4).</summary>
        public static InventoryOperation CreateAdjustmentIncrease(
            Guid tubeSpecificationId, Length length, int? pieceCount, Guid? businessPartnerId,
            DateTime operationDate, DateTime insertedAt, Guid originDeviceId, long originSequenceNumber, string note)
        {
            Guard.NotNullOrWhiteSpace(note, nameof(note));
            return Create(OperationType.AdjustmentIncrease, tubeSpecificationId, length, null, null,
                pieceCount, businessPartnerId, operationDate, insertedAt, originDeviceId, originSequenceNumber, note);
        }

        public static InventoryOperation CreateAdjustmentDecrease(
            Guid tubeSpecificationId, Length length, int? pieceCount, Guid? businessPartnerId,
            DateTime operationDate, DateTime insertedAt, Guid originDeviceId, long originSequenceNumber, string note)
        {
            Guard.NotNullOrWhiteSpace(note, nameof(note));
            return Create(OperationType.AdjustmentDecrease, tubeSpecificationId, length, null, null,
                pieceCount, businessPartnerId, operationDate, insertedAt, originDeviceId, originSequenceNumber, note);
        }

        private static InventoryOperation Create(
            OperationType operationType, Guid tubeSpecificationId, Length length, Weight? weight,
            KgPerMeter? weightPerMeterUsed, int? pieceCount, Guid? businessPartnerId, DateTime operationDate,
            DateTime insertedAt, Guid originDeviceId, long originSequenceNumber, string note)
        {
            Guard.NotEmpty(tubeSpecificationId, nameof(tubeSpecificationId));
            Guard.NotEmpty(originDeviceId, nameof(originDeviceId));
            if (pieceCount.HasValue)
                Guard.NotNegative(pieceCount.Value, nameof(pieceCount));

            return new InventoryOperation
            {
                OperationId = Guid.NewGuid(),
                OperationType = operationType,
                TubeSpecificationId = tubeSpecificationId,
                Length = length,
                Weight = weight,
                WeightPerMeterUsed = weightPerMeterUsed,
                PieceCount = pieceCount,
                BusinessPartnerId = businessPartnerId,
                OperationDate = operationDate,
                InsertedAt = insertedAt,
                OriginDeviceId = originDeviceId,
                OriginSequenceNumber = originSequenceNumber,
                Note = note
            };
        }

        /// <summary>
        /// Reconstructs an operation exactly as it was persisted or as it
        /// arrived in a synchronization package (SAD 27, SAD 33).
        /// Infrastructure layer only — this bypasses factory validation of
        /// "new" business rules such as mandatory adjustment notes, because
        /// an already-committed operation must be re-creatable verbatim
        /// regardless of rules that may have evolved since it was written.
        /// </summary>
        public static InventoryOperation Rehydrate(
            Guid operationId, OperationType operationType, Guid tubeSpecificationId, Length length,
            Weight? weight, KgPerMeter? weightPerMeterUsed, int? pieceCount, Guid? businessPartnerId,
            DateTime operationDate, DateTime insertedAt, Guid originDeviceId, long originSequenceNumber, string note)
        {
            Guard.NotEmpty(operationId, nameof(operationId));
            Guard.NotEmpty(tubeSpecificationId, nameof(tubeSpecificationId));
            Guard.NotEmpty(originDeviceId, nameof(originDeviceId));

            return new InventoryOperation
            {
                OperationId = operationId,
                OperationType = operationType,
                TubeSpecificationId = tubeSpecificationId,
                Length = length,
                Weight = weight,
                WeightPerMeterUsed = weightPerMeterUsed,
                PieceCount = pieceCount,
                BusinessPartnerId = businessPartnerId,
                OperationDate = operationDate,
                InsertedAt = insertedAt,
                OriginDeviceId = originDeviceId,
                OriginSequenceNumber = originSequenceNumber,
                Note = note
            };
        }
    }
}