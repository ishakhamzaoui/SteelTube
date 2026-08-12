using System;

namespace SteelTube.Application.Common
{
    /// <summary>
    /// Abstraction over "now" so use cases stay testable (used for
    /// InsertedAt / OperationDate defaults, SRS 6.3/6.4).
    /// </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}