using SteelTube.Domain.ValueObjects;

namespace SteelTube.Domain.Services
{
    /// <inheritdoc cref="IWeightConversionService"/>
    public sealed class WeightConversionService : IWeightConversionService
    {
        public Weight CalculateWeight(Length length, KgPerMeter kgPerMeter) =>
            Weight.FromKilograms(length.Meters * kgPerMeter.Value);

        public Length CalculateLength(Weight weight, KgPerMeter kgPerMeter) =>
            Length.FromMeters(weight.Kilograms / kgPerMeter.Value);
    }
}