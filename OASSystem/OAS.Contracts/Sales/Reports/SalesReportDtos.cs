namespace OAS.Contracts.Sales.Reports;

public enum SalesReportGroupBy : byte
{
    Day = 1, Month = 2, Customer = 3, Employee = 4,
    Product = 5, Category = 6, Warehouse = 7,
    Year = 8, Summary = 9, Invoice = 10, Line = 11, ProductType = 12,
    Brand = 13, Operator = 14, Currency = 15, PaymentPlan = 16,
    Collections = 17, Return = 18, Discounts = 19, Taxes = 20, Cost = 21, Profit = 22
}

/// <summary>All monetary totals in this report are historical BASE-currency amounts.</summary>
public sealed record SalesReportFilterDto(
    DateOnly FromDate, DateOnly ToDate,
    SalesReportGroupBy GroupBy = SalesReportGroupBy.Day,
    Guid? CustomerId = null, Guid? EmployeeId = null,
    Guid? ProductVariantId = null, Guid? CategoryId = null,
    Guid? WarehouseId = null, int PageNumber = 1, int PageSize = 25,
    Guid? ProductTypeId = null, Guid? BrandId = null, string? Operator = null,
    string? CurrencyCode = null, int? PaymentPlan = null, string? SettlementStatus = null,
    // null preserves strict legacy callers; "*" explicitly opts into automatic historical scope.
    string? HistoricalBaseCurrencyCode = null);

public sealed record SalesReportTotalsDto(
    decimal InvoiceRevenueBase, decimal ReturnedRevenueBase, decimal NetRevenueBase,
    decimal InvoiceDiscountsBase, decimal NetSalesTaxBase, decimal NetCogsBase,
    decimal GrossProfitBase, decimal GrossMarginPercent,
    long InvoiceCount, long ReturnCount)
{
    public int MissingCostCount { get; init; }
    public decimal SoldQuantity { get; init; }
    public decimal ReturnedQuantity { get; init; }
    public bool IsGrossMarginAvailable => NetRevenueBase > 0 && MissingCostCount == 0;
    public bool IsProfitAvailable => MissingCostCount == 0;
}

public sealed record SalesReportBreakdownDto(
    string Key, string Label, SalesReportTotalsDto Totals)
{
    public Guid? DocumentId { get; init; }
    public string? DocumentKind { get; init; }
    public Guid? JournalId { get; init; }
    public string? CustomerName { get; init; }
    public DateOnly? PostingDate { get; init; }
    public string? ItemName { get; init; }
    public string? UnitName { get; init; }
    public string? CurrencyCode { get; init; }
    public int? LineNumber { get; init; }
    public string? SaleWarehouseName { get; init; }
    public string? ReturnWarehouseName { get; init; }
    public decimal? InvoiceTotalBase { get; init; }
    public decimal? ReturnedTotalBase { get; init; }
    public decimal? AllocatedBase { get; init; }
    public decimal? CreditAppliedBase { get; init; }
    public decimal? OutstandingBase { get; init; }
}

public sealed record SalesReportDocumentDto(
    string DocumentKind, Guid DocumentId, string DocumentCode,
    DateOnly PostingDate, Guid CustomerId, Guid? EmployeeId,
    string CurrencyCode, SalesReportTotalsDto Totals)
{
    public string? CustomerName { get; init; }
}

public sealed record SalesReportDto(
    SalesReportFilterDto Filter, string BaseCurrencyCode,
    SalesReportTotalsDto Totals,
    IReadOnlyList<SalesReportBreakdownDto> Breakdown,
    IReadOnlyList<SalesReportDocumentDto> Documents,
    long TotalDocumentCount, int PageNumber, int PageSize,
    bool BreakdownTruncated)
{
    public IReadOnlyList<SalesReportOptionDto> Options { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record SalesReportOptionDto(string Kind, string Key, string Label,
    string? ParentKey = null, string? TypeKey = null);
