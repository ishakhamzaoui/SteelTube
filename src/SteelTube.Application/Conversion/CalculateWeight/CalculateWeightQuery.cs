namespace SteelTube.Application.Conversion.CalculateWeight
{
    /// <summary>Length -> Weight conversion request (SRS 7.2, SAD 15).</summary>
    public sealed class CalculateWeightQuery
    {
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal LengthMeters { get; set; }
    }
}