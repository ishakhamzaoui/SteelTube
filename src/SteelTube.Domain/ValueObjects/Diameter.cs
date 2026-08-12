using System;
using SteelTube.Domain.Common;

namespace SteelTube.Domain.ValueObjects
{
    /// <summary>
    /// Tube diameter in millimeters — part of the material identity
    /// (SAD 9, SRS 2.1). Always strictly positive (SAD 66).
    /// </summary>
    public readonly struct Diameter : IEquatable<Diameter>
    {
        public decimal Millimeters { get; }

        private Diameter(decimal millimeters)
        {
            Millimeters = millimeters;
        }

        public static Diameter FromMillimeters(decimal millimeters)
        {
            Guard.Positive(millimeters, nameof(millimeters));
            return new Diameter(millimeters);
        }

        /// <summary>
        /// Accepts an inch input and normalizes it to millimeters, per
        /// SAD 61/62 ("the normalized value becomes the domain value").
        /// </summary>
        public static Diameter FromInches(decimal inches)
        {
            Guard.Positive(inches, nameof(inches));
            return new Diameter(inches * 25.4m);
        }

        public bool Equals(Diameter other) => Millimeters == other.Millimeters;
        public override bool Equals(object obj) => obj is Diameter other && Equals(other);
        public override int GetHashCode() => Millimeters.GetHashCode();
        public override string ToString() => $"{Millimeters:0.##} mm";

        public static bool operator ==(Diameter left, Diameter right) => left.Equals(right);
        public static bool operator !=(Diameter left, Diameter right) => !left.Equals(right);
    }
}