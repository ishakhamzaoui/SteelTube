using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;

namespace SteelTube.Application.Catalogue.GetCatalogue
{
    public sealed class GetCatalogueQueryHandler
    {
        private readonly IWeightCatalogueRepository _catalogue;

        public GetCatalogueQueryHandler(IWeightCatalogueRepository catalogue)
        {
            _catalogue = catalogue;
        }

        public async Task<IReadOnlyList<WeightCatalogueEntryDto>> HandleAsync(GetCatalogueQuery query, CancellationToken ct = default)
        {
            var entries = await _catalogue.GetAllAsync(ct);
            return entries
                .Select(e => new WeightCatalogueEntryDto
                {
                    Id = e.Id,
                    DiameterMm = e.Diameter.Millimeters,
                    ThicknessMm = e.Thickness.Millimeters,
                    KgPerMeter = e.KgPerMeter.Value,
                    UpdatedAt = e.UpdatedAt
                })
                .ToList();
        }
    }
}