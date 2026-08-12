using System;

namespace SteelTube.Application.Inventory.RemoveStock
{
    /// <summary>
    /// Input for a Sale / stock-decrease operation (SRS 4.3, SRS 9.5).
    /// See <see cref="SteelTube.Application.Inventory.AddStock.AddStockCommand"/>
    /// for the Length/Weight toggle and partner-resolution rules, which are
    /// identical here.
    /// </summary>
    public sealed class RemoveStockCommand
    {
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }

        public decimal? LengthMeters { get; set; }
        public decimal? WeightKilograms { get; set; }

        public int? PieceCount { get; set; }

        public Guid? BusinessPartnerId { get; set; }
        public string BusinessPartnerName { get; set; }

        public DateTime? OperationDate { get; set; }

        public string Note { get; set; }
    }
}