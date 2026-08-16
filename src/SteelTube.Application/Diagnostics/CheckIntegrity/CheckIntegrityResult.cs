using System;
using System.Collections.Generic;
using System.Linq;

namespace SteelTube.Application.Diagnostics.CheckIntegrity
{
    /// <summary>Mirrors the SAD 65 diagnostic screen: status for Database, Inventory projection, and Catalogue.</summary>
    public sealed class CheckIntegrityResult
    {
        public DateTime CheckedAtUtc { get; set; }

        public bool SqliteIntegrityOk { get; set; }
        public bool ForeignKeyIntegrityOk { get; set; }

        public int DuplicateTubeSpecificationGroups { get; set; }
        public int DuplicateCatalogueEntryGroups { get; set; }

        public IReadOnlyList<ProjectionMismatch> ProjectionMismatches { get; set; } = new List<ProjectionMismatch>();

        public int TotalOperations { get; set; }
        public int TotalMaterials { get; set; }

        public bool IsFullyHealthy =>
            SqliteIntegrityOk && ForeignKeyIntegrityOk &&
            DuplicateTubeSpecificationGroups == 0 && DuplicateCatalogueEntryGroups == 0 &&
            ProjectionMismatches.Count == 0;
    }
}