using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Domain.Entities;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// Persistence abstraction for business partners (SAD 25, SRS 5).
    /// </summary>
    public interface IBusinessPartnerRepository
    {
        Task<BusinessPartner> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<BusinessPartner> FindByNameAsync(string name, CancellationToken ct = default);

        /// <summary>
        /// Supports implicit partner creation directly from a transaction
        /// form (SRS 5.4): if a typed name does not match an existing
        /// partner, one is created using only that name.
        /// </summary>
        Task<BusinessPartner> GetOrCreateByNameAsync(string name, DateTime utcNow, CancellationToken ct = default);

        Task AddAsync(BusinessPartner partner, CancellationToken ct = default);

        Task<IReadOnlyList<BusinessPartner>> GetAllAsync(CancellationToken ct = default);
    }
}