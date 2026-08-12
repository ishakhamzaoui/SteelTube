using System;
using SteelTube.Domain.Common;

namespace SteelTube.Domain.Entities
{
    /// <summary>
    /// A materialized "current stock" projection per tube specification
    /// (SAD 21). This is a performance optimization only — it must always
    /// be rebuildable from the full InventoryOperation history (SAD 22),
    /// and it is intentionally allowed to go negative so that offline
    /// overselling can be detected and surfaced rather than hidden
    /// (SAD 36, SAD 37, SRS 14.2).
    /// </summary>
    public sealed class InventoryBalance
    {
        public Guid TubeSpecificationId { get; private set; }
        public decimal QuantityLengthMeters { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private InventoryBalance() { }

        public static InventoryBalance Create(Guid tubeSpecificationId, DateTime utcNow)
        {
            Guard.NotEmpty(tubeSpecificationId, nameof(tubeSpecificationId));
            return new InventoryBalance
            {
                TubeSpecificationId = tubeSpecificationId,
                QuantityLengthMeters = 0m,
                UpdatedAt = utcNow
            };
        }

        public static InventoryBalance Rehydrate(Guid tubeSpecificationId, decimal quantityLengthMeters, DateTime updatedAt)
        {
            Guard.NotEmpty(tubeSpecificationId, nameof(tubeSpecificationId));
            return new InventoryBalance
            {
                TubeSpecificationId = tubeSpecificationId,
                QuantityLengthMeters = quantityLengthMeters,
                UpdatedAt = updatedAt
            };
        }

        /// <summary>Applies a single operation's signed length (SAD 20, SAD 24).</summary>
        public void Apply(decimal signedLengthMeters, DateTime utcNow)
        {
            QuantityLengthMeters += signedLengthMeters;
            UpdatedAt = utcNow;
        }

        public bool IsNegative => QuantityLengthMeters < 0m;
    }
}