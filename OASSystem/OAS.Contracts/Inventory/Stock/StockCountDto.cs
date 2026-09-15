namespace OAS.Contracts.Inventory.Stock;

public sealed record StockCountDto(
    Guid Id,
    string CountNumber,
    Guid WarehouseId,
    string Status,
    DateOnly CountDate,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    string? ApprovedBy,
    DateTimeOffset? PostedAtUtc,
    string? PostedBy,
    string? Notes);