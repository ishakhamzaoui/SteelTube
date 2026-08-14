using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Application.Common;

namespace SteelTube.Application.Backup.RestoreBackup
{
    /// <summary>Implements the flow from SAD 46: validate -> safety-backup the current database -> replace -> (caller restarts).</summary>
    public sealed class RestoreBackupCommandHandler
    {
        private readonly IBackupService _backupService;

        public RestoreBackupCommandHandler(IBackupService backupService)
        {
            _backupService = backupService;
        }

        public async Task<RestoreBackupResult> HandleAsync(RestoreBackupCommand command, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(command.BackupFilePath))
                throw new UseCaseValidationException("Choose a backup file to restore.");

            var isValid = await _backupService.ValidateBackupAsync(command.BackupFilePath, ct);
            if (!isValid)
                throw new UseCaseValidationException(
                    "That file doesn't look like a valid SteelTube backup (it failed a database integrity check).");

            await _backupService.RestoreAsync(command.BackupFilePath, ct);

            return new RestoreBackupResult { RestartRequired = true };
        }
    }
}