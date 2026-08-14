using System.Collections.ObjectModel;
using SteelTube.Application.Inventory.GetCurrentStock;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>SRS 9.3 — material grouped by Diameter + Thickness; total length is the only quantity shown.</summary>
    public sealed class CurrentStockViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;

        public ObservableCollection<CurrentStockItem> Items { get; } = new ObservableCollection<CurrentStockItem>();

        public AsyncRelayCommand RefreshCommand { get; }

        public CurrentStockViewModel(CompositionRoot root)
        {
            _root = root;
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            _ = RefreshAsync();
        }

        private System.Threading.Tasks.Task RefreshAsync() => RunAsync(async () =>
        {
            var items = await _root.GetCurrentStock.HandleAsync(new GetCurrentStockQuery());
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
        });
    }
}