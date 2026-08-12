using System;

namespace SteelTube.Application.Inventory.RemoveStock
{
    public sealed class RemoveStockResult
    {
        public Guid OperationId { get; set; }
        public Guid TubeSpecificationId { get; set; }
        public decimal ResultingStockLengthMeters { get; set; }
        public decimal? CalculatedWeightKilograms { get; set; }
        public decimal? CalculatedLengthMeters { get; set; }

        /// <summary>
        /// True when this sale drove stock below zero. The operation is
        /// still preserved as-is (SAD 37, SAD 38) — the UI is expected to
        /// surface this as a discrepancy warning, not block the sale.
        /// </summary>
        public bool ResultsInNegativeStock { get; set; }
    }
}