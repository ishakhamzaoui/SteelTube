using System;
using SteelTube.Domain.Common;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Domain.Entities
{
    /// <summary>
    /// Maps a tube specification (Diameter + Thickness) to a mass-per-length
    /// conversion factor (SAD 13, SRS 8). Uniqueness of Diameter + Thickness
    /// is enforced by a database constraint (SAD 13.2, SRS 8.3).
    /// </summary>
    public sealed class WeightCatalogueEntry
    {
        public Guid Id { get; private set; }
        public Diameter Diameter { get; private set; }
        public Thickness Thickness { get; private set; }
        public KgPerMeter KgPerMeter { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private WeightCatalogueEntry() { }

        public static WeightCatalogueEntry Create(Diameter diameter, Thickness thickness, KgPerMeter kgPerMeter, DateTime utcNow)
        {
            return new WeightCatalogueEntry
            {
                Id = Guid.NewGuid(),
                Diameter = diameter,
                Thickness = thickness,
                KgPerMeter = kgPerMeter,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
        }

        public static WeightCatalogueEntry Rehydrate(Guid id, Diameter diameter, Thickness thickness, KgPerMeter kgPerMeter, DateTime createdAt, DateTime updatedAt)
        {
            Guard.NotEmpty(id, nameof(id));
            return new WeightCatalogueEntry
            {
                Id = id,
                Diameter = diameter,
                Thickness = thickness,
                KgPerMeter = kgPerMeter,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
        }

        /// <summary>
        /// Updates the conversion factor going forward. This must never
        /// rewrite historical operations (SAD 17, SRS 8.4) — those keep
        /// their own WeightPerMeterUsed snapshot.
        /// </summary>
        public void UpdateFactor(KgPerMeter kgPerMeter, DateTime utcNow)
        {
            KgPerMeter = kgPerMeter;
            UpdatedAt = utcNow;
        }
    }
}