using System.Collections.Generic;

namespace SteelTube.Application.Synchronization.ApplyImport
{
    public sealed class ApplyImportResult
    {
        public int NewOperationsInserted { get; set; }
        public int AlreadyKnownSkipped { get; set; }
        public IReadOnlyList<NegativeStockWarning> NegativeStockWarnings { get; set; } = new List<NegativeStockWarning>();
    }
}