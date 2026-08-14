namespace SteelTube.Application.Backup.RestoreBackup
{
    /// <summary>SRS 16.3. The UI is responsible for the "dangerous action" confirmation prompt (SAD 50) before sending this.</summary>
    public sealed class RestoreBackupCommand
    {
        public string BackupFilePath { get; set; }
    }
}