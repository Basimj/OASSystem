using MediatR;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.StockCounts.Mapping;
using OAS.Application.Inventory.StockCounts.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.StockCounts.Queries.GetStockCounts;

public sealed class GetStockCountsQueryHandler(
    IStockCountRepository repository,
    StockCountMapper mapper)
    : IRequestHandler<GetStockCountsQuery, PagedResult<StockCountDto>>
{
    public async Task<PagedResult<StockCountDto>> Handle(
        GetStockCountsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = StockCountSpecification.Create(
            normalized,
            request.WarehouseId,
            request.Status,
            request.FromDate,
            request.ToDate);

        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<StockCountDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
