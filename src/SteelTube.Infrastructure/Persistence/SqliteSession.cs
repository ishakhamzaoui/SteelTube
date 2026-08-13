using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace SteelTube.Infrastructure.Persistence
{
    /// <summary>
    /// Owns the single SQLite connection used for the lifetime of the
    /// application (SAD 23 — "the database shall be stored locally with the
    /// application data"). A desktop, single-user, offline application does
    /// not need a connection pool; one long-lived connection kept open is
    /// simpler and lighter on the constrained hardware target (SAD 58).
    /// </summary>
    public sealed class SqliteSession : ISqliteConnectionProvider, IDisposable
    {
        public string DatabasePath { get; }
        public SqliteConnection Connection { get; }

        /// <summary>Set only while <see cref="SqliteUnitOfWork"/> has an active transaction.</summary>
        public SqliteTransaction CurrentTransaction { get; set; }

        public SqliteSession(string databasePath)
        {
            DatabasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));

            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Default
            }.ToString();

            Connection = new SqliteConnection(connectionString);
            Connection.Open();

            using (var pragma = Connection.CreateCommand())
            {
                // WAL improves concurrent read/write behavior for very
                // little extra resource cost (SAD 58); foreign keys are off
                // by default in SQLite and must be turned on per connection.
                pragma.CommandText = "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }
        }

        public void Dispose() => Connection.Dispose();
    }
}