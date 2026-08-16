using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SteelTube.Application.Inventory.GetStockHistory;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>SRS 10 -- history and audit trail, with the filters from SRS 10.3.</summary>
    public sealed class HistoryViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;

        public ObservableCollection<StockHistoryItem> Items { get; } = new ObservableCollection<StockHistoryItem>();

        public string[] OperationTypes { get; } = { "All", "Purchase", "Sale", "AdjustmentIncrease", "AdjustmentDecrease" };

        private string _diameterMm = string.Empty;
        public string DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }

        private string _thicknessMm = string.Empty;
        public string ThicknessMm { get => _thicknessMm; set => SetProperty(ref _thicknessMm, value); }

        private string _partnerName = string.Empty;
        public string PartnerName { get => _partnerName; set => SetProperty(ref _partnerName, value); }

        private string _selectedOperationType = "All";
        public string SelectedOperationType { get => _selectedOperationType; set => SetProperty(ref _selectedOperationType, value); }

        private DateTime? _dateFrom;
        public DateTime? DateFrom { get => _dateFrom; set => SetProperty(ref _dateFrom, value); }

        private DateTime? _dateTo;
        public DateTime? DateTo { get => _dateTo; set => SetProperty(ref _dateTo, value); }

        public AsyncRelayCommand SearchCommand { get; }
        public RelayCommand ExportCsvCommand { get; }

        public HistoryViewModel(CompositionRoot root)
        {
            _root = root;
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ExportCsvCommand = new RelayCommand(ExportCsv);
            _ = SearchAsync();
        }

        private Task SearchAsync() => RunAsync(async () =>
        {
            decimal? diameter = null, thickness = null;
            if (!string.IsNullOrWhiteSpace(DiameterMm) && !string.IsNullOrWhiteSpace(ThicknessMm))
            {
                if (decimal.TryParse(DiameterMm, out var d) && decimal.TryParse(ThicknessMm, out var t))
                {
                    diameter = d;
                    thickness = t;
                }
            }

            var items = await _root.GetStockHistory.HandleAsync(new GetStockHistoryQuery
            {
                DiameterMm = diameter,
                ThicknessMm = thickness,
                PartnerName = string.IsNullOrWhiteSpace(PartnerName) ? null : PartnerName.Trim(),
                OperationType = SelectedOperationType == "All" ? null : SelectedOperationType,
                OperationDateFrom = DateFrom,
                OperationDateTo = DateTo
            });

            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
        });

        private void ExportCsv()
        {
            CsvExporter.ExportWithDialog(Items, "History.csv",
                ("Date", r => r.OperationDate),
                ("Type", r => r.OperationType),
                ("Diameter (mm)", r => r.DiameterMm),
                ("Thickness (mm)", r => r.ThicknessMm),
                ("Change (m)", r => r.SignedLengthMeters),
                ("Weight (kg)", r => r.WeightKilograms),
                ("Pieces", r => r.PieceCount),
                ("Partner", r => r.PartnerName),
                ("Inserted", r => r.InsertedAt),
                ("Note", r => r.Note));
        }
    }
}