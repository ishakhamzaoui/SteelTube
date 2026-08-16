using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using SteelTube.Application.Catalogue.AddEntry;
using SteelTube.Application.Catalogue.GetCatalogue;
using SteelTube.Application.Catalogue.UpdateEntry;
using SteelTube.Application.Common;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>
    /// SRS 8.2 — catalogue management. "Add" always creates a new entry
    /// from the typed Diameter/Thickness/kg-per-m; "Update Selected" only
    /// changes the kg/m of whichever row is selected in the list, which
    /// keeps the Add-vs-Update distinction from SRS 8.3 visible in the UI
    /// rather than silently upserting.
    /// </summary>
    public sealed class CatalogueViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;

        public ObservableCollection<WeightCatalogueEntryDto> Entries { get; } = new ObservableCollection<WeightCatalogueEntryDto>();

        private WeightCatalogueEntryDto _selectedEntry;
        public WeightCatalogueEntryDto SelectedEntry
        {
            get => _selectedEntry;
            set => SetProperty(ref _selectedEntry, value);
        }

        private string _diameterMm = string.Empty;
        public string DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }

        private string _thicknessMm = string.Empty;
        public string ThicknessMm { get => _thicknessMm; set => SetProperty(ref _thicknessMm, value); }

        private string _kgPerMeter = string.Empty;
        public string KgPerMeter { get => _kgPerMeter; set => SetProperty(ref _kgPerMeter, value); }

        public AsyncRelayCommand RefreshCommand { get; }
        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand UpdateSelectedCommand { get; }
        public RelayCommand ExportCsvCommand { get; }

        public CatalogueViewModel(CompositionRoot root)
        {
            _root = root;
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            AddCommand = new AsyncRelayCommand(AddAsync);
            UpdateSelectedCommand = new AsyncRelayCommand(UpdateSelectedAsync, () => SelectedEntry != null);
            ExportCsvCommand = new RelayCommand(ExportCsv);
            _ = RefreshAsync();
        }

        private void ExportCsv()
        {
            CsvExporter.ExportWithDialog(Entries, "WeightCatalogue.csv",
                ("Diameter (mm)", r => r.DiameterMm),
                ("Thickness (mm)", r => r.ThicknessMm),
                ("Kg/m", r => r.KgPerMeter),
                ("Updated", r => r.UpdatedAt));
        }

        private Task RefreshAsync() => RunAsync(async () =>
        {
            var entries = await _root.GetCatalogue.HandleAsync(new GetCatalogueQuery());
            Entries.Clear();
            foreach (var entry in entries)
                Entries.Add(entry);
        });

        private Task AddAsync() => RunAsync(async () =>
        {
            var diameter = ParsePositiveDecimal(DiameterMm, "Diameter");
            var thickness = ParsePositiveDecimal(ThicknessMm, "Thickness");
            var kgPerMeter = ParsePositiveDecimal(KgPerMeter, "Kg/m");

            await _root.AddCatalogueEntry.HandleAsync(new AddCatalogueEntryCommand
            {
                DiameterMm = diameter,
                ThicknessMm = thickness,
                KgPerMeter = kgPerMeter
            });

            DiameterMm = string.Empty;
            ThicknessMm = string.Empty;
            KgPerMeter = string.Empty;

            await ReloadAsync();
            SetSuccessMessage("Catalogue entry added.");
        });

        private Task UpdateSelectedAsync() => RunAsync(async () =>
        {
            if (SelectedEntry is null)
                throw new UseCaseValidationException("Select a catalogue entry to update first.");

            var kgPerMeter = ParsePositiveDecimal(KgPerMeter, "Kg/m");

            await _root.UpdateCatalogueEntry.HandleAsync(new UpdateCatalogueEntryCommand
            {
                DiameterMm = SelectedEntry.DiameterMm,
                ThicknessMm = SelectedEntry.ThicknessMm,
                NewKgPerMeter = kgPerMeter
            });

            KgPerMeter = string.Empty;
            await ReloadAsync();
            SetSuccessMessage("Catalogue entry updated. Existing operations keep the kg/m they were recorded with (SAD \u00a717).");
        });

        private async Task ReloadAsync()
        {
            var entries = await _root.GetCatalogue.HandleAsync(new GetCatalogueQuery());
            Entries.Clear();
            foreach (var entry in entries)
                Entries.Add(entry);
        }

        private static decimal ParsePositiveDecimal(string text, string fieldName)
        {
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value <= 0)
                throw new UseCaseValidationException($"{fieldName} must be a number greater than 0.");
            return value;
        }
    }
}