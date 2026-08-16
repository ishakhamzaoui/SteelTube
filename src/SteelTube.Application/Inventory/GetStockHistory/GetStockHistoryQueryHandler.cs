using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteelTube.Application.Abstractions;
using SteelTube.Domain.Enums;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Application.Inventory.GetStockHistory
{
    public sealed class GetStockHistoryQueryHandler
    {
        private readonly IInventoryOperationRepository _operations;
        private readonly ITubeSpecificationRepository _specifications;
        private readonly IBusinessPartnerRepository _partners;

        public GetStockHistoryQueryHandler(
            IInventoryOperationRepository operations, ITubeSpecificationRepository specifications, IBusinessPartnerRepository partners)
        {
            _operations = operations;
            _specifications = specifications;
            _partners = partners;
        }

        public async Task<IReadOnlyList<StockHistoryItem>> HandleAsync(GetStockHistoryQuery query, CancellationToken ct = default)
        {
            var filter = new InventoryOperationFilter { Skip = query.Skip, Take = query.Take };

            if (query.DiameterMm != null && query.ThicknessMm != null)
            {
                var specification = await _specifications.FindAsync(
                    Diameter.FromMillimeters(query.DiameterMm.Value), Thickness.FromMillimeters(query.ThicknessMm.Value), ct);
                if (specification is null)
                    return Array.Empty<StockHistoryItem>(); // No stock has ever existed for this material -- nothing to show, no need to query.
                filter.TubeSpecificationId = specification.Id;
            }

            if (!string.IsNullOrWhiteSpace(query.PartnerName))
            {
                var partner = await _partners.FindByNameAsync(query.PartnerName, ct);
                if (partner is null)
                    return Array.Empty<StockHistoryItem>();
                filter.BusinessPartnerId = partner.Id;
            }

            if (!string.IsNullOrWhiteSpace(query.OperationType) && Enum.TryParse(query.OperationType, out OperationType parsedType))
                filter.OperationType = parsedType;

            filter.OperationDateFrom = query.OperationDateFrom;
            filter.OperationDateTo = query.OperationDateTo;

            var operations = await _operations.GetHistoryAsync(filter, ct);

            var specifications = (await _specifications.GetAllAsync(ct)).ToDictionary(s => s.Id);
            var partners = (await _partners.GetAllAsync(ct)).ToDictionary(p => p.Id);

            var items = new List<StockHistoryItem>(operations.Count);
            foreach (var operation in operations)
            {
                if (!specifications.TryGetValue(operation.TubeSpecificationId, out var specification))
                    continue;

                string partnerName = null;
                if (operation.BusinessPartnerId != null && partners.TryGetValue(operation.BusinessPartnerId.Value, out var partner))
                    partnerName = partner.Name;

                items.Add(new StockHistoryItem
                {
                    OperationId = operation.OperationId,
                    OperationType = operation.OperationType.ToString(),
                    DiameterMm = specification.Diameter.Millimeters,
                    ThicknessMm = specification.Thickness.Millimeters,
                    SignedLengthMeters = operation.SignedLengthMeters,
                    WeightKilograms = operation.Weight?.Kilograms,
                    PieceCount = operation.PieceCount,
                    PartnerName = partnerName,
                    OperationDate = operation.OperationDate,
                    InsertedAt = operation.InsertedAt,
                    Note = operation.Note
                });
            }

            return items;
        }
    }
}