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
}