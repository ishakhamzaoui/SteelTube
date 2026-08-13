namespace SteelTube.Application.Partners.CreatePartner
{
    /// <summary>
    /// Explicit partner creation (SRS 5.3): only Name is mandatory, roles
    /// are optional. For implicit creation from a transaction form (SRS
    /// 5.4), use <c>IBusinessPartnerRepository.GetOrCreateByNameAsync</c>
    /// directly instead — that path deliberately skips a dedicated screen.
    /// </summary>
    public sealed class CreatePartnerCommand
    {
        public string Name { get; set; }
        public bool IsProvider { get; set; }
        public bool IsCustomer { get; set; }
    }
}