using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Transactions.Mapping;
using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactionById;

public sealed class GetInventoryTransactionByIdQueryHandler(
    IInventoryTransactionRepository repository,
    InventoryTransactionMapper mapper)
    : IRequestHandler<GetInventoryTransactionByIdQuery, InventoryTransactionDto>
{
    public async Task<InventoryTransactionDto> Handle(
        GetInventoryTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException(nameof(InventoryTransaction), request.Id);

        return mapper.ToRead(entity);
    }
}
