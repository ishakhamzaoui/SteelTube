using System;
using SteelTube.Domain.Common;

namespace SteelTube.Domain.ValueObjects
{
    /// <summary>
    /// A weight in kilograms (SAD 61). Like Length, a Weight is always
    /// strictly positive (SAD 66 — "Weight > 0 when used as input").
    /// </summary>
    public readonly struct Weight : IEquatable<Weight>, IComparable<Weight>
    {
        public decimal Kilograms { get; }

        private Weight(decimal kilograms)
        {
            Kilograms = kilograms;
        }

        public static Weight FromKilograms(decimal kilograms)
        {
            Guard.Positive(kilograms, nameof(kilograms));
            return new Weight(kilograms);
        }

        public bool Equals(Weight other) => Kilograms == other.Kilograms;
        public override bool Equals(object obj) => obj is Weight other && Equals(other);
        public override int GetHashCode() => Kilograms.GetHashCode();
        public int CompareTo(Weight other) => Kilograms.CompareTo(other.Kilograms);
        public override string ToString() => $"{Kilograms:0.###} kg";

        public static bool operator ==(Weight left, Weight right) => left.Equals(right);
        public static bool operator !=(Weight left, Weight right) => !left.Equals(right);
    }
}