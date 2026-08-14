namespace SteelTube.Application.Backup.RestoreBackup
{
    public sealed class RestoreBackupResult
    {
        /// <summary>Always true today -- restoring replaces the live database file, so the process must restart (SAD 46).</summary>
        public bool RestartRequired { get; set; }
    }
}