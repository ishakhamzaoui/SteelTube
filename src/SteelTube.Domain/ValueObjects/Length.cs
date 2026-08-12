using System;
using SteelTube.Domain.Common;

namespace SteelTube.Domain.ValueObjects
{
    /// <summary>
    /// A length in meters (SAD 61 — internal unit is meters). A Length is
    /// always strictly positive: it represents the magnitude of a stock
    /// movement, not its direction. Direction is expressed separately by
    /// OperationType (SAD 67, SRS Rule 2).
    /// </summary>
    public readonly struct Length : IEquatable<Length>, IComparable<Length>
    {
        public decimal Meters { get; }

        private Length(decimal meters)
        {
            Meters = meters;
        }

        public static Length FromMeters(decimal meters)
        {
            Guard.Positive(meters, nameof(meters));
            return new Length(meters);
        }

        public bool Equals(Length other) => Meters == other.Meters;
        public override bool Equals(object obj) => obj is Length other && Equals(other);
        public override int GetHashCode() => Meters.GetHashCode();
        public int CompareTo(Length other) => Meters.CompareTo(other.Meters);
        public override string ToString() => $"{Meters:0.###} m";

        public static bool operator ==(Length left, Length right) => left.Equals(right);
        public static bool operator !=(Length left, Length right) => !left.Equals(right);
        public static bool operator <(Length left, Length right) => left.CompareTo(right) < 0;
        public static bool operator >(Length left, Length right) => left.CompareTo(right) > 0;
    }
}