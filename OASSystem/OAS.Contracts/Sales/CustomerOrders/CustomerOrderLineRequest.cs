using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record CustomerOrderLineRequest(
    Guid? Id,
    Guid? GroupId,
    SalesLineType LineType,
    Guid? ProductVariantId,
    Guid? WarehouseId,
    string? Description,
    decimal Quantity,
    decimal ActualUnitPrice,
    SalesDiscountType DiscountType,
    decimal? DiscountValue,
    decimal? TaxRate,
    Guid? PrescriptionRevisionId,
    EyeSide? PrescriptionEye,
    bool RequiresProduction,
    string? Notes,
    string? RowVersion = null,
    CustomerOrderLineOpticalSnapshotRequest? OpticalSnapshot = null);
