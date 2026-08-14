using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SteelTube.Application.Common;
using SteelTube.Application.Partners.CreatePartner;
using SteelTube.Application.Partners.GetPartners;
using SteelTube.Desktop.Common;
using SteelTube.Infrastructure;

namespace SteelTube.Desktop.ViewModels
{
    /// <summary>SRS 5.3 — explicit partner creation; only Name is mandatory.</summary>
    public sealed class PartnersViewModel : ViewModelBase
    {
        private readonly CompositionRoot _root;

        public ObservableCollection<BusinessPartnerDto> Partners { get; } = new ObservableCollection<BusinessPartnerDto>();

        private string _newName = string.Empty;
        public string NewName { get => _newName; set => SetProperty(ref _newName, value); }

        private bool _newIsProvider;
        public bool NewIsProvider { get => _newIsProvider; set => SetProperty(ref _newIsProvider, value); }

        private bool _newIsCustomer;
        public bool NewIsCustomer { get => _newIsCustomer; set => SetProperty(ref _newIsCustomer, value); }

        public AsyncRelayCommand RefreshCommand { get; }
        public AsyncRelayCommand CreateCommand { get; }

        public PartnersViewModel(CompositionRoot root)
        {
            _root = root;
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            CreateCommand = new AsyncRelayCommand(CreateAsync);
            _ = RefreshAsync();
        }

        private Task RefreshAsync() => RunAsync(async () =>
        {
            var partners = await _root.GetPartners.HandleAsync(new GetPartnersQuery());
            Partners.Clear();
            foreach (var partner in partners)
                Partners.Add(partner);
        });

        private Task CreateAsync() => RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(NewName))
                throw new UseCaseValidationException("Partner name is required.");

            await _root.CreatePartner.HandleAsync(new CreatePartnerCommand
            {
                Name = NewName.Trim(),
                IsProvider = NewIsProvider,
                IsCustomer = NewIsCustomer
            });

            NewName = string.Empty;
            NewIsProvider = false;
            NewIsCustomer = false;

            var partners = await _root.GetPartners.HandleAsync(new GetPartnersQuery());
            Partners.Clear();
            foreach (var partner in partners)
                Partners.Add(partner);

            SetSuccessMessage("Partner created.");
        });
    }
}