using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteelTube.Infrastructure.Persistence
{
    /// <summary>
    /// Maps a use case's atomic unit of work directly onto a single SQLite
    /// transaction (SAD 24, SAD 26): "the application must never leave a
    /// state where the operation history and current stock disagree."
    ///
    /// Calls can nest (e.g. a future use case that composes two existing
    /// handlers): if a transaction is already active on the session, this
    /// simply joins it instead of starting a second one.
    /// </summary>
    public sealed class SqliteUnitOfWork : SteelTube.Application.Abstractions.IUnitOfWork
    {
        private readonly SqliteSession _session;

        public SqliteUnitOfWork(SqliteSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        {
            await ExecuteInTransactionAsync(async innerCt =>
            {
                await action(innerCt);
                return true;
            }, ct);
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
        {
            if (_session.CurrentTransaction != null)
            {
                // Already inside a unit of work: join it (SAD 24 applies to
                // the outermost caller; nested calls must not commit early).
                return await action(ct);
            }

            using (var transaction = _session.Connection.BeginTransaction())
            {
                _session.CurrentTransaction = transaction;
                try
                {
                    var result = await action(ct);
                    transaction.Commit();
                    return result;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    _session.CurrentTransaction = null;
                }
            }
        }
    }
}