namespace OAS.Contracts.Sales.Reports;

/// <summary>
/// Read-only dashboard facts. Monetary values are BASE currency snapshots and
/// use the same posting-date policy as the sales report.
/// </summary>
public sealed record SalesDashboardDayDto(DateOnly Date, decimal NetSalesBase);

public sealed record SalesDashboardDto(
    DateOnly FromDate,
    DateOnly ToDate,
    string BaseCurrencyCode,
    string BaseCurrencyLabel,
    decimal InvoiceRevenueBase,
    decimal ReturnedRevenueBase,
    decimal NetSalesBase,
    long InvoiceCount,
    long ReturnCount,
    decimal OutstandingBase,
    long OutstandingInvoiceCount,
    long OverdueInvoiceCount,
    long InProgressOrderCount,
    long ReadyForDeliveryCount,
    long InProgressProductionCount,
    long PendingReturnCount,
    IReadOnlyList<SalesDashboardDayDto> Week,
    DateTimeOffset GeneratedAtUtc);
