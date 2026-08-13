using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SteelTube.Application.Abstractions;
using SteelTube.Domain.Entities;
using SteelTube.Infrastructure.Persistence;

namespace SteelTube.Infrastructure.Repositories
{
    /// <inheritdoc cref="IBusinessPartnerRepository"/>
    public sealed class SqliteBusinessPartnerRepository : IBusinessPartnerRepository
    {
        private readonly ISqliteConnectionProvider _provider;

        public SqliteBusinessPartnerRepository(ISqliteConnectionProvider provider)
        {
            _provider = provider;
        }

        public async Task<BusinessPartner> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "SELECT Id, Name, IsProvider, IsCustomer, CreatedAt, UpdatedAt FROM BusinessPartners WHERE Id = $id;"))
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

        public async Task<BusinessPartner> FindByNameAsync(string name, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "SELECT Id, Name, IsProvider, IsCustomer, CreatedAt, UpdatedAt FROM BusinessPartners " +
                "WHERE Name = $name COLLATE NOCASE;"))
            {
                command.Parameters.AddWithValue("$name", name);
                using (var reader = await command.ExecuteReaderAsync(ct))
                {
                    if (!await reader.ReadAsync(ct))
                        return null;
                    return Map(reader);
                }
            }
        }

        public async Task<BusinessPartner> GetOrCreateByNameAsync(string name, DateTime utcNow, CancellationToken ct = default)
        {
            var existing = await FindByNameAsync(name, ct);
            if (existing != null)
                return existing;

            var partner = BusinessPartner.CreateMinimal(name, utcNow);
            await AddAsync(partner, ct);
            return partner;
        }

        public async Task AddAsync(BusinessPartner partner, CancellationToken ct = default)
        {
            using (var command = CreateCommand(
                "INSERT INTO BusinessPartners (Id, Name, IsProvider, IsCustomer, CreatedAt, UpdatedAt) " +
                "VALUES ($id, $name, $isProvider, $isCustomer, $createdAt, $updatedAt);"))
            {
                command.Parameters.AddWithValue("$id", SqlConvert.ToText(partner.Id));
                command.Parameters.AddWithValue("$name", partner.Name);
                command.Parameters.AddWithValue("$isProvider", partner.IsProvider ? 1 : 0);
                command.Parameters.AddWithValue("$isCustomer", partner.IsCustomer ? 1 : 0);
                command.Parameters.AddWithValue("$createdAt", SqlConvert.ToText(partner.CreatedAt));
                command.Parameters.AddWithValue("$updatedAt", SqlConvert.ToText(partner.UpdatedAt));
                await command.ExecuteNonQueryAsync(ct);
            }
        }

        public async Task<IReadOnlyList<BusinessPartner>> GetAllAsync(CancellationToken ct = default)
        {
            var results = new List<BusinessPartner>();
            using (var command = CreateCommand(
                "SELECT Id, Name, IsProvider, IsCustomer, CreatedAt, UpdatedAt FROM BusinessPartners ORDER BY Name COLLATE NOCASE;"))
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

        private static BusinessPartner Map(SqliteDataReader reader) =>
            BusinessPartner.Rehydrate(
                SqlConvert.ToGuid(reader.GetValue(0)),
                reader.GetString(1),
                reader.GetInt64(2) != 0,
                reader.GetInt64(3) != 0,
                SqlConvert.ToDateTime(reader.GetValue(4)),
                SqlConvert.ToDateTime(reader.GetValue(5)));
    }
}