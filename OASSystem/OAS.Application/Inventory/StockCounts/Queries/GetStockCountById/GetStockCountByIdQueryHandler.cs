using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.StockCounts.Mapping;
using OAS.Contracts.Inventory.Stock;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.StockCounts.Queries.GetStockCountById;

public sealed class GetStockCountByIdQueryHandler(
    IStockCountRepository repository,
    StockCountMapper mapper)
    : IRequestHandler<GetStockCountByIdQuery, StockCountDto>
{
    public async Task<StockCountDto> Handle(
        GetStockCountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException(nameof(StockCount), request.Id);

        return mapper.ToRead(entity);
    }
}
