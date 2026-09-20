using MediatR;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Transactions.Mapping;
using OAS.Application.Inventory.Transactions.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;

namespace OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactions;

public sealed class GetInventoryTransactionsQueryHandler(
    IInventoryTransactionRepository repository,
    InventoryTransactionMapper mapper)
    : IRequestHandler<GetInventoryTransactionsQuery, PagedResult<InventoryTransactionDto>>
{
    public async Task<PagedResult<InventoryTransactionDto>> Handle(
        GetInventoryTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = InventoryTransactionSpecification.Create(
            normalized,
            request.TransactionType,
            request.Status,
            request.WarehouseId,
            request.FromDate,
            request.ToDate);

        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<InventoryTransactionDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
