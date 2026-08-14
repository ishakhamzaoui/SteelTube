using System;
using System.Globalization;
using System.Threading.Tasks;
using SteelTube.Application.Common;
using SteelTube.Application.Inventory.AddStock;
using SteelTube.Application.Inventory.RemoveStock;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>
    /// Backs both the Add Stock and Remove Stock screens (SRS 9.5): they
    /// are the same form shape, differing only in which use case gets
    /// called and how the result reads. Fields mirror
    /// <see cref="AddStockCommand"/>/<see cref="RemoveStockCommand"/>
    /// directly: Diameter, Thickness, a Length/Weight toggle (SRS 7.4),
    /// and an "Advanced" group (pieces, operation date) kept visually
    /// secondary per SAD 50.
    /// </summary>
    public sealed class StockMovementViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;
        private readonly bool _isPurchase;

        public string Title => _isPurchase ? "Add Stock" : "Remove Stock";
        public string SubmitLabel => _isPurchase ? "Add" : "Remove";

        private string _diameterMm = string.Empty;
        public string DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }

        private string _thicknessMm = string.Empty;
        public string ThicknessMm { get => _thicknessMm; set => SetProperty(ref _thicknessMm, value); }

        private bool _useWeightInput;
        public bool UseWeightInput { get => _useWeightInput; set => SetProperty(ref _useWeightInput, value); }

        private string _lengthMeters = string.Empty;
        public string LengthMeters { get => _lengthMeters; set => SetProperty(ref _lengthMeters, value); }

        private string _weightKilograms = string.Empty;
        public string WeightKilograms { get => _weightKilograms; set => SetProperty(ref _weightKilograms, value); }

        private string _pieceCount = string.Empty;
        public string PieceCount { get => _pieceCount; set => SetProperty(ref _pieceCount, value); }

        private string _partnerName = string.Empty;
        public string PartnerName { get => _partnerName; set => SetProperty(ref _partnerName, value); }

        private DateTime? _operationDate;
        public DateTime? OperationDate { get => _operationDate; set => SetProperty(ref _operationDate, value); }

        private string _note = string.Empty;
        public string Note { get => _note; set => SetProperty(ref _note, value); }

        public AsyncRelayCommand SubmitCommand { get; }

        public StockMovementViewModel(CompositionRoot root, bool isPurchase)
        {
            _root = root;
            _isPurchase = isPurchase;
            SubmitCommand = new AsyncRelayCommand(SubmitAsync);
        }

        private Task SubmitAsync() => RunAsync(async () =>
        {
            var diameter = ParseRequiredDecimal(DiameterMm, "Diameter");
            var thickness = ParseRequiredDecimal(ThicknessMm, "Thickness");

            decimal? length = null;
            decimal? weight = null;
            if (UseWeightInput)
                weight = ParseRequiredDecimal(WeightKilograms, "Weight");
            else
                length = ParseRequiredDecimal(LengthMeters, "Length");

            int? pieceCount = null;
            if (!string.IsNullOrWhiteSpace(PieceCount))
            {
                if (!int.TryParse(PieceCount, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsedPieces))
                    throw new UseCaseValidationException("Number of pieces must be a whole number.");
                pieceCount = parsedPieces;
            }

            var partnerName = string.IsNullOrWhiteSpace(PartnerName) ? null : PartnerName.Trim();

            if (_isPurchase)
            {
                var result = await _root.AddStock.HandleAsync(new AddStockCommand
                {
                    DiameterMm = diameter,
                    ThicknessMm = thickness,
                    LengthMeters = length,
                    WeightKilograms = weight,
                    PieceCount = pieceCount,
                    BusinessPartnerName = partnerName,
                    OperationDate = OperationDate,
                    Note = string.IsNullOrWhiteSpace(Note) ? null : Note
                });

                SetSuccessMessage($"Added. Stock is now {result.ResultingStockLengthMeters:0.###} m.");
            }
            else
            {
                var result = await _root.RemoveStock.HandleAsync(new RemoveStockCommand
                {
                    DiameterMm = diameter,
                    ThicknessMm = thickness,
                    LengthMeters = length,
                    WeightKilograms = weight,
                    PieceCount = pieceCount,
                    BusinessPartnerName = partnerName,
                    OperationDate = OperationDate,
                    Note = string.IsNullOrWhiteSpace(Note) ? null : Note
                });

                var message = $"Removed. Stock is now {result.ResultingStockLengthMeters:0.###} m.";
                if (result.ResultsInNegativeStock)
                    message += " Warning: stock is now negative -- please review (SAD \u00a737).";
                SetSuccessMessage(message);
            }

            // Clear the quantity-specific fields but keep Diameter/Thickness/Partner,
            // since the same material is often entered several times in a row (SRS 9.1).
            LengthMeters = string.Empty;
            WeightKilograms = string.Empty;
            PieceCount = string.Empty;
            Note = string.Empty;
        });

        private static decimal ParseRequiredDecimal(string text, string fieldName)
        {
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value <= 0)
                throw new UseCaseValidationException($"{fieldName} must be a number greater than 0.");
            return value;
        }
    }
}