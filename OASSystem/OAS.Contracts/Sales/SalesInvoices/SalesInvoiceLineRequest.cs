using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record SalesInvoiceLineRequest(
    Guid? Id,
    Guid? CustomerOrderLineId,
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
    string? RowVersion = null);
