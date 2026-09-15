namespace OAS.Contracts.Inventory.Transactions;

public sealed record InventoryTransactionDto(
    Guid Id,
    string TransactionNumber,
    string TransactionType,
    Guid? SourceWarehouseId,
    Guid? DestinationWarehouseId,
    string Status,
    DateTimeOffset TransactionDate,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Reason,
    string? Notes,
    DateTimeOffset? PostedAtUtc,
    string? PostedBy);