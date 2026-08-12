using System;

namespace SteelTube.Domain.Common
{
    /// <summary>
    /// Small set of guard clauses used across the domain layer to enforce
    /// invariants as close to value creation as possible (SAD 3.1, SAD 66).
    /// </summary>
    internal static class Guard
    {
        public static void Positive(decimal value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be greater than 0.");
        }

        public static void NotNegative(int value, string paramName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must not be negative.");
        }

        public static void NotNullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} must not be empty.", paramName);
        }

        public static void NotEmpty(Guid value, string paramName)
        {
            if (value == Guid.Empty)
                throw new ArgumentException($"{paramName} must not be an empty GUID.", paramName);
        }
    }
}