using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;

namespace SteelTube.Application.Backup.CreateBackup
{
    public sealed class CreateBackupCommandHandler
    {
        private readonly IBackupService _backupService;

        public CreateBackupCommandHandler(IBackupService backupService)
        {
            _backupService = backupService;
        }

        // No IUnitOfWork here on purpose: a backup reads the live database
        // through SQLite's own backup API rather than writing through the
        // repositories, so there is nothing to wrap in a transaction.
        public async Task<CreateBackupResult> HandleAsync(CreateBackupCommand command, CancellationToken ct = default)
        {
            var info = await _backupService.CreateBackupAsync(ct: ct);
            return new CreateBackupResult
            {
                FilePath = info.FilePath,
                CreatedAtUtc = info.CreatedAtUtc,
                SizeBytes = info.SizeBytes
            };
        }
    }
}