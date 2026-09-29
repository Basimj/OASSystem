using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record CreatePurchaseInvoiceRequest(
    string? SupplierInvoiceCode,
    Guid SupplierId,
    DateOnly InvoiceDate,
    DateOnly PostingDate,
    Guid CurrencyId,
    decimal ExchangeRate,
    DateOnly ExchangeRateDate,
    TaxCalculationMode TaxCalculationMode,
    string? Notes,
    IReadOnlyList<CreatePurchaseInvoiceLineRequest> Lines);
