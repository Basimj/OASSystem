using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record CustomerOrderLineDto(
    Guid Id,
    Guid CustomerOrderId,
    int LineNumber,
    Guid? GroupId,
    SalesLineType LineType,
    Guid? ProductVariantId,
    Guid? WarehouseId,
    string DescriptionSnapshot,
    decimal Quantity,
    decimal BaseUnitPrice,
    decimal ActualUnitPrice,
    SalesDiscountType DiscountType,
    decimal? DiscountValue,
    decimal DiscountAmount,
    decimal? TaxRate,
    decimal TaxAmount,
    decimal NetAmount,
    decimal FinalAmount,
    Guid? PrescriptionRevisionId,
    EyeSide? PrescriptionEye,
    bool RequiresProduction,
    string? Notes,
    bool IsActive,
    string RowVersion);
