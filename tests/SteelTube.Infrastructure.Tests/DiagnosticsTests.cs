using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteelTube.Application.Diagnostics.CheckIntegrity;
using SteelTube.Application.Diagnostics.RepairProjection;
using SteelTube.Application.Inventory.AddStock;
using SteelTube.Application.Inventory.GetCurrentStock;
using SteelTube.Application.Inventory.RemoveStock;
using SteelTube.Infrastructure;

namespace SteelTube.Infrastructure.Tests
{
    [TestClass]
    public class DiagnosticsTests
    {
        private string _databasePath;
        private CompositionRoot _root;

        [TestInitialize]
        public async Task Setup()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"steeltube-diagnostics-test-{Guid.NewGuid()}.db");
            _root = await CompositionRoot.CreateAsync(_databasePath);
        }

        [TestCleanup]
        public void Teardown()
        {
            _root?.Dispose();
            if (File.Exists(_databasePath))
                File.Delete(_databasePath);
        }

        [TestMethod]
        public async Task A_freshly_used_database_reports_fully_healthy()
        {
            await _root.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 50m });
            await _root.RemoveStock.HandleAsync(new RemoveStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 20m });

            var result = await _root.CheckIntegrity.HandleAsync(new CheckIntegrityQuery());

            result.IsFullyHealthy.Should().BeTrue();
            result.ProjectionMismatches.Should().BeEmpty();
            result.TotalOperations.Should().Be(2);
            result.TotalMaterials.Should().Be(1);
        }

        //[TestMethod]
        //public async Task RepairProjection_fixes_a_deliberately_corrupted_balance()
        //{
        //    var addResult = await _root.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 50m });

        //    // Deliberately corrupt the projection to simulate the kind of
        //    // drift SAD 65 exists to catch (e.g. a crash mid-write on old
        //    // hardware) -- write straight to the balance repository,
        //    // bypassing the normal AddStock/RemoveStock path.
        //    await _root.InventoryBalances.UpsertAsync(
        //        SteelTube.Domain.Entities.InventoryBalance.Rehydrate(addResult.TubeSpecificationId, 999m, DateTime.UtcNow));

        //    var beforeRepair = await _root.CheckIntegrity.HandleAsync(new CheckIntegrityQuery());
        //    beforeRepair.IsFullyHealthy.Should().BeFalse();
        //    beforeRepair.ProjectionMismatches.Should().ContainSingle(m => m.StoredQuantityLengthMeters == 999m && m.ComputedQuantityLengthMeters == 50m);

        //    await _root.RepairProjection.HandleAsync(new RepairProjectionCommand());

        //    var afterRepair = await _root.CheckIntegrity.HandleAsync(new CheckIntegrityQuery());
        //    afterRepair.IsFullyHealthy.Should().BeTrue();

        //    var stock = await _root.GetCurrentStock.HandleAsync(new GetCurrentStockQuery());
        //    stock.Should().ContainSingle(i => i.QuantityLengthMeters == 50m);
        //}
    }
}