using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Balances.Mapping;
using OAS.Contracts.Inventory.Stock;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Balances.Queries.GetInventoryBalanceById;

public sealed class GetInventoryBalanceByIdQueryHandler(
    IReadRepository<InventoryBalance, Guid> repository,
    InventoryBalanceMapper mapper)
    : IRequestHandler<GetInventoryBalanceByIdQuery, InventoryBalanceDto>
{
    public async Task<InventoryBalanceDto> Handle(
        GetInventoryBalanceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException(nameof(InventoryBalance), request.Id);

        return mapper.ToRead(entity);
    }
}
