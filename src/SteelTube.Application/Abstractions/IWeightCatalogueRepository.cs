using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Domain.Entities;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// Persistence abstraction for the weight catalogue (SAD 25, SRS 8).
    /// </summary>
    public interface IWeightCatalogueRepository
    {
        Task<WeightCatalogueEntry> FindAsync(Diameter diameter, Thickness thickness, CancellationToken ct = default);

        Task AddAsync(WeightCatalogueEntry entry, CancellationToken ct = default);

        Task UpdateAsync(WeightCatalogueEntry entry, CancellationToken ct = default);

        Task<IReadOnlyList<WeightCatalogueEntry>> GetAllAsync(CancellationToken ct = default);
    }
}