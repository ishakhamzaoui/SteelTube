using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Application.Common;
using SteelTube.Domain.Entities;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Catalogue.AddEntry
{
    public sealed class AddCatalogueEntryCommandHandler
    {
        private readonly IWeightCatalogueRepository _catalogue;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public AddCatalogueEntryCommandHandler(IWeightCatalogueRepository catalogue, IUnitOfWork unitOfWork, IClock clock)
        {
            _catalogue = catalogue;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public Task<AddCatalogueEntryResult> HandleAsync(AddCatalogueEntryCommand command, CancellationToken ct = default) =>
            _unitOfWork.ExecuteInTransactionAsync(txCt => HandleInternalAsync(command, txCt), ct);

        private async Task<AddCatalogueEntryResult> HandleInternalAsync(AddCatalogueEntryCommand command, CancellationToken ct)
        {
            var diameter = Diameter.FromMillimeters(command.DiameterMm);
            var thickness = Thickness.FromMillimeters(command.ThicknessMm);

            var existing = await _catalogue.FindAsync(diameter, thickness, ct);
            if (existing != null)
                throw new UseCaseValidationException(
                    $"A weight conversion for {diameter} \u00d7 {thickness} already exists. Use Update instead.");

            var utcNow = _clock.UtcNow;
            var entry = WeightCatalogueEntry.Create(diameter, thickness, KgPerMeter.FromValue(command.KgPerMeter), utcNow);
            await _catalogue.AddAsync(entry, ct);

            return new AddCatalogueEntryResult { EntryId = entry.Id };
        }
    }
}