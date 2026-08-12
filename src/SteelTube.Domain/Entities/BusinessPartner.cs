using System;
using SteelTube.Domain.Common;

namespace SteelTube.Domain.Entities
{
    /// <summary>
    /// A company or person involved in a transaction, unifying customers
    /// and providers into a single model (SAD 12, SRS 5, ADR-011). The
    /// only mandatory field is Name (SRS 5.2).
    /// </summary>
    public sealed class BusinessPartner
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public bool IsProvider { get; private set; }
        public bool IsCustomer { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private BusinessPartner() { }

        public static BusinessPartner Create(string name, bool isProvider, bool isCustomer, DateTime utcNow)
        {
            Guard.NotNullOrWhiteSpace(name, nameof(name));
            return new BusinessPartner
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                IsProvider = isProvider,
                IsCustomer = isCustomer,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
        }

        /// <summary>
        /// Convenience factory for implicit creation from a transaction
        /// form (SRS 5.4), where only a name is typically supplied.
        /// </summary>
        public static BusinessPartner CreateMinimal(string name, DateTime utcNow) =>
            Create(name, isProvider: false, isCustomer: false, utcNow);

        public static BusinessPartner Rehydrate(Guid id, string name, bool isProvider, bool isCustomer, DateTime createdAt, DateTime updatedAt)
        {
            Guard.NotEmpty(id, nameof(id));
            Guard.NotNullOrWhiteSpace(name, nameof(name));
            return new BusinessPartner
            {
                Id = id,
                Name = name,
                IsProvider = isProvider,
                IsCustomer = isCustomer,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
        }

        public void SetRoles(bool isProvider, bool isCustomer, DateTime utcNow)
        {
            IsProvider = isProvider;
            IsCustomer = isCustomer;
            UpdatedAt = utcNow;
        }

        public void Rename(string name, DateTime utcNow)
        {
            Guard.NotNullOrWhiteSpace(name, nameof(name));
            Name = name.Trim();
            UpdatedAt = utcNow;
        }
    }
}