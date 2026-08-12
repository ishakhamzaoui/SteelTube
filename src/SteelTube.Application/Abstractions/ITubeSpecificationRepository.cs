using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Domain.Entities;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// Persistence abstraction for tube material identities (SAD 25).
    /// Diameter + Thickness is unique (SAD 9.2), so lookups are keyed by
    /// that pair rather than by Id wherever the caller only has the user's
    /// Diameter/Thickness input.
    /// </summary>
    public interface ITubeSpecificationRepository
    {
        Task<TubeSpecification> FindAsync(Diameter diameter, Thickness thickness, CancellationToken ct = default);

        Task<TubeSpecification> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Returns the existing specification for Diameter + Thickness, or
        /// creates and persists a new one. Used by stock commands, since
        /// the user enters Diameter/Thickness directly rather than
        /// pre-selecting a known material (SRS 6.2).
        /// </summary>
        Task<TubeSpecification> GetOrCreateAsync(Diameter diameter, Thickness thickness, DateTime utcNow, CancellationToken ct = default);

        Task<IReadOnlyList<TubeSpecification>> GetAllAsync(CancellationToken ct = default);
    }
}