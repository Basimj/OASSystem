namespace OAS.Contracts.Sales.PriceOverrides;

public sealed record RequestSalesPriceOverrideRequest(
    Guid SalesInvoiceLineId,
    decimal OverridePrice,
    string Reason,
    string InvoiceRowVersion);
