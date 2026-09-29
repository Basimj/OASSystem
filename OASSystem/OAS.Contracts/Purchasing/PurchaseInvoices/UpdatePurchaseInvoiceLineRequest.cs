namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record UpdatePurchaseInvoiceLineRequest(
    Guid? Id,
    int LineSequence,
    Guid? PurchaseOrderLineId,
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxRate,
    string? RowVersion);
