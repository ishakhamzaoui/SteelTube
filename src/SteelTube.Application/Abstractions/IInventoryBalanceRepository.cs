using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Domain.Entities;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// Persistence abstraction for the materialized current-stock
    /// projection (SAD 21, SAD 25). Must always be rebuildable from
    /// InventoryOperation (SAD 22) — it is never the source of truth.
    /// </summary>
    public interface IInventoryBalanceRepository
    {
        Task<InventoryBalance> GetAsync(Guid tubeSpecificationId, CancellationToken ct = default);

        Task UpsertAsync(InventoryBalance balance, CancellationToken ct = default);

        Task<IReadOnlyList<InventoryBalance>> GetAllAsync(CancellationToken ct = default);

        /// <summary>Deletes and recomputes every balance from the operation ledger (SAD 22).</summary>
        Task RebuildAllAsync(CancellationToken ct = default);
    }
}