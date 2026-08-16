using System;

namespace SteelTube.Application.Inventory.GetStockHistory
{
    /// <summary>One row of the History screen (SRS 10.1/10.2).</summary>
    public sealed class StockHistoryItem
    {
        public Guid OperationId { get; set; }
        public string OperationType { get; set; }
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }

        /// <summary>Signed per SAD 67 -- positive for Purchase/AdjustmentIncrease, negative for Sale/AdjustmentDecrease.</summary>
        public decimal SignedLengthMeters { get; set; }

        public decimal? WeightKilograms { get; set; }
        public int? PieceCount { get; set; }
        public string PartnerName { get; set; }
        public DateTime OperationDate { get; set; }
        public DateTime InsertedAt { get; set; }
        public string Note { get; set; }
    }
}