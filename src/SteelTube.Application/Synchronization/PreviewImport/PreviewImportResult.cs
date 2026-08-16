using System;
using System.Collections.Generic;
using System.Linq;

namespace SteelTube.Application.Synchronization.PreviewImport
{
    /// <summary>Mirrors the SAD 40 preview screen exactly: source, new/known counts, affected materials, negative-stock count.</summary>
    public sealed class PreviewImportResult
    {
        public Guid PackageId { get; set; }
        public Guid SourceDeviceId { get; set; }
        public string SourceDeviceName { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public int NewOperationsCount { get; set; }
        public int AlreadyKnownCount { get; set; }

        public IReadOnlyList<AffectedMaterialPreview> AffectedMaterials { get; set; } = new List<AffectedMaterialPreview>();

        public int AffectedMaterialsCount => AffectedMaterials.Count;
        public int PotentialNegativeStockCount => AffectedMaterials.Count(m => m.WouldResultInNegativeStock);
    }
}