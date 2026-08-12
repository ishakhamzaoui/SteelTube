using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Domain.Tests
{
    [TestClass]
    public class LengthTests
    {
        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        public void FromMeters_rejects_non_positive_values(decimal meters)
        {
            Action act = () => Length.FromMeters(meters);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void FromMeters_accepts_positive_values()
        {
            var length = Length.FromMeters(12.5m);
            length.Meters.Should().Be(12.5m);
        }
    }
}