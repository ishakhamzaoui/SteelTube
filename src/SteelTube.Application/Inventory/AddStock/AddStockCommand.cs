using System;

namespace SteelTube.Application.Inventory.AddStock
{
    /// <summary>
    /// Input for a Purchase / stock-increase operation (SAD 68, SRS 9.5).
    /// The UI exposes a Length/Weight toggle (SRS 7.4) — exactly one of
    /// <see cref="LengthMeters"/> or <see cref="WeightKilograms"/> should
    /// be supplied as the driving quantity.
    ///
    /// Partner may be supplied either as an existing Id, or as a typed
    /// name for implicit creation (SRS 5.4); both are optional (SRS 5.5).
    /// </summary>
    public sealed class AddStockCommand
    {
        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }

        public decimal? LengthMeters { get; set; }
        public decimal? WeightKilograms { get; set; }

        public int? PieceCount { get; set; }

        public Guid? BusinessPartnerId { get; set; }
        public string BusinessPartnerName { get; set; }

        /// <summary>Advanced field (SRS 6.4). Defaults to today when not supplied.</summary>
        public DateTime? OperationDate { get; set; }

        public string Note { get; set; }
    }
}