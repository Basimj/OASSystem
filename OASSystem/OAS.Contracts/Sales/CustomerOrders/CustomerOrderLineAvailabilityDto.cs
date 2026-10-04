namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record CustomerOrderLineAvailabilityDto(
    Guid CustomerOrderLineId,
    Guid ProductVariantId,
    Guid WarehouseId,
    decimal RequestedQuantity,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal AlreadyReservedForThisOrder,
    decimal ShortageQuantity,
    string AvailabilityStatus);
