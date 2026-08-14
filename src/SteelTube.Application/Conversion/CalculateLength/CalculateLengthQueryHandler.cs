using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Domain.Exceptions;
using SteelTube.Domain.Services;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Conversion.CalculateLength
{
    /// <summary>Implements the flow from SAD 16: TubeSpecification -> WeightCatalogueEntry -> KgPerMeter -> conversion.</summary>
    public sealed class CalculateLengthQueryHandler
    {
        private readonly IWeightCatalogueRepository _catalogue;
        private readonly IWeightConversionService _conversion;

        public CalculateLengthQueryHandler(IWeightCatalogueRepository catalogue, IWeightConversionService conversion)
        {
            _catalogue = catalogue;
            _conversion = conversion;
        }

        public async Task<CalculateLengthResult> HandleAsync(CalculateLengthQuery query, CancellationToken ct = default)
        {
            var diameter = Diameter.FromMillimeters(query.DiameterMm);
            var thickness = Thickness.FromMillimeters(query.ThicknessMm);

            var entry = await _catalogue.FindAsync(diameter, thickness, ct);
            if (entry is null)
                throw new BusinessRuleViolationException(
                    $"No weight conversion is configured for {diameter} \u00d7 {thickness}.");

            var length = _conversion.CalculateLength(Weight.FromKilograms(query.WeightKilograms), entry.KgPerMeter);

            return new CalculateLengthResult
            {
                LengthMeters = length.Meters,
                KgPerMeterUsed = entry.KgPerMeter.Value
            };
        }
    }
}