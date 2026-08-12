using System;
using SteelTube.Domain.Common;

namespace SteelTube.Domain.ValueObjects
{
    /// <summary>
    /// A mass-per-length conversion factor as stored in the Weight
    /// Catalogue (SAD 13, SRS 8). Always strictly positive (SAD 66).
    /// </summary>
    public readonly struct KgPerMeter : IEquatable<KgPerMeter>
    {
        public decimal Value { get; }

        private KgPerMeter(decimal value)
        {
            Value = value;
        }

        public static KgPerMeter FromValue(decimal value)
        {
            Guard.Positive(value, nameof(value));
            return new KgPerMeter(value);
        }

        public bool Equals(KgPerMeter other) => Value == other.Value;
        public override bool Equals(object obj) => obj is KgPerMeter other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"{Value:0.###} kg/m";

        public static bool operator ==(KgPerMeter left, KgPerMeter right) => left.Equals(right);
        public static bool operator !=(KgPerMeter left, KgPerMeter right) => !left.Equals(right);
    }
}