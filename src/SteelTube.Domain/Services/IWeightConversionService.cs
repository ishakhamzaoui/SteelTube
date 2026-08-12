using SteelTube.Domain.ValueObjects;

namespace SteelTube.Domain.Services
{
    /// <summary>
    /// Deterministic conversion between Length and Weight through a known
    /// kg/m factor (SAD 14, SRS 7.1, SRS Rule 5). This service performs
    /// only the arithmetic — looking the kg/m factor up from the catalogue
    /// is an Application-layer concern (SAD 14: "catalogue lookup is
    /// separate from the mathematical calculation").
    /// </summary>
    public interface IWeightConversionService
    {
        Weight CalculateWeight(Length length, KgPerMeter kgPerMeter);
        Length CalculateLength(Weight weight, KgPerMeter kgPerMeter);
    }
}