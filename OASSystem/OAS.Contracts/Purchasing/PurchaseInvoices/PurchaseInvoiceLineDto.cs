namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record PurchaseInvoiceLineDto(
    Guid Id,
    Guid PurchaseInvoiceId,
    int LineSequence,
    Guid? PurchaseOrderLineId,
    Guid ProductVariantId,
    string? ProductCodeSnapshot,
    string DescriptionSnapshot,
    decimal Quantity,
    decimal UnitPrice,
    decimal GrossAmount,
    decimal DiscountAmount,
    decimal NetAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal FinalAmount,
    decimal BaseNetAmount,
    decimal BaseTaxAmount,
    decimal BaseFinalAmount,
    string RowVersion);
