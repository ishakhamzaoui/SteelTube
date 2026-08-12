using System;
using SteelTube.Domain.Common;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Domain.Entities
{
    /// <summary>
    /// The inventory material identity (SAD 9, SRS 2.1). Identity is
    /// Diameter + Thickness only — Length is deliberately excluded
    /// (SAD 9.3): physical tubes of the same specification are mixed
    /// together in varying lengths, so length cannot be a reliable
    /// inventory classification (ADR-004).
    /// </summary>
    public sealed class TubeSpecification
    {
        public Guid Id { get; private set; }
        public Diameter Diameter { get; private set; }
        public Thickness Thickness { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        // EF/serialization-friendly parameterless constructor.
        private TubeSpecification() { }

        /// <summary>
        /// Creates a brand-new specification. Uniqueness of
        /// Diameter + Thickness is enforced by a database constraint
        /// (SAD 9.2) and should be checked by the repository/use case
        /// before calling this (see IWeightCatalogueRepository /
        /// ITubeSpecificationRepository.GetOrCreateAsync).
        /// </summary>
        public static TubeSpecification Create(Diameter diameter, Thickness thickness, DateTime utcNow)
        {
            return new TubeSpecification
            {
                Id = Guid.NewGuid(),
                Diameter = diameter,
                Thickness = thickness,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
        }

        /// <summary>
        /// Reconstructs an existing specification from persistence.
        /// Infrastructure layer only — do not use for new specifications.
        /// </summary>
        public static TubeSpecification Rehydrate(Guid id, Diameter diameter, Thickness thickness, DateTime createdAt, DateTime updatedAt)
        {
            Guard.NotEmpty(id, nameof(id));
            return new TubeSpecification
            {
                Id = id,
                Diameter = diameter,
                Thickness = thickness,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
        }

        public string DisplayName => $"{Diameter.Millimeters:0.##} \u00d7 {Thickness.Millimeters:0.##} mm";
    }
}