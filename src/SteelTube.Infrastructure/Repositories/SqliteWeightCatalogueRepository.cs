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
    /// <inheritdoc cref="IWeightCatalogueRepository"/>
    public sealed class SqliteWeightCatalogueRepository : IWeightCatalogueRepository
    {
        private readonly ISqliteConnectionProvider _provider;

        public SqliteWeightCatalogueRepository(ISqliteConnectionProvider provider)
        {
            _provider = provider;
        }

        public async Task<WeightCatalogueEntry> FindAsync(Diameter diameter, Thickness thickness, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "SELECT Id, DiameterMm, ThicknessMm, KgPerMeter, CreatedAt, UpdatedAt FROM WeightCatalogue " +
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

        public async Task AddAsync(WeightCatalogueEntry entry, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "INSERT INTO WeightCatalogue (Id, DiameterMm, ThicknessMm, KgPerMeter, CreatedAt, UpdatedAt) " +
                "VALUES ($id, $diameter, $thickness, $kgPerMeter, $createdAt, $updatedAt);"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(entry.Id));
                command.Parameters.AddWithValue("$diameter", SqlConvert.ToText(entry.Diameter.Millimeters));
                command.Parameters.AddWithValue("$thickness", SqlConvert.ToText(entry.Thickness.Millimeters));
                command.Parameters.AddWithValue("$kgPerMeter", SqlConvert.ToText(entry.KgPerMeter.Value));
                command.Parameters.AddWithValue("$createdAt", SqlConvert.ToText(entry.CreatedAt));
                command.Parameters.AddWithValue("$updatedAt", SqlConvert.ToText(entry.UpdatedAt));
                await command.ExecuteNonQueryAsync(ct);
            }
        }

        public async Task UpdateAsync(WeightCatalogueEntry entry, CancellationToken ct = default)
        {
            // Only the factor and UpdatedAt change (SAD 17: historical
            // operations already snapshot the value they used, so updating
            // the catalogue here never rewrites history).
            using (var command = CreateCommand(
                "UPDATE WeightCatalogue SET KgPerMeter = $kgPerMeter, UpdatedAt = $updatedAt WHERE Id = $id;"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(entry.Id));
                command.Parameters.AddWithValue("$kgPerMeter", SqlConvert.ToText(entry.KgPerMeter.Value));
                command.Parameters.AddWithValue("$updatedAt", SqlConvert.ToText(entry.UpdatedAt));
                await command.ExecuteNonQueryAsync(ct);
            }
        }

        public async Task<IReadOnlyList<WeightCatalogueEntry>> GetAllAsync(CancellationToken ct = default)
        {
            var results = new List<WeightCatalogueEntry>();
            using (var command = CreateCommand(
                "SELECT Id, DiameterMm, ThicknessMm, KgPerMeter, CreatedAt, UpdatedAt FROM WeightCatalogue " +
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

        private static WeightCatalogueEntry Map(SqliteDataReader reader) =>
            WeightCatalogueEntry.Rehydrate(
                SqlConvert.ToGuid(reader.GetValue(0)),
                Diameter.FromMillimeters(SqlConvert.ToDecimal(reader.GetValue(1))),
                Thickness.FromMillimeters(SqlConvert.ToDecimal(reader.GetValue(2))),
                KgPerMeter.FromValue(SqlConvert.ToDecimal(reader.GetValue(3))),
                SqlConvert.ToDateTime(reader.GetValue(4)),
                SqlConvert.ToDateTime(reader.GetValue(5)));
    }
}