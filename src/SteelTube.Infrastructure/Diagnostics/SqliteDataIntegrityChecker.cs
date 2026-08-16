using System;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Infrastructure.Persistence;

namespace SteelTube.Infrastructure.Diagnostics
{
    /// <inheritdoc cref="IDataIntegrityChecker"/>
    public sealed class SqliteDataIntegrityChecker : IDataIntegrityChecker
    {
        private readonly ISqliteConnectionProvider _provider;

        public SqliteDataIntegrityChecker(ISqliteConnectionProvider provider)
        {
            _provider = provider;
        }

        public async Task<bool> CheckSqliteIntegrityAsync(CancellationToken ct = default)
        {
            using (var command = CreateCommand("PRAGMA integrity_check;"))
            {
                var result = (string)await command.ExecuteScalarAsync(ct);
                return string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
            }
        }

        public async Task<bool> CheckForeignKeyIntegrityAsync(CancellationToken ct = default)
        {
            using (var command = CreateCommand("PRAGMA foreign_key_check;"))
            using (var reader = await command.ExecuteReaderAsync(ct))
            {
                // Any returned row describes a violation -- no rows means everything is consistent.
                return !await reader.ReadAsync(ct);
            }
        }

        private Microsoft.Data.Sqlite.SqliteCommand CreateCommand(string sql)
        {
            var command = _provider.Connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _provider.CurrentTransaction;
            return command;
        }
    }
}