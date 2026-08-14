using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// Backup/restore abstraction (SAD 45, SAD 46, SRS 16). Implemented in
    /// Infrastructure using SQLite's own online backup mechanism -- SAD 45
    /// specifically calls for "a consistent SQLite backup rather than
    /// copying a potentially active database file blindly", which is
    /// exactly what SQLite's backup API guarantees even while the live
    /// connection is open and in use.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Creates a timestamped, verified backup. Pass null to use the
        /// default backup folder (SRS 17 Data settings: "Backup location").
        /// </summary>
        Task<BackupInfo> CreateBackupAsync(string destinationDirectory = null, CancellationToken ct = default);

        /// <summary>Opens the file and runs a SQLite integrity check without touching the live database (SRS 16.3 step 1).</summary>
        Task<bool> ValidateBackupAsync(string backupFilePath, CancellationToken ct = default);

        /// <summary>
        /// Backs up the current database first (SRS 16.3 step 2), then
        /// replaces it with <paramref name="backupFilePath"/>. This closes
        /// the live connection as part of the swap, so the application
        /// must restart afterwards (SAD 46's flow ends with
        /// "Reload application") -- callers should treat any use of the
        /// database after this returns as invalid until the process restarts.
        /// </summary>
        Task RestoreAsync(string backupFilePath, CancellationToken ct = default);
    }

    public sealed class BackupInfo
    {
        public string FilePath { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public long SizeBytes { get; set; }
    }
}