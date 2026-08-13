namespace SteelTube.Application.Catalogue.AddEntry
{
    /// <summary>
    /// Adds a new Diameter + Thickness -> kg/m conversion factor to the
    /// catalogue (SRS 8.2). Diameter + Thickness must not already have an
    /// entry (SRS 8.3, SAD 13.2) — use
    /// <see cref="SteelTube.Application.Catalogue.UpdateEntry.UpdateCatalogueEntryCommand"/>
    /// to change an existing one instead.
    /// </summary>
    public sealed class AddCatalogueEntryCommand
    {
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal KgPerMeter { get; set; }
    }
}