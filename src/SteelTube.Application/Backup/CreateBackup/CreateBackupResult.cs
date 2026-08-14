using System;

namespace SteelTube.Application.Backup.CreateBackup
{
    public sealed class CreateBackupResult
    {
        public string FilePath { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public long SizeBytes { get; set; }
    }
}