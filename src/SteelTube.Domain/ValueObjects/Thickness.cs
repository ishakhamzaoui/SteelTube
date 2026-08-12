using System;
using SteelTube.Domain.Common;

namespace SteelTube.Domain.ValueObjects
{
    /// <summary>
    /// Tube wall thickness in millimeters — part of the material identity
    /// (SAD 9, SRS 2.1). Always strictly positive (SAD 66).
    /// </summary>
    public readonly struct Thickness : IEquatable<Thickness>
    {
        public decimal Millimeters { get; }

        private Thickness(decimal millimeters)
        {
            Millimeters = millimeters;
        }

        public static Thickness FromMillimeters(decimal millimeters)
        {
            Guard.Positive(millimeters, nameof(millimeters));
            return new Thickness(millimeters);
        }

        public static Thickness FromInches(decimal inches)
        {
            Guard.Positive(inches, nameof(inches));
            return new Thickness(inches * 25.4m);
        }

        public bool Equals(Thickness other) => Millimeters == other.Millimeters;
        public override bool Equals(object obj) => obj is Thickness other && Equals(other);
        public override int GetHashCode() => Millimeters.GetHashCode();
        public override string ToString() => $"{Millimeters:0.##} mm";

        public static bool operator ==(Thickness left, Thickness right) => left.Equals(right);
        public static bool operator !=(Thickness left, Thickness right) => !left.Equals(right);
    }
}