using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Application.Common;
using SteelTube.Domain.Entities;

namespace SteelTube.Application.Partners.CreatePartner
{
    public sealed class CreatePartnerCommandHandler
    {
        private readonly IBusinessPartnerRepository _partners;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public CreatePartnerCommandHandler(IBusinessPartnerRepository partners, IUnitOfWork unitOfWork, IClock clock)
        {
            _partners = partners;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public Task<CreatePartnerResult> HandleAsync(CreatePartnerCommand command, CancellationToken ct = default) =>
            _unitOfWork.ExecuteInTransactionAsync(txCt => HandleInternalAsync(command, txCt), ct);

        private async Task<CreatePartnerResult> HandleInternalAsync(CreatePartnerCommand command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
                throw new UseCaseValidationException("Partner name is required.");

            var existing = await _partners.FindByNameAsync(command.Name, ct);
            if (existing != null)
                throw new UseCaseValidationException($"A partner named \"{command.Name}\" already exists.");

            var partner = BusinessPartner.Create(command.Name, command.IsProvider, command.IsCustomer, _clock.UtcNow);
            await _partners.AddAsync(partner, ct);

            return new CreatePartnerResult { PartnerId = partner.Id };
        }
    }
}