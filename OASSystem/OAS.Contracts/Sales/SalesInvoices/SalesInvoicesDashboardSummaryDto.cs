namespace OAS.Contracts.Sales.SalesInvoices;

/// <summary>Document counts over the complete database, independent of the current list page.</summary>
public sealed record SalesInvoicesDashboardSummaryDto(
    int Total, int Today, int Draft, int Confirmed, int Posted, int Cancelled, int WithPostedReturns);
