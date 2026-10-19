namespace OAS.Contracts.Sales.Reports;

public enum SalesReportGroupBy : byte
{
    Day = 1, Month = 2, Customer = 3, Employee = 4,
    Product = 5, Category = 6, Warehouse = 7
}

/// <summary>All monetary totals in this report are historical BASE-currency amounts.</summary>
public sealed record SalesReportFilterDto(
    DateOnly FromDate, DateOnly ToDate,
    SalesReportGroupBy GroupBy = SalesReportGroupBy.Day,
    Guid? CustomerId = null, Guid? EmployeeId = null,
    Guid? ProductVariantId = null, Guid? CategoryId = null,
    Guid? WarehouseId = null, int PageNumber = 1, int PageSize = 25);

public sealed record SalesReportTotalsDto(
    decimal InvoiceRevenueBase, decimal ReturnedRevenueBase, decimal NetRevenueBase,
    decimal InvoiceDiscountsBase, decimal NetSalesTaxBase, decimal NetCogsBase,
    decimal GrossProfitBase, decimal GrossMarginPercent,
    long InvoiceCount, long ReturnCount);

public sealed record SalesReportBreakdownDto(
    string Key, string Label, SalesReportTotalsDto Totals);

public sealed record SalesReportDocumentDto(
    string DocumentKind, Guid DocumentId, string DocumentCode,
    DateOnly PostingDate, Guid CustomerId, Guid? EmployeeId,
    string CurrencyCode, SalesReportTotalsDto Totals);

public sealed record SalesReportDto(
    SalesReportFilterDto Filter, string BaseCurrencyCode,
    SalesReportTotalsDto Totals,
    IReadOnlyList<SalesReportBreakdownDto> Breakdown,
    IReadOnlyList<SalesReportDocumentDto> Documents,
    long TotalDocumentCount, int PageNumber, int PageSize,
    bool BreakdownTruncated);
