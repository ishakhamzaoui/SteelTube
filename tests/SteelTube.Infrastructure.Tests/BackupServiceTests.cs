using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteelTube.Application.Backup.CreateBackup;
using SteelTube.Application.Backup.RestoreBackup;
using SteelTube.Application.Inventory.AddStock;
using SteelTube.Application.Inventory.GetCurrentStock;
using SteelTube.Infrastructure;

namespace SteelTube.Infrastructure.Tests
{
    [TestClass]
    public class BackupServiceTests
    {
        private string _sourceDbPath;
        private string _targetDbPath;
        private string _backupsDir;

        [TestInitialize]
        public void Setup()
        {
            var root = Path.Combine(Path.GetTempPath(), $"steeltube-backup-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(root);
            _sourceDbPath = Path.Combine(root, "source.db");
            _targetDbPath = Path.Combine(root, "target.db");
            _backupsDir = Path.Combine(root, "backups");
        }

        [TestCleanup]
        public void Teardown()
        {
            try { Directory.Delete(Path.GetDirectoryName(_sourceDbPath), recursive: true); } catch { /* best effort */ }
        }

        [TestMethod]
        public async Task CreateBackup_produces_a_file_that_passes_validation()
        {
            using (var root = await CompositionRoot.CreateAsync(_sourceDbPath))
            {
                await root.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 50m });

                var result = await root.CreateBackup.HandleAsync(new CreateBackupCommand());

                File.Exists(result.FilePath).Should().BeTrue();
                (await root.BackupService.ValidateBackupAsync(result.FilePath)).Should().BeTrue();
            }
        }

        [TestMethod]
        public async Task ValidateBackup_rejects_a_missing_file()
        {
            using (var root = await CompositionRoot.CreateAsync(_sourceDbPath))
            {
                var isValid = await root.BackupService.ValidateBackupAsync(
                    Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid()}.db"));

                isValid.Should().BeFalse();
            }
        }

        [TestMethod]
        public async Task Restoring_a_backup_brings_its_data_into_the_target_database()
        {
            string backupFilePath;

            // 1. Seed a "source" database and back it up.
            using (var source = await CompositionRoot.CreateAsync(_sourceDbPath))
            {
                await source.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 75m });
                var backupResult = await source.CreateBackup.HandleAsync(new CreateBackupCommand());
                backupFilePath = backupResult.FilePath;
            }

            // 2. Restore that backup on top of a different, empty "target" database.
            //    RestoreAsync closes the live connection as part of the swap (SAD 46),
            //    so the CompositionRoot used for the restore call cannot be reused afterward.
            using (var target = await CompositionRoot.CreateAsync(_targetDbPath))
            {
                var restoreResult = await target.RestoreBackup.HandleAsync(new RestoreBackupCommand { BackupFilePath = backupFilePath });
                restoreResult.RestartRequired.Should().BeTrue();
            }

            // 3. Re-open the (now restored) target database as a fresh process would after restart.
            using (var reopened = await CompositionRoot.CreateAsync(_targetDbPath))
            {
                var stock = await reopened.GetCurrentStock.HandleAsync(new GetCurrentStockQuery());
                stock.Should().ContainSingle(i => i.DiameterMm == 500m && i.ThicknessMm == 10m && i.QuantityLengthMeters == 75m);
            }
        }
    }
}