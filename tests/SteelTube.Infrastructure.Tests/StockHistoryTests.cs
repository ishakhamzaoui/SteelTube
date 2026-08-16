using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteelTube.Application.Inventory.AddStock;
using SteelTube.Application.Inventory.GetStockHistory;
using SteelTube.Application.Inventory.RemoveStock;
using SteelTube.Infrastructure;

namespace SteelTube.Infrastructure.Tests
{
    [TestClass]
    public class StockHistoryTests
    {
        private string _databasePath;
        private CompositionRoot _root;

        [TestInitialize]
        public async Task Setup()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"steeltube-history-test-{Guid.NewGuid()}.db");
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
        public async Task History_shows_signed_length_for_both_purchases_and_sales()
        {
            await _root.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 50m, Note = "Purchase" });
            await _root.RemoveStock.HandleAsync(new RemoveStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 20m, Note = "Sale" });

            var history = await _root.GetStockHistory.HandleAsync(new GetStockHistoryQuery());

            history.Should().HaveCount(2);
            history.Should().Contain(h => h.OperationType == "Purchase" && h.SignedLengthMeters == 50m);
            history.Should().Contain(h => h.OperationType == "Sale" && h.SignedLengthMeters == -20m);
        }

        [TestMethod]
        public async Task Filtering_by_material_that_was_never_touched_returns_empty_without_creating_it()
        {
            var history = await _root.GetStockHistory.HandleAsync(new GetStockHistoryQuery { DiameterMm = 999m, ThicknessMm = 5m });

            history.Should().BeEmpty();

            var stock = await _root.GetCurrentStock.HandleAsync(new SteelTube.Application.Inventory.GetCurrentStock.GetCurrentStockQuery());
            stock.Should().BeEmpty(); // The filter lookup must be read-only -- it must not have created a TubeSpecification row.
        }

        [TestMethod]
        public async Task Filtering_by_partner_name_only_returns_that_partners_operations()
        {
            await _root.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 500m, ThicknessMm = 10m, LengthMeters = 50m, BusinessPartnerName = "ABC Steel" });
            await _root.AddStock.HandleAsync(new AddStockCommand { DiameterMm = 400m, ThicknessMm = 8m, LengthMeters = 30m, BusinessPartnerName = "XYZ Construction" });

            var history = await _root.GetStockHistory.HandleAsync(new GetStockHistoryQuery { PartnerName = "ABC Steel" });

            history.Should().ContainSingle(h => h.PartnerName == "ABC Steel");
        }
    }
}