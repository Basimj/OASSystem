using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record CustomerDemandQueueItemDto(
    Guid CustomerOrderLineId,
    Guid ProductVariantId,
    string? ProductCode,
    string ProductName,
    SalesLineType LineType,
    EyeSide? Eye,
    decimal RequestedQuantity,
    decimal ShortageQuantity,
    Guid WarehouseId,
    Guid? PreferredSupplierId,
    DateTimeOffset? ScheduledOrderAtUtc,
    DateOnly? RequiredDate,
    string? OpticalSummary);
