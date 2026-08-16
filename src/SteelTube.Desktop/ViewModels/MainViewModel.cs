using System.Collections.ObjectModel;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>
    /// Root ViewModel behind MainWindow: the navigation list (SAD 49) plus
    /// whichever screen is currently selected.
    /// </summary>
    public sealed class MainViewModel : ViewModelBase
    {
        public ObservableCollection<NavigationItem> NavigationItems { get; }

        private NavigationItem _selectedItem;
        public NavigationItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value) && value != null)
                    CurrentViewModel = value.CreateViewModel();
            }
        }

        private object _currentViewModel;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        public MainViewModel(CompositionRoot root)
        {
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem("Dashboard", () => new DashboardViewModel(root)),
                new NavigationItem("Current Stock", () => new CurrentStockViewModel(root)),
                new NavigationItem("Add Stock", () => new StockMovementViewModel(root, isPurchase: true)),
                new NavigationItem("Remove Stock", () => new StockMovementViewModel(root, isPurchase: false)),
                new NavigationItem("Partners", () => new PartnersViewModel(root)),
                new NavigationItem("Weight Catalogue", () => new CatalogueViewModel(root)),
                new NavigationItem("Converter", () => new ConverterViewModel(root)),
                new NavigationItem("History", () => new PlaceholderViewModel(
                    "History", "History and audit trail is coming in a future phase (SAD \u00a710).")),
                new NavigationItem("Data", () => new BackupViewModel(root)),
                new NavigationItem("Synchronize", () => new SynchronizationViewModel(root)),
                new NavigationItem("Settings", () => new PlaceholderViewModel(
                    "Settings", "Settings are coming in a future phase (SAD \u00a754).")),
            };

            SelectedItem = NavigationItems[0];
        }
    }
}