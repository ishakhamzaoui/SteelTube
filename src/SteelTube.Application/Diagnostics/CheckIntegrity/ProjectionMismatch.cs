namespace SteelTube.Application.Diagnostics.CheckIntegrity
{
    /// <summary>
    /// A material where the materialized InventoryBalance (SAD 21) doesn't
    /// match what summing every InventoryOperation for it produces (SAD
    /// 22). Should never happen through normal use of this codebase --
    /// every stock command updates both inside the same transaction -- but
    /// is exactly what this screen exists to catch after something
    /// abnormal (a crash mid-write on old hardware, manual DB editing, a
    /// bug).
    /// </summary>
    public sealed class ProjectionMismatch
    {
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal StoredQuantityLengthMeters { get; set; }
        public decimal ComputedQuantityLengthMeters { get; set; }
    }
}