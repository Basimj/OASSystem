using Microsoft.EntityFrameworkCore;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence.Repositories.Generic;

namespace OAS.Infrastructure.Persistence.Repositories.Inventory;

public sealed class InventoryTransactionRepository(OasDbContext dbContext)
    : EfRepository<InventoryTransaction, Guid>(dbContext), IInventoryTransactionRepository
{
    public async Task<IReadOnlyList<InventoryTransactionLine>> GetLinesAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.InventoryTransactionLines
            .AsNoTracking()
            .Where(l => l.TransactionId == transactionId)
            .ToListAsync(cancellationToken);
    }
}
