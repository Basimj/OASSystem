namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record CreatePurchaseInvoiceLineRequest(
    int LineSequence,
    Guid? PurchaseOrderLineId,
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxRate);
