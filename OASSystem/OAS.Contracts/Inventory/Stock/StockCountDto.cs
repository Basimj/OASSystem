using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory;

namespace OAS.Contracts.Inventory.Stock;

public sealed record StockCountDto(
    Guid Id,
    string CountNumber,
    Guid WarehouseId,
    StockCountStatus Status,
    DateOnly CountDate,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    string? ApprovedBy,
    DateTimeOffset? PostedAtUtc,
    string? PostedBy,
    string? Notes);