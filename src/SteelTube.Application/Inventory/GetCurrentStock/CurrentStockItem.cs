using System;

namespace SteelTube.Application.Inventory.GetCurrentStock
{
    /// <summary>
    /// A single row of the Current Stock screen (SRS 9.3): material grouped
    /// by Diameter + Thickness, with the aggregate length as the only
    /// quantity shown — individual piece lengths are never surfaced here.
    /// </summary>
    public sealed class CurrentStockItem
    {
        public Guid TubeSpecificationId { get; set; }
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal QuantityLengthMeters { get; set; }
        public decimal? KgPerMeter { get; set; }
        public decimal? QuantityWeightKilograms { get; set; }
        public bool IsNegative { get; set; }
    }
}