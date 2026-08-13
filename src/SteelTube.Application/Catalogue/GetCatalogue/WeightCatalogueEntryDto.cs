using System;

namespace SteelTube.Application.Catalogue.GetCatalogue
{
    public sealed class WeightCatalogueEntryDto
    {
        public Guid Id { get; set; }
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal KgPerMeter { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}