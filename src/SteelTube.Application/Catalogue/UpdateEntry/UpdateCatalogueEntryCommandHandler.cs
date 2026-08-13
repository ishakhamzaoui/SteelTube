using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Application.Common;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Catalogue.UpdateEntry
{
    public sealed class UpdateCatalogueEntryCommandHandler
    {
        private readonly IWeightCatalogueRepository _catalogue;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public UpdateCatalogueEntryCommandHandler(IWeightCatalogueRepository catalogue, IUnitOfWork unitOfWork, IClock clock)
        {
            _catalogue = catalogue;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public Task HandleAsync(UpdateCatalogueEntryCommand command, CancellationToken ct = default) =>
            _unitOfWork.ExecuteInTransactionAsync(txCt => HandleInternalAsync(command, txCt), ct);

        private async Task HandleInternalAsync(UpdateCatalogueEntryCommand command, CancellationToken ct)
        {
            var diameter = Diameter.FromMillimeters(command.DiameterMm);
            var thickness = Thickness.FromMillimeters(command.ThicknessMm);

            var entry = await _catalogue.FindAsync(diameter, thickness, ct);
            if (entry is null)
                throw new UseCaseValidationException(
                    $"No weight conversion is configured for {diameter} \u00d7 {thickness}. Add it first.");

            entry.UpdateFactor(KgPerMeter.FromValue(command.NewKgPerMeter), _clock.UtcNow);
            await _catalogue.UpdateAsync(entry, ct);
        }
    }
}