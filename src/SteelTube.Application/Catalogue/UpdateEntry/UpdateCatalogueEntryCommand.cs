namespace SteelTube.Application.Catalogue.UpdateEntry
{
    /// <summary>
    /// Changes the kg/m factor for an existing Diameter + Thickness entry
    /// (SRS 8.2). This never rewrites historical operations — those keep
    /// their own <c>WeightPerMeterUsed</c> snapshot (SAD 17, SRS 8.4).
    /// </summary>
    public sealed class UpdateCatalogueEntryCommand
    {
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal NewKgPerMeter { get; set; }
    }
}