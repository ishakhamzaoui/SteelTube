using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Domain.Services;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Inventory.GetCurrentStock
{
    /// <summary>
    /// Reads the materialized projection (SAD 21, SAD 59) rather than
    /// recalculating the full operation history, and enriches each row
    /// with a calculated weight when a catalogue entry exists (SRS 9.4).
    /// </summary>
    public sealed class GetCurrentStockQueryHandler
    {
        private readonly IInventoryBalanceRepository _balances;
        private readonly ITubeSpecificationRepository _specifications;
        private readonly IWeightCatalogueRepository _catalogue;
        private readonly IWeightConversionService _conversion;

        public GetCurrentStockQueryHandler(
            IInventoryBalanceRepository balances,
            ITubeSpecificationRepository specifications,
            IWeightCatalogueRepository catalogue,
            IWeightConversionService conversion)
        {
            _balances = balances;
            _specifications = specifications;
            _catalogue = catalogue;
            _conversion = conversion;
        }

        public async Task<IReadOnlyList<CurrentStockItem>> HandleAsync(GetCurrentStockQuery query, CancellationToken ct = default)
        {
            var balances = await _balances.GetAllAsync(ct);
            var specifications = (await _specifications.GetAllAsync(ct)).ToDictionary(s => s.Id);
            var results = new List<CurrentStockItem>(balances.Count);

            foreach (var balance in balances)
            {
                if (!specifications.TryGetValue(balance.TubeSpecificationId, out var specification))
                    continue;

                var catalogueEntry = await _catalogue.FindAsync(specification.Diameter, specification.Thickness, ct);
                decimal? weightKg = null;
                if (catalogueEntry is not null && balance.QuantityLengthMeters > 0)
                {
                    weightKg = _conversion
                        .CalculateWeight(Length.FromMeters(balance.QuantityLengthMeters), catalogueEntry.KgPerMeter)
                        .Kilograms;
                }

                results.Add(new CurrentStockItem
                {
                    TubeSpecificationId = specification.Id,
                    DiameterMm = specification.Diameter.Millimeters,
                    ThicknessMm = specification.Thickness.Millimeters,
                    QuantityLengthMeters = balance.QuantityLengthMeters,
                    KgPerMeter = catalogueEntry?.KgPerMeter.Value,
                    QuantityWeightKilograms = weightKg,
                    IsNegative = balance.IsNegative
                });
            }

            return results
                .OrderBy(r => r.DiameterMm)
                .ThenBy(r => r.ThicknessMm)
                .ToList();
        }
    }
}