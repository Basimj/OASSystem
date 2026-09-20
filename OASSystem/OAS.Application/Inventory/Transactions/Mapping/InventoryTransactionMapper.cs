using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Entities.Inventory;
using ContractStatus = OAS.Contracts.Enums.Inventory.InventoryTransactionStatus;
using ContractType = OAS.Contracts.Enums.Inventory.InventoryTransactionType;

namespace OAS.Application.Inventory.Transactions.Mapping;

public sealed class InventoryTransactionMapper
{
    public InventoryTransactionDto ToRead(InventoryTransaction source)
    {
        return new InventoryTransactionDto(
            source.Id,
            source.TransactionNumber,
            (ContractType)(int)source.TransactionType,
            source.SourceWarehouseId,
            source.DestinationWarehouseId,
            (ContractStatus)(int)source.Status,
            source.TransactionDate,
            source.ReferenceType,
            source.ReferenceId,
            source.Reason,
            source.Notes,
            source.PostedAtUtc,
            source.PostedBy);
    }
}
