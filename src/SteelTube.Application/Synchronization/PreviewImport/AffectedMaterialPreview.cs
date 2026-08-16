namespace SteelTube.Application.Synchronization.PreviewImport
{
    /// <summary>One row of the preview from SAD 40 -- a material touched by at least one new operation in the package.</summary>
    public sealed class AffectedMaterialPreview
    {
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal CurrentQuantityLengthMeters { get; set; }
        public decimal DeltaLengthMeters { get; set; }
        public decimal ResultingQuantityLengthMeters { get; set; }
        public bool WouldResultInNegativeStock { get; set; }
    }
}