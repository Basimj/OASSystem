using MediatR;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.StockCounts.Mapping;
using OAS.Contracts.Inventory.Stock;

namespace OAS.Application.Inventory.StockCounts.Queries.GetStockCountLines;

public sealed class GetStockCountLinesQueryHandler(
    IStockCountRepository repository,
    StockCountLineMapper mapper)
    : IRequestHandler<GetStockCountLinesQuery, IReadOnlyList<StockCountLineDto>>
{
    public async Task<IReadOnlyList<StockCountLineDto>> Handle(
        GetStockCountLinesQuery request,
        CancellationToken cancellationToken)
    {
        var lines = await repository.GetLinesAsync(request.StockCountId, cancellationToken);
        return lines.Select(mapper.ToRead).ToArray();
    }
}
