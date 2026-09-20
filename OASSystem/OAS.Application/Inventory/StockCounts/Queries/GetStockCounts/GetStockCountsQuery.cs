using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;
using DomainStatus = OAS.Domain.Enums.Inventory.StockCountStatus;

namespace OAS.Application.Inventory.StockCounts.Queries.GetStockCounts;

public sealed record GetStockCountsQuery(
    PageRequest Request,
    Guid? WarehouseId = null,
    DomainStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null)
    : IQuery<PagedResult<StockCountDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.StockCounts.View];
}
