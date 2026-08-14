namespace SteelTube.Application.Conversion.CalculateLength
{
    /// <summary>Weight -> Length conversion request (SRS 7.3, SAD 16).</summary>
    public sealed class CalculateLengthQuery
    {
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal WeightKilograms { get; set; }
    }
}