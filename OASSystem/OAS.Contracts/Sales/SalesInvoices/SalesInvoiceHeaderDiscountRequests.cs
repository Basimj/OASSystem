using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record SetSalesInvoiceHeaderDiscountRequest(
    SalesDiscountType DiscountType,
    decimal? DiscountValue,
    string? Reason,
    string RowVersion);

public sealed record ApproveSalesInvoiceHeaderDiscountRequest(string RowVersion);
