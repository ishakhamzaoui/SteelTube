using System;

namespace SteelTube.Application.Inventory.AddStock
{
    public sealed class AddStockResult
    {
        public Guid OperationId { get; set; }
        public Guid TubeSpecificationId { get; set; }
        public decimal ResultingStockLengthMeters { get; set; }
        public decimal? CalculatedWeightKilograms { get; set; }
        public decimal? CalculatedLengthMeters { get; set; }
    }
}