using OAS.Contracts.Enums.Inventory;

namespace OAS.Contracts.Inventory.Stock;

public sealed record StockReservationDto(
    Guid Id,
    Guid ProductVariantId,
    Guid WarehouseId,
    decimal Quantity,
    string SourceModule,
    string SourceDocumentType,
    Guid SourceDocumentId,
    Guid SourceLineId,
    StockReservationStatus Status,
    DateTimeOffset ReservedAtUtc,
    DateTimeOffset? ReleasedAtUtc,
    DateTimeOffset? ConsumedAtUtc,
    bool IsActive,
    string RowVersion);
