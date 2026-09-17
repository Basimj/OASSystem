using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory;

namespace OAS.Contracts.Inventory.Transactions;

public sealed record InventoryTransactionDto(
    Guid Id,
    string TransactionNumber,
    InventoryTransactionType TransactionType,
    Guid? SourceWarehouseId,
    Guid? DestinationWarehouseId,
    InventoryTransactionStatus Status,
    DateTimeOffset TransactionDate,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Reason,
    string? Notes,
    DateTimeOffset? PostedAtUtc,
    string? PostedBy);