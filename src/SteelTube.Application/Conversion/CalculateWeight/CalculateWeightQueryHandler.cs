using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Domain.Exceptions;
using SteelTube.Domain.Services;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Conversion.CalculateWeight
{
    /// <summary>Implements the flow from SAD 15: TubeSpecification -> WeightCatalogueEntry -> KgPerMeter -> conversion.</summary>
    public sealed class CalculateWeightQueryHandler
    {
        private readonly IWeightCatalogueRepository _catalogue;
        private readonly IWeightConversionService _conversion;

        public CalculateWeightQueryHandler(IWeightCatalogueRepository catalogue, IWeightConversionService conversion)
        {
            _catalogue = catalogue;
            _conversion = conversion;
        }

        public async Task<CalculateWeightResult> HandleAsync(CalculateWeightQuery query, CancellationToken ct = default)
        {
            var diameter = Diameter.FromMillimeters(query.DiameterMm);
            var thickness = Thickness.FromMillimeters(query.ThicknessMm);

            var entry = await _catalogue.FindAsync(diameter, thickness, ct);
            if (entry is null)
                throw new BusinessRuleViolationException(
                    $"No weight conversion is configured for {diameter} \u00d7 {thickness}.");

            var weight = _conversion.CalculateWeight(Length.FromMeters(query.LengthMeters), entry.KgPerMeter);

            return new CalculateWeightResult
            {
                WeightKilograms = weight.Kilograms,
                KgPerMeterUsed = entry.KgPerMeter.Value
            };
        }
    }
}