using MediatR;
using OAS.Application.Inventory.Transactions.Commands.CreateInventoryTransaction;
using OAS.Application.Inventory.Transactions.Commands.PostInventoryTransaction;
using OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactionById;
using OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactionLines;
using OAS.Application.Inventory.Transactions.Queries.GetInventoryTransactions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;
using DomainStatus = OAS.Domain.Enums.Inventory.InventoryTransactionStatus;
using DomainType = OAS.Domain.Enums.Inventory.InventoryTransactionType;

namespace OAS.Application.Inventory.Transactions.Services;

public sealed class InventoryTransactionService(ISender sender) : IInventoryTransactionService
{
    public Task<Guid> CreateAsync(CreateInventoryTransactionRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new CreateInventoryTransactionCommand(request), cancellationToken);

    public Task<Guid> PostAsync(Guid transactionId, PostInventoryTransactionRequest? request = null, CancellationToken cancellationToken = default) =>
        sender.Send(new PostInventoryTransactionCommand(transactionId, request), cancellationToken);

    public Task<InventoryTransactionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        sender.Send(new GetInventoryTransactionByIdQuery(id), cancellationToken);

    public Task<IReadOnlyList<InventoryTransactionLineDto>> GetLinesAsync(Guid transactionId, CancellationToken cancellationToken = default) =>
        sender.Send(new GetInventoryTransactionLinesQuery(transactionId), cancellationToken);

    public Task<PagedResult<InventoryTransactionDto>> GetPageAsync(
        PageRequest request,
        DomainType? transactionType = null,
        DomainStatus? status = null,
        Guid? warehouseId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default) =>
        sender.Send(new GetInventoryTransactionsQuery(request, transactionType, status, warehouseId, fromDate, toDate), cancellationToken);
}
