using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteelTube.Application.Inventory.AddStock;
using SteelTube.Application.Inventory.RemoveStock;
using SteelTube.Infrastructure;

namespace SteelTube.Infrastructure.Tests
{
    /// <summary>
    /// End-to-end tests through the real SQLite-backed stack (CompositionRoot
    /// -> SqliteUnitOfWork -> repositories), exercising the acceptance
    /// scenario shape from SRS 29 on a single device: Add Stock then Remove
    /// Stock must produce the exact expected running total, and it must
    /// come back out of a fresh read (not just an in-memory return value).
    /// </summary>
    [TestClass]
    public class CompositionRootTests
    {
        private string _databasePath;
        private CompositionRoot _root;

        [TestInitialize]
        public async Task Setup()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"steeltube-test-{Guid.NewGuid()}.db");
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
        public async Task AddStock_then_RemoveStock_produces_expected_running_total()
        {
            // SRS 29 acceptance scenario numbers, single-device slice.
            var addResult = await _root.AddStock.HandleAsync(new AddStockCommand
            {
                DiameterMm = 500m,
                ThicknessMm = 10m,
                LengthMeters = 50m,
                Note = "Initial purchase"
            });
            addResult.ResultingStockLengthMeters.Should().Be(50m);

            var removeResult = await _root.RemoveStock.HandleAsync(new RemoveStockCommand
            {
                DiameterMm = 500m,
                ThicknessMm = 10m,
                LengthMeters = 20m,
                Note = "Sale"
            });
            removeResult.ResultingStockLengthMeters.Should().Be(30m);
            removeResult.ResultsInNegativeStock.Should().BeFalse();

            var currentStock = await _root.GetCurrentStock.HandleAsync(new SteelTube.Application.Inventory.GetCurrentStock.GetCurrentStockQuery());
            currentStock.Should().ContainSingle(i => i.DiameterMm == 500m && i.ThicknessMm == 10m && i.QuantityLengthMeters == 30m);
        }

        [TestMethod]
        public async Task Two_operations_on_the_same_specification_reuse_the_same_TubeSpecification()
        {
            var first = await _root.AddStock.HandleAsync(new AddStockCommand
            {
                DiameterMm = 400m,
                ThicknessMm = 8m,
                LengthMeters = 10m
            });

            var second = await _root.AddStock.HandleAsync(new AddStockCommand
            {
                DiameterMm = 400m,
                ThicknessMm = 8m,
                LengthMeters = 5m
            });

            second.TubeSpecificationId.Should().Be(first.TubeSpecificationId);
            second.ResultingStockLengthMeters.Should().Be(15m);
        }

        [TestMethod]
        public async Task RemoveStock_beyond_available_length_flags_negative_stock_but_still_persists()
        {
            await _root.AddStock.HandleAsync(new AddStockCommand
            {
                DiameterMm = 500m,
                ThicknessMm = 10m,
                LengthMeters = 30m
            });

            var result = await _root.RemoveStock.HandleAsync(new RemoveStockCommand
            {
                DiameterMm = 500m,
                ThicknessMm = 10m,
                LengthMeters = 50m,
                Note = "Oversold while offline"
            });

            result.ResultingStockLengthMeters.Should().Be(-20m);
            result.ResultsInNegativeStock.Should().BeTrue();
        }
    }
}