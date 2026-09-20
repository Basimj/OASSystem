using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Transactions;
using DomainStatus = OAS.Domain.Enums.Inventory.InventoryTransactionStatus;
using DomainType = OAS.Domain.Enums.Inventory.InventoryTransactionType;

namespace OAS.Application.Inventory.Transactions.Services;

public interface IInventoryTransactionService
{
    Task<Guid> CreateAsync(CreateInventoryTransactionRequest request, CancellationToken cancellationToken = default);
    Task<Guid> PostAsync(Guid transactionId, PostInventoryTransactionRequest? request = null, CancellationToken cancellationToken = default);
    Task<InventoryTransactionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryTransactionLineDto>> GetLinesAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<PagedResult<InventoryTransactionDto>> GetPageAsync(
        PageRequest request,
        DomainType? transactionType = null,
        DomainStatus? status = null,
        Guid? warehouseId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
}
