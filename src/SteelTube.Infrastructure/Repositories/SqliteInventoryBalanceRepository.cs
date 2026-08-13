using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SteelTube.Application.Abstractions;
using SteelTube.Domain.Entities;
using SteelTube.Infrastructure.Persistence;

namespace SteelTube.Infrastructure.Repositories
{
    /// <inheritdoc cref="IInventoryBalanceRepository"/>
    public sealed class SqliteInventoryBalanceRepository : IInventoryBalanceRepository
    {
        private readonly ISqliteConnectionProvider _provider;

        public SqliteInventoryBalanceRepository(ISqliteConnectionProvider provider)
        {
            _provider = provider;
        }

        public async Task<InventoryBalance> GetAsync(Guid tubeSpecificationId, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "SELECT TubeSpecificationId, QuantityLengthM, UpdatedAt FROM InventoryBalances WHERE TubeSpecificationId = $id;"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(tubeSpecificationId));
                using (var reader = await command.ExecuteReaderAsync(ct))
                {
                    if (!await reader.ReadAsync(ct))
                        return null;
                    return Map(reader);
                }
            }
        }

        public async Task UpsertAsync(InventoryBalance balance, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "INSERT INTO InventoryBalances (TubeSpecificationId, QuantityLengthM, UpdatedAt) " +
                "VALUES ($id, $quantity, $updatedAt) " +
                "ON CONFLICT (TubeSpecificationId) DO UPDATE SET QuantityLengthM = $quantity, UpdatedAt = $updatedAt;"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(balance.TubeSpecificationId));
                command.Parameters.AddWithValue("$quantity", SqlConvert.ToText(balance.QuantityLengthMeters));
                command.Parameters.AddWithValue("$updatedAt", SqlConvert.ToText(balance.UpdatedAt));
                await command.ExecuteNonQueryAsync(ct);
            }
        }

        public async Task<IReadOnlyList<InventoryBalance>> GetAllAsync(CancellationToken ct = default)
        {
            var results = new List<InventoryBalance>();
            using (var command = CreateCommand(
                "SELECT TubeSpecificationId, QuantityLengthM, UpdatedAt FROM InventoryBalances;"))
            using (var reader = await command.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                    results.Add(Map(reader));
            }
            return results;
        }

        /// <summary>
        /// Rebuilds every balance from the operation ledger (SAD 22): the
        /// projection must always be reproducible from InventoryOperation
        /// alone. Reuses InventoryOperation.SignedLengthMeters rather than
        /// re-implementing the Purchase/Sale/Adjustment sign convention in
        /// SQL, so there is exactly one place that convention lives
        /// (SAD 3.6, SAD 67).
        /// </summary>
        public async Task RebuildAllAsync(CancellationToken ct = default)
        {
            var operations = new List<InventoryOperation>();
            using (var command = CreateCommand(
                "SELECT Id, OperationType, TubeSpecificationId, LengthM, WeightKg, WeightPerMeterUsed, " +
                "PieceCount, BusinessPartnerId, OperationDate, InsertedAt, OriginDeviceId, OriginSequenceNumber, Note " +
                "FROM InventoryOperations;"))
            using (var reader = await command.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                    operations.Add(SqliteInventoryOperationRepository.MapStatic(reader));
            }

            var utcNow = DateTime.UtcNow;
            var balancesByTubeSpecification = operations
                .GroupBy(o => o.TubeSpecificationId)
                .Select(g =>
                {
                    var balance = InventoryBalance.Create(g.Key, utcNow);
                    foreach (var operation in g)
                        balance.Apply(operation.SignedLengthMeters, utcNow);
                    return balance;
                })
                .ToList();

            using (var delete = CreateCommand("DELETE FROM InventoryBalances;"))
                await delete.ExecuteNonQueryAsync(ct);

            foreach (var balance in balancesByTubeSpecification)
                await UpsertAsync(balance, ct);
        }

        private SqliteCommand CreateCommand(string sql)
        {
            var command = _provider.Connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _provider.CurrentTransaction;
            return command;
        }

        private static InventoryBalance Map(SqliteDataReader reader) =>
            InventoryBalance.Rehydrate(
                SqlConvert.ToGuid(reader.GetValue(0)),
                SqlConvert.ToDecimal(reader.GetValue(1)),
                SqlConvert.ToDateTime(reader.GetValue(2)));
    }
}