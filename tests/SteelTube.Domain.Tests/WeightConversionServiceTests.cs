using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteelTube.Domain.Services;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Domain.Tests
{
    // Values taken directly from SRS 7.2 / 7.3 worked examples.
    [TestClass]
    public class WeightConversionServiceTests
    {
        private readonly IWeightConversionService _sut = new WeightConversionService();

        [TestMethod]
        public void CalculateWeight_matches_SRS_example()
        {
            var length = Length.FromMeters(50m);
            var kgPerMeter = KgPerMeter.FromValue(120.4m);

            var weight = _sut.CalculateWeight(length, kgPerMeter);

            weight.Kilograms.Should().Be(6020.0m);
        }

        [TestMethod]
        public void CalculateLength_matches_SRS_example()
        {
            var weight = Weight.FromKilograms(6020m);
            var kgPerMeter = KgPerMeter.FromValue(120.4m);

            var length = _sut.CalculateLength(weight, kgPerMeter);

            length.Meters.Should().Be(50m);
        }
    }
}