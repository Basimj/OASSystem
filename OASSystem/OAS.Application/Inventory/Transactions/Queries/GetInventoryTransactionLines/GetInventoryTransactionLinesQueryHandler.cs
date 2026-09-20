using MediatR;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Transactions.Mapping;
using OAS.Contracts.Inventory.Transactions;

namespace OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactionLines;

public sealed class GetInventoryTransactionLinesQueryHandler(
    IInventoryTransactionRepository repository,
    InventoryTransactionLineMapper mapper)
    : IRequestHandler<GetInventoryTransactionLinesQuery, IReadOnlyList<InventoryTransactionLineDto>>
{
    public async Task<IReadOnlyList<InventoryTransactionLineDto>> Handle(
        GetInventoryTransactionLinesQuery request,
        CancellationToken cancellationToken)
    {
        var lines = await repository.GetLinesAsync(request.TransactionId, cancellationToken);
        return lines.Select(mapper.ToRead).ToArray();
    }
}
