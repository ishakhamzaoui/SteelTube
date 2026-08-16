using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;

namespace SteelTube.Application.Diagnostics.CheckIntegrity
{
    /// <summary>Implements the checks listed in SAD 65: Database, Inventory projection, and Catalogue.</summary>
    public sealed class CheckIntegrityQueryHandler
    {
        private readonly IDataIntegrityChecker _checker;
        private readonly ITubeSpecificationRepository _specifications;
        private readonly IWeightCatalogueRepository _catalogue;
        private readonly IInventoryOperationRepository _operations;
        private readonly IInventoryBalanceRepository _balances;
        private readonly Common.IClock _clock;

        public CheckIntegrityQueryHandler(
            IDataIntegrityChecker checker, ITubeSpecificationRepository specifications, IWeightCatalogueRepository catalogue,
            IInventoryOperationRepository operations, IInventoryBalanceRepository balances, Common.IClock clock)
        {
            _checker = checker;
            _specifications = specifications;
            _catalogue = catalogue;
            _operations = operations;
            _balances = balances;
            _clock = clock;
        }

        public async Task<CheckIntegrityResult> HandleAsync(CheckIntegrityQuery query, CancellationToken ct = default)
        {
            var sqliteOk = await _checker.CheckSqliteIntegrityAsync(ct);
            var fkOk = await _checker.CheckForeignKeyIntegrityAsync(ct);

            var specifications = await _specifications.GetAllAsync(ct);
            var duplicateSpecGroups = specifications
                .GroupBy(s => (s.Diameter.Millimeters, s.Thickness.Millimeters))
                .Count(g => g.Count() > 1);

            var catalogueEntries = await _catalogue.GetAllAsync(ct);
            var duplicateCatalogueGroups = catalogueEntries
                .GroupBy(e => (e.Diameter.Millimeters, e.Thickness.Millimeters))
                .Count(g => g.Count() > 1);

            // SAD 22 -- recompute what every balance *should* be from the
            // operation ledger, without persisting anything, and compare.
            var allOperations = await _operations.GetAllAsync(ct);
            var computedByTubeSpec = allOperations
                .GroupBy(o => o.TubeSpecificationId)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.SignedLengthMeters));

            var actualBalances = (await _balances.GetAllAsync(ct)).ToDictionary(b => b.TubeSpecificationId, b => b.QuantityLengthMeters);
            var specificationsById = specifications.ToDictionary(s => s.Id);

            var mismatches = new List<ProjectionMismatch>();
            foreach (var specification in specifications)
            {
                var computed = computedByTubeSpec.TryGetValue(specification.Id, out var c) ? c : 0m;
                var stored = actualBalances.TryGetValue(specification.Id, out var a) ? a : 0m;

                if (computed != stored)
                {
                    mismatches.Add(new ProjectionMismatch
                    {
                        DiameterMm = specification.Diameter.Millimeters,
                        ThicknessMm = specification.Thickness.Millimeters,
                        StoredQuantityLengthMeters = stored,
                        ComputedQuantityLengthMeters = computed
                    });
                }
            }

            return new CheckIntegrityResult
            {
                CheckedAtUtc = _clock.UtcNow,
                SqliteIntegrityOk = sqliteOk,
                ForeignKeyIntegrityOk = fkOk,
                DuplicateTubeSpecificationGroups = duplicateSpecGroups,
                DuplicateCatalogueEntryGroups = duplicateCatalogueGroups,
                ProjectionMismatches = mismatches,
                TotalOperations = allOperations.Count,
                TotalMaterials = specifications.Count
            };
        }
    }
}