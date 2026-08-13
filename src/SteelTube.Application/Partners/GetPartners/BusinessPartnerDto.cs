using System;

namespace SteelTube.Application.Partners.GetPartners
{
    public sealed class BusinessPartnerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsProvider { get; set; }
        public bool IsCustomer { get; set; }
    }
}