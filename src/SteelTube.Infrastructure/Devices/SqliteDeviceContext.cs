using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SteelTube.Application.Abstractions;
using SteelTube.Infrastructure.Persistence;

namespace SteelTube.Infrastructure.Devices
{
    /// <summary>
    /// Persists the single device identity for this installation (SAD 28)
    /// and the local operation sequence counter (SAD 29). The Devices table
    /// holds exactly one row per installation; DeviceId is generated once,
    /// the first time the app runs against a fresh database, and never
    /// regenerated afterwards.
    /// </summary>
    public sealed class SqliteDeviceContext : IDeviceContext
    {
        private readonly ISqliteConnectionProvider _provider;

        public Guid DeviceId { get; }

        private SqliteDeviceContext(ISqliteConnectionProvider provider, Guid deviceId)
        {
            _provider = provider;
            DeviceId = deviceId;
        }

        /// <summary>
        /// Loads the existing device row, or creates one (name defaults to
        /// the machine name; can be changed later via Settings, SAD 17).
        /// Call this once during app startup, before building any use case
        /// handlers.
        /// </summary>
        public static async Task<SqliteDeviceContext> CreateAsync(ISqliteConnectionProvider provider, CancellationToken ct = default)
        {
            using (var select = provider.Connection.CreateCommand())
            {
                select.Transaction = provider.CurrentTransaction;
                select.CommandText = "SELECT Id FROM Devices LIMIT 1;";
                var existing = await select.ExecuteScalarAsync(ct);
                if (existing != null)
                    return new SqliteDeviceContext(provider, SqlConvert.ToGuid(existing));
            }

            var deviceId = Guid.NewGuid();
            using (var insert = provider.Connection.CreateCommand())
            {
                insert.Transaction = provider.CurrentTransaction;
                insert.CommandText =
                    "INSERT INTO Devices (Id, Name, CreatedAt, LastSequenceNumber) VALUES ($id, $name, $createdAt, 0);";
                insert.Parameters.AddWithValue("$id", SqlConvert.ToText(deviceId));
                insert.Parameters.AddWithValue("$name", Environment.MachineName);
                insert.Parameters.AddWithValue("$createdAt", SqlConvert.ToText(DateTime.UtcNow));
                await insert.ExecuteNonQueryAsync(ct);
            }

            return new SqliteDeviceContext(provider, deviceId);
        }

        public async Task<long> NextSequenceNumberAsync(CancellationToken ct = default)
        {
            using (var update = _provider.Connection.CreateCommand())
            {
                update.Transaction = _provider.CurrentTransaction;
                update.CommandText =
                    "UPDATE Devices SET LastSequenceNumber = LastSequenceNumber + 1 WHERE Id = $id;";
                update.Parameters.AddWithValue("$id", SqlConvert.ToText(DeviceId));
                await update.ExecuteNonQueryAsync(ct);
            }

            using (var select = _provider.Connection.CreateCommand())
            {
                select.Transaction = _provider.CurrentTransaction;
                select.CommandText = "SELECT LastSequenceNumber FROM Devices WHERE Id = $id;";
                select.Parameters.AddWithValue("$id", SqlConvert.ToText(DeviceId));
                var result = await select.ExecuteScalarAsync(ct);
                return (long)result;
            }
        }
    }
}