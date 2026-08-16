using System;

namespace SteelTube.Application.Inventory.GetStockHistory
{
    /// <summary>
    /// SRS 10.3 filtering: Material, Partner, Operation type, and a date
    /// range. Diameter/Thickness and PartnerName are accepted as typed by
    /// the user rather than as raw local IDs -- the same business-identity
    /// principle used throughout (SAD 3.1) -- and resolved read-only
    /// inside the handler.
    /// </summary>
    public sealed class GetStockHistoryQuery
    {
        public decimal? DiameterMm { get; set; }
        public decimal? ThicknessMm { get; set; }
        public string PartnerName { get; set; }

        /// <summary>One of Purchase/Sale/AdjustmentIncrease/AdjustmentDecrease, or null for all types.</summary>
        public string OperationType { get; set; }

        public DateTime? OperationDateFrom { get; set; }
        public DateTime? OperationDateTo { get; set; }

        public int Skip { get; set; }
        public int Take { get; set; } = 200;
    }
}