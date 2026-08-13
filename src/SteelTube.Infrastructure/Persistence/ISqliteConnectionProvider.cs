using Microsoft.Data.Sqlite;

namespace SteelTube.Infrastructure.Persistence
{
    /// <summary>
    /// Gives repositories access to the app's single SQLite connection and,
    /// when one is active, the ambient transaction started by
    /// <see cref="SqliteUnitOfWork"/>. Every repository command attaches
    /// <see cref="CurrentTransaction"/> so the whole unit of work commits or
    /// rolls back together (SAD 24, SAD 26).
    /// </summary>
    public interface ISqliteConnectionProvider
    {
        SqliteConnection Connection { get; }

        SqliteTransaction CurrentTransaction { get; }
    }
}