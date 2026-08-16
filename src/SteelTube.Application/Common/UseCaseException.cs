using System;

namespace SteelTube.Application.Common
{
    /// <summary>
    /// Base type for Application-layer errors, mirroring the error
    /// classification in SAD 51 (Validation / Business / Infrastructure /
    /// Synchronization). The Desktop layer catches these and shows a
    /// friendly message; it never shows a raw stack trace (SAD 52).
    /// </summary>
    public abstract class UseCaseException : Exception
    {
        protected UseCaseException(string message) : base(message) { }
        protected UseCaseException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class UseCaseValidationException : UseCaseException
    {
        public UseCaseValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// SAD 51's "Synchronization Errors" category, kept distinct from
    /// general validation so a future Logging phase (SAD 53, which lists
    /// "Synchronization" as its own log category) can filter on it.
    /// Covers: corrupt/malformed package JSON, an unsupported
    /// formatVersion (SAD 43), or a structurally invalid operation inside
    /// an otherwise-parseable package.
    /// </summary>
    public sealed class SynchronizationException : UseCaseException
    {
        public SynchronizationException(string message) : base(message) { }
        public SynchronizationException(string message, Exception innerException) : base(message, innerException) { }
    }
}