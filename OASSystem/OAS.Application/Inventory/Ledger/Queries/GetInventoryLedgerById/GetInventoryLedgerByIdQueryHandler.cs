using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Ledger.Mapping;
using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Ledger.Queries.GetInventoryLedgerById;

public sealed class GetInventoryLedgerByIdQueryHandler(
    IReadRepository<InventoryLedger, Guid> repository,
    InventoryLedgerMapper mapper)
    : IRequestHandler<GetInventoryLedgerByIdQuery, InventoryLedgerDto>
{
    public async Task<InventoryLedgerDto> Handle(
        GetInventoryLedgerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException(nameof(InventoryLedger), request.Id);

        return mapper.ToRead(entity);
    }
}
