using System;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Application.Catalogue.AddEntry;
using SteelTube.Application.Catalogue.GetCatalogue;
using SteelTube.Application.Catalogue.UpdateEntry;
using SteelTube.Application.Common;
using SteelTube.Application.Inventory.AddStock;
using SteelTube.Application.Inventory.GetCurrentStock;
using SteelTube.Application.Inventory.RemoveStock;
using SteelTube.Application.Partners.CreatePartner;
using SteelTube.Application.Partners.GetPartners;
using SteelTube.Domain.Services;
using SteelTube.Infrastructure.Common;
using SteelTube.Infrastructure.Devices;
using SteelTube.Infrastructure.Persistence;
using SteelTube.Infrastructure.Repositories;

namespace SteelTube.Infrastructure
{
    /// <summary>
    /// Manual composition root: builds the single SQLite session, every
    /// repository, and every use case handler the Desktop layer needs.
    ///
    /// SteelTube intentionally has no DI container (SAD Goal 5 —
    /// Operational simplicity; SAD 58 avoids dependency-heavy frameworks on
    /// the constrained hardware target). One object graph, built once at
    /// startup, is enough for an application this size.
    /// </summary>
    public sealed class CompositionRoot : IDisposable
    {
        public SqliteSession Session { get; }
        public IClock Clock { get; }
        public IDeviceContext DeviceContext { get; }
        public IUnitOfWork UnitOfWork { get; }

        public ITubeSpecificationRepository TubeSpecifications { get; }
        public IBusinessPartnerRepository BusinessPartners { get; }
        public IWeightCatalogueRepository WeightCatalogue { get; }
        public IInventoryOperationRepository InventoryOperations { get; }
        public IInventoryBalanceRepository InventoryBalances { get; }
        public IWeightConversionService WeightConversion { get; }

        // Inventory (SAD 18 / 73 Phase 2)
        public AddStockCommandHandler AddStock { get; }
        public RemoveStockCommandHandler RemoveStock { get; }
        public GetCurrentStockQueryHandler GetCurrentStock { get; }

        // Weight Catalogue (SAD 18 / 73 Phase 3)
        public AddCatalogueEntryCommandHandler AddCatalogueEntry { get; }
        public UpdateCatalogueEntryCommandHandler UpdateCatalogueEntry { get; }
        public GetCatalogueQueryHandler GetCatalogue { get; }

        // Business Partners (SAD 18 / 73 Phase 4)
        public CreatePartnerCommandHandler CreatePartner { get; }
        public GetPartnersQueryHandler GetPartners { get; }

        private CompositionRoot(
            SqliteSession session, IClock clock, IDeviceContext deviceContext, IUnitOfWork unitOfWork,
            ITubeSpecificationRepository tubeSpecifications, IBusinessPartnerRepository businessPartners,
            IWeightCatalogueRepository weightCatalogue, IInventoryOperationRepository inventoryOperations,
            IInventoryBalanceRepository inventoryBalances, IWeightConversionService weightConversion)
        {
            Session = session;
            Clock = clock;
            DeviceContext = deviceContext;
            UnitOfWork = unitOfWork;
            TubeSpecifications = tubeSpecifications;
            BusinessPartners = businessPartners;
            WeightCatalogue = weightCatalogue;
            InventoryOperations = inventoryOperations;
            InventoryBalances = inventoryBalances;
            WeightConversion = weightConversion;

            AddStock = new AddStockCommandHandler(
                tubeSpecifications, weightCatalogue, businessPartners, inventoryOperations, inventoryBalances,
                weightConversion, deviceContext, unitOfWork, clock);

            RemoveStock = new RemoveStockCommandHandler(
                tubeSpecifications, weightCatalogue, businessPartners, inventoryOperations, inventoryBalances,
                weightConversion, deviceContext, unitOfWork, clock);

            GetCurrentStock = new GetCurrentStockQueryHandler(
                inventoryBalances, tubeSpecifications, weightCatalogue, weightConversion);

            AddCatalogueEntry = new AddCatalogueEntryCommandHandler(weightCatalogue, unitOfWork, clock);
            UpdateCatalogueEntry = new UpdateCatalogueEntryCommandHandler(weightCatalogue, unitOfWork, clock);
            GetCatalogue = new GetCatalogueQueryHandler(weightCatalogue);

            CreatePartner = new CreatePartnerCommandHandler(businessPartners, unitOfWork, clock);
            GetPartners = new GetPartnersQueryHandler(businessPartners);
        }

        /// <summary>
        /// Opens (or creates) the database at <paramref name="databasePath"/>
        /// -- defaults to <see cref="DatabasePathProvider.GetDefaultPath"/> --
        /// ensures the schema exists (SAD 27), and builds every service.
        /// Call once from App.xaml.cs (OnStartup) and keep the result alive
        /// for the process lifetime; dispose it on shutdown.
        /// </summary>
        public static async Task<CompositionRoot> CreateAsync(string databasePath = null, CancellationToken ct = default)
        {
            var session = new SqliteSession(databasePath ?? DatabasePathProvider.GetDefaultPath());

            try
            {
                await DbInitializer.EnsureCreatedAsync(session.Connection, ct);

                var deviceContext = await SqliteDeviceContext.CreateAsync(session, ct);

                return new CompositionRoot(
                    session,
                    new SystemClock(),
                    deviceContext,
                    new SqliteUnitOfWork(session),
                    new SqliteTubeSpecificationRepository(session),
                    new SqliteBusinessPartnerRepository(session),
                    new SqliteWeightCatalogueRepository(session),
                    new SqliteInventoryOperationRepository(session),
                    new SqliteInventoryBalanceRepository(session),
                    new WeightConversionService());
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        public void Dispose() => Session.Dispose();
    }
}