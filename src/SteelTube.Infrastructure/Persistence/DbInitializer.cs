using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace SteelTube.Infrastructure.Persistence
{
    /// <summary>
    /// Creates the database schema from SAD 27 on first run, and tracks a
    /// schema version for future controlled migrations (SAD 44 — "the
    /// migration process must not silently discard user data"; this initial
    /// version only ever CREATEs, it never drops or alters).
    /// </summary>
    public static class DbInitializer
    {
        public const int CurrentSchemaVersion = 1;

        public static async Task EnsureCreatedAsync(SqliteConnection connection, CancellationToken ct = default)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
CREATE TABLE IF NOT EXISTS SchemaVersion (
    Version INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS TubeSpecifications (
    Id           TEXT NOT NULL PRIMARY KEY,
    DiameterMm   TEXT NOT NULL,
    ThicknessMm  TEXT NOT NULL,
    CreatedAt    TEXT NOT NULL,
    UpdatedAt    TEXT NOT NULL,
    UNIQUE (DiameterMm, ThicknessMm)
);

CREATE TABLE IF NOT EXISTS BusinessPartners (
    Id           TEXT NOT NULL PRIMARY KEY,
    Name         TEXT NOT NULL,
    IsProvider   INTEGER NOT NULL,
    IsCustomer   INTEGER NOT NULL,
    CreatedAt    TEXT NOT NULL,
    UpdatedAt    TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_BusinessPartners_Name ON BusinessPartners (Name);

CREATE TABLE IF NOT EXISTS WeightCatalogue (
    Id           TEXT NOT NULL PRIMARY KEY,
    DiameterMm   TEXT NOT NULL,
    ThicknessMm  TEXT NOT NULL,
    KgPerMeter   TEXT NOT NULL,
    CreatedAt    TEXT NOT NULL,
    UpdatedAt    TEXT NOT NULL,
    UNIQUE (DiameterMm, ThicknessMm)
);

CREATE TABLE IF NOT EXISTS InventoryOperations (
    Id                    TEXT NOT NULL PRIMARY KEY,
    OperationType         TEXT NOT NULL,
    TubeSpecificationId   TEXT NOT NULL,
    LengthM               TEXT NOT NULL,
    WeightKg              TEXT NULL,
    WeightPerMeterUsed    TEXT NULL,
    PieceCount            INTEGER NULL,
    BusinessPartnerId     TEXT NULL,
    OperationDate         TEXT NOT NULL,
    InsertedAt            TEXT NOT NULL,
    OriginDeviceId        TEXT NOT NULL,
    OriginSequenceNumber  INTEGER NOT NULL,
    Note                  TEXT NULL,
    FOREIGN KEY (TubeSpecificationId) REFERENCES TubeSpecifications (Id),
    FOREIGN KEY (BusinessPartnerId) REFERENCES BusinessPartners (Id)
);
-- Indexed columns per SAD 59.
CREATE INDEX IF NOT EXISTS IX_InventoryOperations_TubeSpecificationId ON InventoryOperations (TubeSpecificationId);
CREATE INDEX IF NOT EXISTS IX_InventoryOperations_BusinessPartnerId ON InventoryOperations (BusinessPartnerId);
CREATE INDEX IF NOT EXISTS IX_InventoryOperations_OperationDate ON InventoryOperations (OperationDate);
CREATE INDEX IF NOT EXISTS IX_InventoryOperations_InsertedAt ON InventoryOperations (InsertedAt);
CREATE INDEX IF NOT EXISTS IX_InventoryOperations_OperationType ON InventoryOperations (OperationType);
CREATE INDEX IF NOT EXISTS IX_InventoryOperations_OriginDeviceId_OriginSequenceNumber ON InventoryOperations (OriginDeviceId, OriginSequenceNumber);

CREATE TABLE IF NOT EXISTS InventoryBalances (
    TubeSpecificationId  TEXT NOT NULL PRIMARY KEY,
    QuantityLengthM      TEXT NOT NULL,
    UpdatedAt            TEXT NOT NULL,
    FOREIGN KEY (TubeSpecificationId) REFERENCES TubeSpecifications (Id)
);

-- One row per installation (SAD 28). LastSequenceNumber backs the
-- monotonically increasing local counter from SAD 29.
CREATE TABLE IF NOT EXISTS Devices (
    Id                  TEXT NOT NULL PRIMARY KEY,
    Name                TEXT NOT NULL,
    CreatedAt           TEXT NOT NULL,
    LastSequenceNumber  INTEGER NOT NULL
);
";
                await command.ExecuteNonQueryAsync(ct);
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM SchemaVersion;";
                var rowCount = (long)await command.ExecuteScalarAsync(ct);
                if (rowCount == 0)
                {
                    using (var insert = connection.CreateCommand())
                    {
                        insert.CommandText = "INSERT INTO SchemaVersion (Version) VALUES ($version);";
                        insert.Parameters.AddWithValue("$version", CurrentSchemaVersion);
                        await insert.ExecuteNonQueryAsync(ct);
                    }
                }
            }
        }
    }
}