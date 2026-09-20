using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.Balances.Queries.GetInventoryBalances;

public sealed record GetInventoryBalancesQuery(
    PageRequest Request,
    Guid? WarehouseId = null,
    Guid? ProductVariantId = null)
    : IQuery<PagedResult<InventoryBalanceDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Balances.View];
}
