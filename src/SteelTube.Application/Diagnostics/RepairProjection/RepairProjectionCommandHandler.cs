using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;

namespace SteelTube.Application.Diagnostics.RepairProjection
{
    /// <summary>
    /// The corrective action for a <see cref="CheckIntegrity.ProjectionMismatch"/>
    /// list: delete and recompute every balance from the operation ledger
    /// (SAD 22). The operation ledger itself is never touched -- this can
    /// only ever make the projection agree with history, never the other
    /// way around (SAD 3.5: history is never rewritten).
    /// </summary>
    public sealed class RepairProjectionCommandHandler
    {
        private readonly IInventoryBalanceRepository _balances;
        private readonly IUnitOfWork _unitOfWork;

        public RepairProjectionCommandHandler(IInventoryBalanceRepository balances, IUnitOfWork unitOfWork)
        {
            _balances = balances;
            _unitOfWork = unitOfWork;
        }

        public Task HandleAsync(RepairProjectionCommand command, CancellationToken ct = default) =>
            _unitOfWork.ExecuteInTransactionAsync(txCt => _balances.RebuildAllAsync(txCt), ct);
    }
}