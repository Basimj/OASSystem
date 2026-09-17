using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory;

namespace OAS.Contracts.Inventory.Transactions;

public sealed record CreateInventoryTransactionRequest(
    string TransactionNumber,
    InventoryTransactionType TransactionType,
    Guid? SourceWarehouseId,
    Guid? DestinationWarehouseId,
    DateTimeOffset TransactionDate,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Reason,
    string? Notes,
    IReadOnlyList<CreateInventoryTransactionLineRequest> Lines);