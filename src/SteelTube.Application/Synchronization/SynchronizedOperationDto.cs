using System;

namespace SteelTube.Application.Synchronization
{
    /// <summary>
    /// The wire shape of one operation inside a synchronization package
    /// (SAD 13, SRS 13). Deliberately carries <see cref="DiameterMm"/>/
    /// <see cref="ThicknessMm"/> rather than a TubeSpecificationId, and
    /// <see cref="BusinessPartnerName"/> rather than a BusinessPartnerId --
    /// both IDs are local surrogate keys generated independently per
    /// device (ADR-004/ADR-011), so they carry no meaning on a different
    /// device. Diameter+Thickness and Name are the actual business
    /// identities (SAD 3.1), and are what the importing device resolves
    /// against its own data via the same GetOrCreate pattern AddStock
    /// already uses. OperationId/OriginDeviceId/OriginSequenceNumber are
    /// the one part of this DTO that IS globally meaningful (SAD 28/29)
    /// and must be preserved exactly on import.
    /// </summary>
    public sealed class SynchronizedOperationDto
    {
        public Guid OperationId { get; set; }
        public Guid OriginDeviceId { get; set; }
        public long OriginSequenceNumber { get; set; }
        public string OperationType { get; set; }

        public decimal DiameterMm { get; set; }
        public decimal ThicknessMm { get; set; }
        public decimal LengthMeters { get; set; }
        public decimal? WeightKilograms { get; set; }
        public decimal? WeightPerMeterUsed { get; set; }
        public int? PieceCount { get; set; }

        public string BusinessPartnerName { get; set; }

        public DateTime OperationDate { get; set; }
        public DateTime InsertedAt { get; set; }
        public string Note { get; set; }
    }
}