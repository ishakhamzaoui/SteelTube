using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Application.Common;
using SteelTube.Domain.Entities;
using SteelTube.Domain.Exceptions;
using SteelTube.Domain.Services;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Inventory.RemoveStock
{
    /// <summary>
    /// Implements the Sale flow from SAD 20 and SAD 68. Negative resulting
    /// stock is a business-validity concern, not a technical one (SAD 37):
    /// the operation is still recorded, and the result simply flags it for
    /// the user to review.
    /// </summary>
    public sealed class RemoveStockCommandHandler
    {
        private readonly ITubeSpecificationRepository _specifications;
        private readonly IWeightCatalogueRepository _catalogue;
        private readonly IBusinessPartnerRepository _partners;
        private readonly IInventoryOperationRepository _operations;
        private readonly IInventoryBalanceRepository _balances;
        private readonly IWeightConversionService _conversion;
        private readonly IDeviceContext _device;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public RemoveStockCommandHandler(
            ITubeSpecificationRepository specifications,
            IWeightCatalogueRepository catalogue,
            IBusinessPartnerRepository partners,
            IInventoryOperationRepository operations,
            IInventoryBalanceRepository balances,
            IWeightConversionService conversion,
            IDeviceContext device,
            IUnitOfWork unitOfWork,
            IClock clock)
        {
            _specifications = specifications;
            _catalogue = catalogue;
            _partners = partners;
            _operations = operations;
            _balances = balances;
            _conversion = conversion;
            _device = device;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public Task<RemoveStockResult> HandleAsync(RemoveStockCommand command, CancellationToken ct = default) =>
            _unitOfWork.ExecuteInTransactionAsync(txCt => HandleInternalAsync(command, txCt), ct);

        private async Task<RemoveStockResult> HandleInternalAsync(RemoveStockCommand command, CancellationToken ct)
        {
            if (command.LengthMeters is null && command.WeightKilograms is null)
                throw new UseCaseValidationException("Either a length or a weight must be provided.");
            if (command.LengthMeters is not null && command.WeightKilograms is not null)
                throw new UseCaseValidationException("Provide either a length or a weight, not both.");

            var utcNow = _clock.UtcNow;
            var diameter = Diameter.FromMillimeters(command.DiameterMm);
            var thickness = Thickness.FromMillimeters(command.ThicknessMm);

            var specification = await _specifications.GetOrCreateAsync(diameter, thickness, utcNow, ct);
            var catalogueEntry = await _catalogue.FindAsync(diameter, thickness, ct);

            Length length;
            Weight? weight = null;
            KgPerMeter? weightPerMeterUsed = null;

            if (command.LengthMeters is not null)
            {
                length = Length.FromMeters(command.LengthMeters.Value);
                if (catalogueEntry is not null)
                {
                    weightPerMeterUsed = catalogueEntry.KgPerMeter;
                    weight = _conversion.CalculateWeight(length, catalogueEntry.KgPerMeter);
                }
            }
            else
            {
                if (catalogueEntry is null)
                    throw new BusinessRuleViolationException(
                        $"No weight conversion is configured for {specification.DisplayName}.");

                weightPerMeterUsed = catalogueEntry.KgPerMeter;
                weight = Weight.FromKilograms(command.WeightKilograms.Value);
                length = _conversion.CalculateLength(weight.Value, catalogueEntry.KgPerMeter);
            }

            var partnerId = await ResolvePartnerAsync(command, utcNow, ct);
            var sequenceNumber = await _device.NextSequenceNumberAsync(ct);

            var operation = InventoryOperation.CreateSale(
                specification.Id, length, weight, weightPerMeterUsed, command.PieceCount, partnerId,
                command.OperationDate ?? utcNow, utcNow, _device.DeviceId, sequenceNumber, command.Note);

            await _operations.AddAsync(operation, ct);

            var balance = await _balances.GetAsync(specification.Id, ct)
                          ?? InventoryBalance.Create(specification.Id, utcNow);
            balance.Apply(operation.SignedLengthMeters, utcNow);
            await _balances.UpsertAsync(balance, ct);

            return new RemoveStockResult
            {
                OperationId = operation.OperationId,
                TubeSpecificationId = specification.Id,
                ResultingStockLengthMeters = balance.QuantityLengthMeters,
                CalculatedWeightKilograms = weight?.Kilograms,
                CalculatedLengthMeters = command.WeightKilograms is not null ? length.Meters : (decimal?)null,
                ResultsInNegativeStock = balance.IsNegative
            };
        }

        private async Task<System.Guid?> ResolvePartnerAsync(RemoveStockCommand command, System.DateTime utcNow, CancellationToken ct)
        {
            if (command.BusinessPartnerId is not null)
                return command.BusinessPartnerId;

            if (!string.IsNullOrWhiteSpace(command.BusinessPartnerName))
            {
                var partner = await _partners.GetOrCreateByNameAsync(command.BusinessPartnerName, utcNow, ct);
                return partner.Id;
            }

            return null;
        }
    }
}