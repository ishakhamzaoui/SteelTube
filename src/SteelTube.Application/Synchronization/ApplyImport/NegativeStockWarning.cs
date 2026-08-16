namespace SteelTube.Application.Synchronization.ApplyImport
{
    /// <summary>SAD 37/38 -- a material whose stock went negative as a result of this import. Never blocks the import; only flags it for review.</summary>
    public sealed class NegativeStockWarning
    {
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal ResultingQuantityLengthMeters { get; set; }
    }
}