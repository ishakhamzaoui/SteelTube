using System;

namespace SteelTube.Application.Synchronization.Export
{
    public sealed class ExportOperationsResult
    {
        /// <summary>The serialized package (SAD 42) -- the caller (Desktop) decides where to save it.</summary>
        public string PackageJson { get; set; }

        /// <summary>A reasonable default file name, e.g. "SteelTube-Sync-WarehousePC-20260814-153000.json".</summary>
        public string SuggestedFileName { get; set; }

        public Guid PackageId { get; set; }
        public int OperationCount { get; set; }
    }
}