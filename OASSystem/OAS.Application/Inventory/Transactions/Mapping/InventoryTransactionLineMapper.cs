using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Transactions.Mapping;

public sealed class InventoryTransactionLineMapper
{
    public InventoryTransactionLineDto ToRead(InventoryTransactionLine source)
    {
        return new InventoryTransactionLineDto(
            source.Id,
            source.TransactionId,
            source.ProductVariantId,
            source.Quantity,
            source.UnitCost,
            source.TotalCost,
            source.Notes);
    }
}
