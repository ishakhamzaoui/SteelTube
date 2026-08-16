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
        public string DeviceName { get; }

        private SqliteDeviceContext(ISqliteConnectionProvider provider, Guid deviceId, string deviceName)
        {
            _provider = provider;
            DeviceId = deviceId;
            DeviceName = deviceName;
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
                select.CommandText = "SELECT Id, Name FROM Devices LIMIT 1;";
                using (var reader = await select.ExecuteReaderAsync(ct))
                {
                    if (await reader.ReadAsync(ct))
                        return new SqliteDeviceContext(provider, SqlConvert.ToGuid(reader.GetValue(0)), reader.GetString(1));
                }
            }

            var deviceId = Guid.NewGuid();
            var deviceName = Environment.MachineName;
            using (var insert = provider.Connection.CreateCommand())
            {
                insert.Transaction = provider.CurrentTransaction;
                insert.CommandText =
                    "INSERT INTO Devices (Id, Name, CreatedAt, LastSequenceNumber) VALUES ($id, $name, $createdAt, 0);";
                insert.Parameters.AddWithValue("$id", SqlConvert.ToText(deviceId));
                insert.Parameters.AddWithValue("$name", deviceName);
                insert.Parameters.AddWithValue("$createdAt", SqlConvert.ToText(DateTime.UtcNow));
                await insert.ExecuteNonQueryAsync(ct);
            }

            return new SqliteDeviceContext(provider, deviceId, deviceName);
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