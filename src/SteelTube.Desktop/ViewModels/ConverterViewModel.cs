using System.Globalization;
using System.Threading.Tasks;
using SteelTube.Application.Common;
using SteelTube.Application.Conversion.CalculateLength;
using SteelTube.Application.Conversion.CalculateWeight;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>Standalone Length <-> Weight calculator (SRS 7, SAD 15/16). Read-only -- never touches inventory.</summary>
    public sealed class ConverterViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;

        private string _diameterMm = string.Empty;
        public string DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }

        private string _thicknessMm = string.Empty;
        public string ThicknessMm { get => _thicknessMm; set => SetProperty(ref _thicknessMm, value); }

        private string _lengthMeters = string.Empty;
        public string LengthMeters { get => _lengthMeters; set => SetProperty(ref _lengthMeters, value); }

        private string _weightKilograms = string.Empty;
        public string WeightKilograms { get => _weightKilograms; set => SetProperty(ref _weightKilograms, value); }

        public AsyncRelayCommand CalculateWeightCommand { get; }
        public AsyncRelayCommand CalculateLengthCommand { get; }

        public ConverterViewModel(CompositionRoot root)
        {
            _root = root;
            CalculateWeightCommand = new AsyncRelayCommand(CalculateWeightAsync);
            CalculateLengthCommand = new AsyncRelayCommand(CalculateLengthAsync);
        }

        private Task CalculateWeightAsync() => RunAsync(async () =>
        {
            var diameter = ParsePositiveDecimal(DiameterMm, "Diameter");
            var thickness = ParsePositiveDecimal(ThicknessMm, "Thickness");
            var length = ParsePositiveDecimal(LengthMeters, "Length");

            var result = await _root.CalculateWeight.HandleAsync(new CalculateWeightQuery
            {
                DiameterMm = diameter,
                ThicknessMm = thickness,
                LengthMeters = length
            });

            WeightKilograms = result.WeightKilograms.ToString("0.###", CultureInfo.CurrentCulture);
            SetSuccessMessage($"Using {result.KgPerMeterUsed:0.###} kg/m from the catalogue.");
        });

        private Task CalculateLengthAsync() => RunAsync(async () =>
        {
            var diameter = ParsePositiveDecimal(DiameterMm, "Diameter");
            var thickness = ParsePositiveDecimal(ThicknessMm, "Thickness");
            var weight = ParsePositiveDecimal(WeightKilograms, "Weight");

            var result = await _root.CalculateLength.HandleAsync(new CalculateLengthQuery
            {
                DiameterMm = diameter,
                ThicknessMm = thickness,
                WeightKilograms = weight
            });

            LengthMeters = result.LengthMeters.ToString("0.###", CultureInfo.CurrentCulture);
            SetSuccessMessage($"Using {result.KgPerMeterUsed:0.###} kg/m from the catalogue.");
        });

        private static decimal ParsePositiveDecimal(string text, string fieldName)
        {
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value <= 0)
                throw new UseCaseValidationException($"{fieldName} must be a number greater than 0.");
            return value;
        }
    }
}