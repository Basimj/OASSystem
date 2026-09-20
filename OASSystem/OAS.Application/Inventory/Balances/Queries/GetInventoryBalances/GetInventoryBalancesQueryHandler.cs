using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Inventory.Balances.Mapping;
using OAS.Application.Inventory.Balances.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Balances.Queries.GetInventoryBalances;

public sealed class GetInventoryBalancesQueryHandler(
    IReadRepository<InventoryBalance, Guid> repository,
    InventoryBalanceMapper mapper)
    : IRequestHandler<GetInventoryBalancesQuery, PagedResult<InventoryBalanceDto>>
{
    public async Task<PagedResult<InventoryBalanceDto>> Handle(
        GetInventoryBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = InventoryBalanceSpecification.Create(
            normalized,
            request.WarehouseId,
            request.ProductVariantId);

        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<InventoryBalanceDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
