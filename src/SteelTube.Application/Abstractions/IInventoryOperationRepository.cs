using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Domain.Entities;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// Persistence abstraction for the operation ledger (SAD 25). This is
    /// the append-only source of truth (SAD 3.3) — there is deliberately no
    /// Update or Delete method here (SAD 3.5, SRS Rule 9).
    /// </summary>
    public interface IInventoryOperationRepository
    {
        /// <summary>Used by the synchronization merge algorithm for duplicate detection (SAD 33, SRS 12.4).</summary>
        Task<bool> ExistsAsync(Guid operationId, CancellationToken ct = default);

        Task AddAsync(InventoryOperation operation, CancellationToken ct = default);

        Task<IReadOnlyList<InventoryOperation>> GetHistoryAsync(InventoryOperationFilter filter, CancellationToken ct = default);

        /// <summary>All operations local to this device, for full or incremental export (SAD 29, SRS 12.7).</summary>
        Task<IReadOnlyList<InventoryOperation>> GetByOriginDeviceAfterSequenceAsync(Guid deviceId, long afterSequenceNumber, CancellationToken ct = default);

        /// <summary>Used to rebuild InventoryBalance from scratch (SAD 22).</summary>
        Task<IReadOnlyList<InventoryOperation>> GetAllForTubeSpecificationAsync(Guid tubeSpecificationId, CancellationToken ct = default);
    }

    /// <summary>Filter for SAD 59 indexed history queries / SRS 10.3.</summary>
    public sealed class InventoryOperationFilter
    {
        public Guid? TubeSpecificationId { get; set; }
        public Guid? BusinessPartnerId { get; set; }
        public Domain.Enums.OperationType? OperationType { get; set; }
        public DateTime? OperationDateFrom { get; set; }
        public DateTime? OperationDateTo { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; } = 200;
    }
}