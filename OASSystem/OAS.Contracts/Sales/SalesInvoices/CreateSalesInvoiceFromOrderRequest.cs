namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record CreateSalesInvoiceFromOrderRequest(
    string InvoiceCode,
    DateOnly InvoiceDate,
    DateOnly PostingDate,
    string? Description,
    string OrderRowVersion);
