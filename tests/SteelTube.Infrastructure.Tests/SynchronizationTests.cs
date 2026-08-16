using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteelTube.Application.Common;
using SteelTube.Application.Inventory.AddStock;
using SteelTube.Application.Inventory.GetCurrentStock;
using SteelTube.Application.Inventory.RemoveStock;
using SteelTube.Application.Synchronization.ApplyImport;
using SteelTube.Application.Synchronization.Export;
using SteelTube.Application.Synchronization.PreviewImport;
using SteelTube.Infrastructure;

namespace SteelTube.Infrastructure.Tests
{
    /// <summary>
    /// Exercises the actual two-device merge scenario from SRS 24: Device A
    /// purchases +50m and exports; Device B imports it, sells 20m, and
    /// exports back; Device A must end up at exactly 30m -- never 80m, and
    /// never with a duplicated purchase. Also covers SAD 35 idempotency and
    /// SAD 72's "corrupt JSON is rejected" / "invalid version is rejected"
    /// cases.
    /// </summary>
    [TestClass]
    public class SynchronizationTests
    {
        private string _pathA;
        private string _pathB;

        [TestInitialize]
        public void Setup()
        {
            var root = Path.Combine(Path.GetTempPath(), $"steeltube-sync-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(root);
            _pathA = Path.Combine(root, "deviceA.db");
            _pathB = Path.Combine(root, "deviceB.db");
        }

        [TestCleanup]
        public void Teardown()
        {
            try { Directory.Delete(Path.GetDirectoryName(_pathA), recursive: true); } catch { /* best effort */ }
        }

        [TestMethod]
        public async Task Two_device_round_trip_matches_SRS_24_exactly()
        {
            using (var deviceA = await CompositionRoot.CreateAsync(_pathA))
            using (var deviceB = await CompositionRoot.CreateAsync(_pathB))
            {
                // Device A: +50m, export.
                await deviceA.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 50m });
                var exportFromA = await deviceA.ExportOperations.HandleAsync(new ExportOperationsCommand());

                // Device B: import A's package -> 50m, then sell 20m -> 30m, then export.
                var previewOnB = await deviceB.PreviewImport.HandleAsync(new PreviewImportQuery { PackageJson = exportFromA.PackageJson });
                previewOnB.NewOperationsCount.Should().Be(1);
                previewOnB.AffectedMaterials.Should().ContainSingle(m => m.ResultingQuantityLengthMeters == 50m);

                await deviceB.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = exportFromA.PackageJson });
                (await deviceB.GetCurrentStock.HandleAsync(new GetCurrentStockQuery()))
                    .Should().ContainSingle(i => i.QuantityLengthMeters == 50m);

                await deviceB.RemoveStock.HandleAsync(new RemoveStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 20m });
                var exportFromB = await deviceB.ExportOperations.HandleAsync(new ExportOperationsCommand());

                // Device A: import B's full export back. A already knows its own purchase;
                // only B's sale is new. Result must be 50 - 20 = 30, never 80.
                var previewOnA = await deviceA.PreviewImport.HandleAsync(new PreviewImportQuery { PackageJson = exportFromB.PackageJson });
                previewOnA.NewOperationsCount.Should().Be(1);
                previewOnA.AlreadyKnownCount.Should().Be(1);
                previewOnA.AffectedMaterials.Should().ContainSingle(m =>
                    m.CurrentQuantityLengthMeters == 50m && m.DeltaLengthMeters == -20m && m.ResultingQuantityLengthMeters == 30m);

                var applyOnA = await deviceA.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = exportFromB.PackageJson });
                applyOnA.NewOperationsInserted.Should().Be(1);
                applyOnA.AlreadyKnownSkipped.Should().Be(1);

                var finalStockA = await deviceA.GetCurrentStock.HandleAsync(new GetCurrentStockQuery());
                finalStockA.Should().ContainSingle(i => i.DiameterMm == 500m && i.ThicknessMm == 10m && i.QuantityLengthMeters == 30m);
            }
        }

        [TestMethod]
        public async Task Importing_the_same_package_twice_has_no_additional_effect()
        {
            using (var deviceA = await CompositionRoot.CreateAsync(_pathA))
            using (var deviceB = await CompositionRoot.CreateAsync(_pathB))
            {
                await deviceA.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 50m });
                var export = await deviceA.ExportOperations.HandleAsync(new ExportOperationsCommand());

                var firstApply = await deviceB.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = export.PackageJson });
                var secondApply = await deviceB.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = export.PackageJson });
                var thirdApply = await deviceB.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = export.PackageJson });

                firstApply.NewOperationsInserted.Should().Be(1);
                secondApply.NewOperationsInserted.Should().Be(0);
                thirdApply.NewOperationsInserted.Should().Be(0);

                (await deviceB.GetCurrentStock.HandleAsync(new GetCurrentStockQuery()))
                    .Should().ContainSingle(i => i.QuantityLengthMeters == 50m);
            }
        }

        [TestMethod]
        public async Task Concurrent_offline_oversell_is_merged_and_flagged_not_rejected()
        {
            // SAD 36 worked example: from a shared 100m, A sells 80, B sells 50 -> merged result is -30m.
            using (var deviceA = await CompositionRoot.CreateAsync(_pathA))
            using (var deviceB = await CompositionRoot.CreateAsync(_pathB))
            {
                await deviceA.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 100m });
                var initialExport = await deviceA.ExportOperations.HandleAsync(new ExportOperationsCommand());
                await deviceB.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = initialExport.PackageJson });

                await deviceA.RemoveStock.HandleAsync(new RemoveStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 80m });
                await deviceB.RemoveStock.HandleAsync(new RemoveStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 50m });

                var exportFromB = await deviceB.ExportOperations.HandleAsync(new ExportOperationsCommand());
                var result = await deviceA.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = exportFromB.PackageJson });

                result.NewOperationsInserted.Should().Be(1); // only B's sale is new to A
                result.NegativeStockWarnings.Should().ContainSingle(w => w.ResultingQuantityLengthMeters == -30m);

                var finalStock = await deviceA.GetCurrentStock.HandleAsync(new GetCurrentStockQuery());
                finalStock.Should().ContainSingle(i => i.QuantityLengthMeters == -30m);
            }
        }

        [TestMethod]
        public async Task Corrupt_json_is_rejected_and_leaves_the_database_unchanged()
        {
            using (var deviceB = await CompositionRoot.CreateAsync(_pathB))
            {
                Func<Task> act = () => deviceB.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = "{ not valid json" });

                await act.Should().ThrowAsync<SynchronizationException>();
                (await deviceB.GetCurrentStock.HandleAsync(new GetCurrentStockQuery())).Should().BeEmpty();
            }
        }

        [TestMethod]
        public async Task Unsupported_format_version_is_rejected()
        {
            using (var deviceB = await CompositionRoot.CreateAsync(_pathB))
            {
                var badPackageJson = "{\"formatVersion\":999,\"packageId\":\"" + Guid.NewGuid() +
                    "\",\"sourceDeviceId\":\"" + Guid.NewGuid() + "\",\"sourceDeviceName\":\"X\",\"createdAtUtc\":\"2026-01-01T00:00:00Z\",\"operations\":[]}";

                Func<Task> act = () => deviceB.ApplyImport.HandleAsync(new ApplyImportCommand { PackageJson = badPackageJson });

                await act.Should().ThrowAsync<SynchronizationException>();
            }
        }
    }
}