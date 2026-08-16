using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SteelTube.Application.Abstractions;
using SteelTube.Domain.Entities;
using SteelTube.Domain.Enums;
using SteelTube.Domain.ValueObjects;
using SteelTube.Infrastructure.Persistence;

namespace SteelTube.Infrastructure.Repositories
{
    /// <inheritdoc cref="IInventoryOperationRepository"/>
    public sealed class SqliteInventoryOperationRepository : IInventoryOperationRepository
    {
        private const string SelectColumns =
            "Id, OperationType, TubeSpecificationId, LengthM, WeightKg, WeightPerMeterUsed, " +
            "PieceCount, BusinessPartnerId, OperationDate, InsertedAt, OriginDeviceId, OriginSequenceNumber, Note";

        private readonly ISqliteConnectionProvider _provider;

        public SqliteInventoryOperationRepository(ISqliteConnectionProvider provider)
        {
            _provider = provider;
        }

        public async Task<bool> ExistsAsync(Guid operationId, CancellationToken ct = default)
        {
            using (var command = CreateCommand("SELECT 1 FROM InventoryOperations WHERE Id = $id LIMIT 1;"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(operationId));
                var result = await command.ExecuteScalarAsync(ct);
                return result != null;
            }
        }

        public async Task AddAsync(InventoryOperation operation, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "INSERT INTO InventoryOperations (" + SelectColumns + ") VALUES (" +
                "$id, $type, $tubeSpecId, $length, $weight, $weightPerMeter, $pieces, $partnerId, " +
                "$operationDate, $insertedAt, $deviceId, $sequence, $note);"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(operation.OperationId));
                command.Parameters.AddWithValue("$type", operation.OperationType.ToString());
                command.Parameters.AddWithValue("$tubeSpecId", SqlConvert.ToText(operation.TubeSpecificationId));
                command.Parameters.AddWithValue("$length", SqlConvert.ToText(operation.Length.Meters));
                command.Parameters.AddWithValue("$weight", SqlConvert.ToParam(operation.Weight is null ? null : SqlConvert.ToText(operation.Weight.Value.Kilograms)));
                command.Parameters.AddWithValue("$weightPerMeter", SqlConvert.ToParam(operation.WeightPerMeterUsed is null ? null : SqlConvert.ToText(operation.WeightPerMeterUsed.Value.Value)));
                command.Parameters.AddWithValue("$pieces", SqlConvert.ToParam(operation.PieceCount));
                command.Parameters.AddWithValue("$partnerId", SqlConvert.ToParam(operation.BusinessPartnerId is null ? null : SqlConvert.ToText(operation.BusinessPartnerId.Value)));
                command.Parameters.AddWithValue("$operationDate", SqlConvert.ToText(operation.OperationDate));
                command.Parameters.AddWithValue("$insertedAt", SqlConvert.ToText(operation.InsertedAt));
                command.Parameters.AddWithValue("$deviceId", SqlConvert.ToText(operation.OriginDeviceId));
                command.Parameters.AddWithValue("$sequence", operation.OriginSequenceNumber);
                command.Parameters.AddWithValue("$note", SqlConvert.ToParam(operation.Note));
                await command.ExecuteNonQueryAsync(ct);
            }
        }

        public async Task<IReadOnlyList<InventoryOperation>> GetHistoryAsync(InventoryOperationFilter filter, CancellationToken ct = default)
        {
            filter = filter ?? new InventoryOperationFilter();

            var where = new List<string>();
            var sql = new StringBuilder("SELECT " + SelectColumns + " FROM InventoryOperations");

            using (var command = _provider.Connection.CreateCommand())
            {
                command.Transaction = _provider.CurrentTransaction;

                if (filter.TubeSpecificationId != null)
                {
                    where.Add("TubeSpecificationId = $tubeSpecId");
                    command.Parameters.AddWithValue("$tubeSpecId", SqlConvert.ToText(filter.TubeSpecificationId.Value));
                }
                if (filter.BusinessPartnerId != null)
                {
                    where.Add("BusinessPartnerId = $partnerId");
                    command.Parameters.AddWithValue("$partnerId", SqlConvert.ToText(filter.BusinessPartnerId.Value));
                }
                if (filter.OperationType != null)
                {
                    where.Add("OperationType = $opType");
                    command.Parameters.AddWithValue("$opType", filter.OperationType.Value.ToString());
                }
                if (filter.OperationDateFrom != null)
                {
                    where.Add("OperationDate >= $dateFrom");
                    command.Parameters.AddWithValue("$dateFrom", SqlConvert.ToText(filter.OperationDateFrom.Value));
                }
                if (filter.OperationDateTo != null)
                {
                    where.Add("OperationDate <= $dateTo");
                    command.Parameters.AddWithValue("$dateTo", SqlConvert.ToText(filter.OperationDateTo.Value));
                }

                if (where.Count > 0)
                    sql.Append(" WHERE ").Append(string.Join(" AND ", where));

                sql.Append(" ORDER BY OperationDate DESC, InsertedAt DESC LIMIT $take OFFSET $skip;");
                command.Parameters.AddWithValue("$take", filter.Take);
                command.Parameters.AddWithValue("$skip", filter.Skip);

                command.CommandText = sql.ToString();

                var results = new List<InventoryOperation>();
                using (var reader = await command.ExecuteReaderAsync(ct))
                {
                    while (await reader.ReadAsync(ct))
                        results.Add(MapStatic(reader));
                }
                return results;
            }
        }

        public async Task<IReadOnlyList<InventoryOperation>> GetByOriginDeviceAfterSequenceAsync(Guid deviceId, long afterSequenceNumber, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "SELECT " + SelectColumns + " FROM InventoryOperations " +
                "WHERE OriginDeviceId = $deviceId AND OriginSequenceNumber > $after " +
                "ORDER BY OriginSequenceNumber ASC;"))
            {
                command.Parameters.AddWithValue("$deviceId", SqlConvert.ToText(deviceId));
                command.Parameters.AddWithValue("$after", afterSequenceNumber);

                var results = new List<InventoryOperation>();
                using (var reader = await command.ExecuteReaderAsync(ct))
                {
                    while (await reader.ReadAsync(ct))
                        results.Add(MapStatic(reader));
                }
                return results;
            }
        }

        public async Task<IReadOnlyList<InventoryOperation>> GetAllForTubeSpecificationAsync(Guid tubeSpecificationId, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "SELECT " + SelectColumns + " FROM InventoryOperations " +
                "WHERE TubeSpecificationId = $id ORDER BY OperationDate ASC, InsertedAt ASC;"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(tubeSpecificationId));

                var results = new List<InventoryOperation>();
                using (var reader = await command.ExecuteReaderAsync(ct))
                {
                    while (await reader.ReadAsync(ct))
                        results.Add(MapStatic(reader));
                }
                return results;
            }
        }

        public async Task<IReadOnlyList<InventoryOperation>> GetAllAsync(CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "SELECT " + SelectColumns + " FROM InventoryOperations ORDER BY OperationDate ASC, InsertedAt ASC;"))
            {
                var results = new List<InventoryOperation>();
                using (var reader = await command.ExecuteReaderAsync(ct))
                {
                    while (await reader.ReadAsync(ct))
                        results.Add(MapStatic(reader));
                }
                return results;
            }
        }

        private SqliteCommand CreateCommand(string sql)
        {
            var command = _provider.Connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _provider.CurrentTransaction;
            return command;
        }

        /// <summary>
        /// Internal, reused by <see cref="SqliteInventoryBalanceRepository.RebuildAllAsync"/>
        /// so projection rebuilding shares the exact same row-to-entity mapping as normal reads.
        /// </summary>
        internal static InventoryOperation MapStatic(SqliteDataReader reader)
        {
            var operationType = (OperationType)Enum.Parse(typeof(OperationType), reader.GetString(1));
            var weight = reader.IsDBNull(4) ? (Weight?)null : Weight.FromKilograms(SqlConvert.ToDecimal(reader.GetValue(4)));
            var weightPerMeterUsed = reader.IsDBNull(5) ? (KgPerMeter?)null : KgPerMeter.FromValue(SqlConvert.ToDecimal(reader.GetValue(5)));
            var pieceCount = reader.IsDBNull(6) ? (int?)null : (int)reader.GetInt64(6);
            var businessPartnerId = reader.IsDBNull(7) ? (Guid?)null : SqlConvert.ToGuid(reader.GetValue(7));
            var note = reader.IsDBNull(12) ? null : reader.GetString(12);

            return InventoryOperation.Rehydrate(
                SqlConvert.ToGuid(reader.GetValue(0)),
                operationType,
                SqlConvert.ToGuid(reader.GetValue(2)),
                Length.FromMeters(SqlConvert.ToDecimal(reader.GetValue(3))),
                weight,
                weightPerMeterUsed,
                pieceCount,
                businessPartnerId,
                SqlConvert.ToDateTime(reader.GetValue(8)),
                SqlConvert.ToDateTime(reader.GetValue(9)),
                SqlConvert.ToGuid(reader.GetValue(10)),
                reader.GetInt64(11),
                note);
        }
    }
}