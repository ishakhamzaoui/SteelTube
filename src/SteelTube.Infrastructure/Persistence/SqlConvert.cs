using System;
using System.Globalization;

namespace SteelTube.Infrastructure.Persistence
{
    /// <summary>
    /// Centralizes how domain primitives are read from / written to SQLite.
    ///
    /// Decimals (length, weight, diameter, thickness, kg/m) are stored as
    /// TEXT using invariant-culture formatting rather than SQLite's native
    /// REAL (a binary double), because decimal -> string -> decimal is
    /// exact for <see cref="decimal"/> while a double round-trip is not
    /// (SAD 60 — decimal precision must be preserved). Guids and dates are
    /// also stored as TEXT for the same "no silent precision loss, human
    /// readable for diagnostics" reasoning as SAD 42/53.
    /// </summary>
    internal static class SqlConvert
    {
        public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";

        public static string ToText(decimal value) => value.ToString(CultureInfo.InvariantCulture);

        public static decimal ToDecimal(object value) => decimal.Parse((string)value, CultureInfo.InvariantCulture);

        public static decimal? ToNullableDecimal(object value) =>
            value is null || value is DBNull ? (decimal?)null : ToDecimal(value);

        public static string ToText(Guid value) => value.ToString();

        public static Guid ToGuid(object value) => Guid.Parse((string)value);

        public static Guid? ToNullableGuid(object value) =>
            value is null || value is DBNull ? (Guid?)null : ToGuid(value);

        public static string ToText(DateTime value) =>
            value.ToUniversalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture);

        public static DateTime ToDateTime(object value) =>
            DateTime.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        public static object ToParam(object value) => value ?? DBNull.Value;

        /// <summary>
        /// Used only for the Diameter/Thickness lookup WHERE clause
        /// (finding an existing TubeSpecification / WeightCatalogueEntry).
        /// Comparing as REAL is a deliberate, narrow exception to the
        /// "decimal as TEXT" rule above: it only affects equality lookup
        /// for physical dimensions with a handful of decimal places, never
        /// the stored value or any arithmetic (SAD 60 still holds for
        /// everything that is calculated or persisted).
        /// </summary>
        public static double ToLookupDouble(decimal value) => (double)value;
    }
}