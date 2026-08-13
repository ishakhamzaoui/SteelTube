using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SteelTube.Application.Abstractions;
using SteelTube.Domain.Entities;
using SteelTube.Domain.ValueObjects;
using SteelTube.Infrastructure.Persistence;

namespace SteelTube.Infrastructure.Repositories
{
    /// <inheritdoc cref="ITubeSpecificationRepository"/>
    public sealed class SqliteTubeSpecificationRepository : ITubeSpecificationRepository
    {
        private readonly ISqliteConnectionProvider _provider;

        public SqliteTubeSpecificationRepository(ISqliteConnectionProvider provider)
        {
            _provider = provider;
        }

        public async Task<TubeSpecification> FindAsync(Diameter diameter, Thickness thickness, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "SELECT Id, DiameterMm, ThicknessMm, CreatedAt, UpdatedAt FROM TubeSpecifications " +
                "WHERE CAST(DiameterMm AS REAL) = $diameter AND CAST(ThicknessMm AS REAL) = $thickness;"))
            {
                command.Parameters.AddWithValue("$diameter", SqlConvert.ToLookupDouble(diameter.Millimeters));
                command.Parameters.AddWithValue("$thickness", SqlConvert.ToLookupDouble(thickness.Millimeters));

                using (var reader = await command.ExecuteReaderAsync(ct))
                {
                    if (!await reader.ReadAsync(ct))
                        return null;
                    return Map(reader);
                }
            }
        }

        public async Task<TubeSpecification> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "SELECT Id, DiameterMm, ThicknessMm, CreatedAt, UpdatedAt FROM TubeSpecifications WHERE Id = $id;"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(id));

                using (var reader = await command.ExecuteReaderAsync(ct))
                {
                    if (!await reader.ReadAsync(ct))
                        return null;
                    return Map(reader);
                }
            }
        }

        public async Task<TubeSpecification> GetOrCreateAsync(Diameter diameter, Thickness thickness, DateTime utcNow, CancellationToken ct = default)
        {
            var existing = await FindAsync(diameter, thickness, ct);
            if (existing != null)
                return existing;

            var specification = TubeSpecification.Create(diameter, thickness, utcNow);

            using (var command = CreateCommand(
                "INSERT INTO TubeSpecifications (Id, DiameterMm, ThicknessMm, CreatedAt, UpdatedAt) " +
                "VALUES ($id, $diameter, $thickness, $createdAt, $updatedAt);"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(specification.Id));
                command.Parameters.AddWithValue("$diameter", SqlConvert.ToText(specification.Diameter.Millimeters));
                command.Parameters.AddWithValue("$thickness", SqlConvert.ToText(specification.Thickness.Millimeters));
                command.Parameters.AddWithValue("$createdAt", SqlConvert.ToText(specification.CreatedAt));
                command.Parameters.AddWithValue("$updatedAt", SqlConvert.ToText(specification.UpdatedAt));

                try
                {
                    await command.ExecuteNonQueryAsync(ct);
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT (UNIQUE)
                {
                    // Lost a race with another write in the same process/transaction pattern
                    // (SAD 9.2 uniqueness constraint) -- re-read the row that won.
                    return await FindAsync(diameter, thickness, ct);
                }
            }

            return specification;
        }

        public async Task<IReadOnlyList<TubeSpecification>> GetAllAsync(CancellationToken ct = default)
        {
            var results = new List<TubeSpecification>();
            using (var command = CreateCommand(
                "SELECT Id, DiameterMm, ThicknessMm, CreatedAt, UpdatedAt FROM TubeSpecifications " +
                "ORDER BY CAST(DiameterMm AS REAL), CAST(ThicknessMm AS REAL);"))
            using (var reader = await command.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                    results.Add(Map(reader));
            }
            return results;
        }

        private SqliteCommand CreateCommand(string sql)
        {
            var command = _provider.Connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _provider.CurrentTransaction;
            return command;
        }

        private static TubeSpecification Map(SqliteDataReader reader) =>
            TubeSpecification.Rehydrate(
                SqlConvert.ToGuid(reader.GetValue(0)),
                Diameter.FromMillimeters(SqlConvert.ToDecimal(reader.GetValue(1))),
                Thickness.FromMillimeters(SqlConvert.ToDecimal(reader.GetValue(2))),
                SqlConvert.ToDateTime(reader.GetValue(3)),
                SqlConvert.ToDateTime(reader.GetValue(4)));
    }
}