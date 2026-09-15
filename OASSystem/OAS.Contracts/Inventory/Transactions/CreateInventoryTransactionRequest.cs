namespace OAS.Contracts.Inventory.Transactions;

public sealed record CreateInventoryTransactionRequest(
    string TransactionNumber,
    string TransactionType,
    Guid? SourceWarehouseId,
    Guid? DestinationWarehouseId,
    DateTimeOffset TransactionDate,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Reason,
    string? Notes,
    IReadOnlyList<CreateInventoryTransactionLineRequest> Lines);