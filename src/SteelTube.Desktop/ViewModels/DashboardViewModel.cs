using System.Linq;
using SteelTube.Application.Inventory.GetCurrentStock;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>A quick-glance summary built from the same projection the Current Stock screen uses (SAD 21).</summary>
    public sealed class DashboardViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;

        private int _materialCount;
        public int MaterialCount { get => _materialCount; private set => SetProperty(ref _materialCount, value); }

        private decimal _totalLengthMeters;
        public decimal TotalLengthMeters { get => _totalLengthMeters; private set => SetProperty(ref _totalLengthMeters, value); }

        private int _negativeStockCount;
        public int NegativeStockCount { get => _negativeStockCount; private set => SetProperty(ref _negativeStockCount, value); }

        public DashboardViewModel(CompositionRoot root)
        {
            _root = root;
            _ = RunAsync(LoadAsync);
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            var items = await _root.GetCurrentStock.HandleAsync(new GetCurrentStockQuery());
            MaterialCount = items.Count;
            TotalLengthMeters = items.Sum(i => i.QuantityLengthMeters);
            NegativeStockCount = items.Count(i => i.IsNegative);
        }
    }
}