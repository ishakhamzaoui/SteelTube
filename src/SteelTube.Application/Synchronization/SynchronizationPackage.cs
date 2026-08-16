using System;
using System.Collections.Generic;

namespace SteelTube.Application.Synchronization
{
    /// <summary>
    /// The full synchronization JSON package (SAD 13, SAD 31-32). Exchanged
    /// between devices via file transfer -- USB drive, shared folder, etc.
    /// (SRS 12.1) -- there is no network transport involved.
    /// </summary>
    public sealed class SynchronizationPackage
    {
        /// <summary>The only format this build knows how to read/write (SAD 43). Bump when the shape changes.</summary>
        public const int CurrentFormatVersion = 1;

        public int FormatVersion { get; set; } = CurrentFormatVersion;
        public Guid PackageId { get; set; }
        public Guid SourceDeviceId { get; set; }
        public string SourceDeviceName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<SynchronizedOperationDto> Operations { get; set; } = new List<SynchronizedOperationDto>();
    }
}