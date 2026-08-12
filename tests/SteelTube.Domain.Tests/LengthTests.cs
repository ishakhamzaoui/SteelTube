using System;
using FluentAssertions;
using SteelTube.Domain.ValueObjects;
using Xunit;

namespace SteelTube.Domain.Tests
{
    public class LengthTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void FromMeters_rejects_non_positive_values(decimal meters)
        {
            Action act = () => Length.FromMeters(meters);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void FromMeters_accepts_positive_values()
        {
            var length = Length.FromMeters(12.5m);
            length.Meters.Should().Be(12.5m);
        }
    }
}