using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// Coordinates atomic persistence across repositories, mapping to a
    /// single SQLite transaction (SAD 24, SAD 26). Every stock-changing
    /// command must run through this so the operation ledger and the
    /// InventoryBalance projection never disagree (SAD 24: "the
    /// application must never leave a state where the operation history
    /// and current stock disagree").
    /// </summary>
    public interface IUnitOfWork
    {
        Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);
    }
}