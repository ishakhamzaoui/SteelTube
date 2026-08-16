using System.Threading;
using System.Threading.Tasks;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// The two checks that genuinely need direct SQLite access (SAD 65):
    /// a full-database integrity check and a foreign-key consistency
    /// check. Everything else SAD 65 asks for (catalogue uniqueness,
    /// operation identity uniqueness, and the InventoryBalance projection
    /// -- SAD 22) is ordinary business logic over data the existing
    /// repositories already expose, so it lives in
    /// CheckIntegrityQueryHandler instead of duplicating it here.
    /// </summary>
    public interface IDataIntegrityChecker
    {
        /// <summary>Runs SQLite's own <c>PRAGMA integrity_check</c>.</summary>
        Task<bool> CheckSqliteIntegrityAsync(CancellationToken ct = default);

        /// <summary>Runs <c>PRAGMA foreign_key_check</c> -- true when it reports zero violations.</summary>
        Task<bool> CheckForeignKeyIntegrityAsync(CancellationToken ct = default);
    }
}