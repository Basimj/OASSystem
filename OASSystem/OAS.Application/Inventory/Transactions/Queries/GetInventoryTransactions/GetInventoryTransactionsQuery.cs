using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;
using DomainStatus = OAS.Domain.Enums.Inventory.InventoryTransactionStatus;
using DomainType = OAS.Domain.Enums.Inventory.InventoryTransactionType;

namespace OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactions;

public sealed record GetInventoryTransactionsQuery(
    PageRequest Request,
    DomainType? TransactionType = null,
    DomainStatus? Status = null,
    Guid? WarehouseId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null)
    : IQuery<PagedResult<InventoryTransactionDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.Transactions.View];
}
