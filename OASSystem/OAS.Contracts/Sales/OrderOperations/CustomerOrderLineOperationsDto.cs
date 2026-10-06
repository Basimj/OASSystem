using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OrderOperations;

public sealed record CustomerOrderLineOperationsDto(
    Guid CustomerOrderLineId,
    int LineNumber,
    SalesLineType LineType,
    Guid? ProductVariantId,
    string? ProductCode,
    string ProductName,
    EyeSide? Eye,
    decimal Quantity,
    Guid? WarehouseId,
    string? WarehouseName,
    CustomerOrderLineAvailabilityState Availability,
    decimal ReservedQuantity,
    decimal ShortageQuantity,
    CustomerOrderSupplyStatus ProcurementStatus,
    Guid? SupplierId,
    string? SupplierName,
    DateOnly? ExpectedDeliveryDate,
    bool RequiresProduction,
    CustomerOrderLineOpticalSnapshotDto? OpticalSnapshot);
