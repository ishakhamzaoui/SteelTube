using System;

namespace SteelTube.Domain.Exceptions
{
    /// <summary>
    /// Base type for every exception raised by the Domain layer. The
    /// Application layer is responsible for translating these into
    /// user-friendly results (SAD 51, SAD 52) — the Domain layer never
    /// knows about the UI.
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
    }

    /// <summary>
    /// Raised when a value does not satisfy a basic domain invariant, e.g.
    /// "Thickness must be greater than 0" (SAD 51 — Validation Errors).
    /// </summary>
    public sealed class DomainValidationException : DomainException
    {
        public DomainValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// Raised when a value is individually valid but the requested operation
    /// violates a business rule, e.g. "No weight conversion is configured
    /// for 500 x 10 mm" (SAD 51 — Business Errors, SRS 7.6).
    /// </summary>
    public sealed class BusinessRuleViolationException : DomainException
    {
        public BusinessRuleViolationException(string message) : base(message) { }
    }
}