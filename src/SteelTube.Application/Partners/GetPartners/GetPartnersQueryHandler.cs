using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;

namespace SteelTube.Application.Partners.GetPartners
{
    public sealed class GetPartnersQueryHandler
    {
        private readonly IBusinessPartnerRepository _partners;

        public GetPartnersQueryHandler(IBusinessPartnerRepository partners)
        {
            _partners = partners;
        }

        public async Task<IReadOnlyList<BusinessPartnerDto>> HandleAsync(GetPartnersQuery query, CancellationToken ct = default)
        {
            var partners = await _partners.GetAllAsync(ct);
            return partners
                .Select(p => new BusinessPartnerDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsProvider = p.IsProvider,
                    IsCustomer = p.IsCustomer
                })
                .ToList();
        }
    }
}