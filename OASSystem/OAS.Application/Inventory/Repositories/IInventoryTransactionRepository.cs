using OAS.Application.Abstractions.Persistence;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Repositories;

public interface IInventoryTransactionRepository : IRepository<InventoryTransaction, Guid>
{
    Task<IReadOnlyList<InventoryTransactionLine>> GetLinesAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);
}
