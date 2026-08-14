using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SteelTube.Application.Abstractions;
using SteelTube.Infrastructure.Persistence;

namespace SteelTube.Infrastructure.Backup
{
    /// <inheritdoc cref="IBackupService"/>
    public sealed class SqliteBackupService : IBackupService
    {
        private readonly SqliteSession _session;
        private readonly string _defaultBackupDirectory;
        private readonly string _safetyBackupDirectory;

        public SqliteBackupService(SqliteSession session)
        {
            _session = session;
            var applicationDataDirectory = Path.GetDirectoryName(session.DatabasePath);
            _defaultBackupDirectory = Path.Combine(applicationDataDirectory, "Backups");
            _safetyBackupDirectory = Path.Combine(applicationDataDirectory, "SafetyBackups");
        }

        public async Task<BackupInfo> CreateBackupAsync(string destinationDirectory = null, CancellationToken ct = default)
        {
            var directory = destinationDirectory ?? _defaultBackupDirectory;
            Directory.CreateDirectory(directory);

            var utcNow = DateTime.UtcNow;
            var destinationPath = Path.Combine(directory, $"SteelTube-Backup-{utcNow:yyyyMMdd-HHmmss}.db");

            // SQLite's own online backup API (SAD 45): produces a
            // consistent snapshot even while the live connection stays
            // open, unlike a raw file copy which could catch the database
            // mid-write.
            using (var destinationConnection = new SqliteConnection(
                new SqliteConnectionStringBuilder { DataSource = destinationPath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString()))
            {
                destinationConnection.Open();
                await Task.Run(() => _session.Connection.BackupDatabase(destinationConnection), ct);
            }

            if (!await ValidateBackupAsync(destinationPath, ct))
                throw new InvalidOperationException("The backup could not be verified after it was created.");

            var fileInfo = new FileInfo(destinationPath);
            return new BackupInfo
            {
                FilePath = destinationPath,
                CreatedAtUtc = utcNow,
                SizeBytes = fileInfo.Length
            };
        }

        public async Task<bool> ValidateBackupAsync(string backupFilePath, CancellationToken ct = default)
        {
            if (!File.Exists(backupFilePath))
                return false;

            try
            {
                using (var connection = new SqliteConnection(
                    new SqliteConnectionStringBuilder { DataSource = backupFilePath, Mode = SqliteOpenMode.ReadOnly }.ToString()))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA integrity_check;";
                        var result = (string)await command.ExecuteScalarAsync(ct);
                        return string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public async Task RestoreAsync(string backupFilePath, CancellationToken ct = default)
        {
            // SRS 16.3 step 2 / SAD 46: back up the current (about to be
            // overwritten) database before touching anything.
            await CreateBackupAsync(_safetyBackupDirectory, ct);

            var livePath = _session.DatabasePath;
            _session.CloseConnection();

            File.Copy(backupFilePath, livePath, overwrite: true);

            // WAL sidecar files from the connection we just closed must not
            // linger next to the restored file (SAD 23 -- journal_mode=WAL).
            TryDelete(livePath + "-wal");
            TryDelete(livePath + "-shm");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
                // Best effort -- SQLite will recreate/reconcile these on next open if they can't be removed now.
            }
        }
    }
}