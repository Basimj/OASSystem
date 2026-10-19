using OAS.Contracts.Common.Pagination;

namespace OAS.Contracts.Purchasing.Performance;

/// <summary>All date filters are document-local dates: order date for orders, posting date for posted documents,
/// and effective date for supplier price history. Quantities are always in the document's purchase unit.</summary>
public sealed record PurchasingPerformanceFilterDto(
    DateOnly FromDate,
    DateOnly ToDate,
    string Kind = "orders",
    Guid? SupplierId = null,
    Guid? WarehouseId = null,
    Guid? ProductVariantId = null,
    Guid? CurrencyId = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 25);

public sealed record PurchasingPerformanceSummaryDto(
    int OrderCount,
    int OpenOrderCount,
    int LateOrderCount,
    int PostedReceiptCount,
    int PostedInvoiceCount,
    int PostedReturnCount,
    double? AverageReceiptLeadDays,
    decimal PostedReceiptCostBase,
    decimal PostedInvoiceTotalBase,
    decimal PostedReturnTotalBase,
    string BaseCurrencyCode,
    decimal? OnTimeDeliveryRatePercent = null,
    decimal? UninvoicedReceiptCostBase = null,
    decimal? PurchasePriceVarianceBase = null);

/// <summary>Amounts remain separate by source and currency; no cross-currency grand total is provided.
/// Open quantity is PO ordered minus posted accepted receipts (returns do not reopen a PO).
/// Uninvoiced accepted is a physical indicator, not the accounting GRNI ledger balance.</summary>
public sealed record PurchasingPerformanceRowDto
{
    public string Kind { get; init; } = "";
    public Guid DocumentId { get; init; }
    public string DocumentCode { get; init; } = "";
    public DateOnly DocumentDate { get; init; }
    public string Status { get; init; } = "";
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = "";
    public Guid? WarehouseId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string ProductName { get; init; } = "";
    public string? UnitName { get; init; }
    public string CurrencyCode { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal? OrderedQuantity { get; init; }
    public decimal? AcceptedQuantity { get; init; }
    public decimal? InvoicedQuantity { get; init; }
    public decimal? ReturnedQuantity { get; init; }
    public decimal? OpenQuantity { get; init; }
    public decimal? UninvoicedAcceptedQuantity { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? DocumentAmount { get; init; }
    public decimal? BaseAmount { get; init; }
    public decimal? PriceVariance { get; init; }
    public DateOnly? ExpectedDeliveryDate { get; init; }
    public int? LeadTimeDays { get; init; }
    public bool IsLate { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? PurchaseOrderLineId { get; init; }
    public Guid? PurchaseReceiptId { get; init; }
    public Guid? PurchaseInvoiceId { get; init; }
    public Guid? PurchaseReturnId { get; init; }
}

public sealed record PurchasingPerformanceReportDto(
    PurchasingPerformanceFilterDto Filter,
    PurchasingPerformanceSummaryDto Summary,
    PagedResult<PurchasingPerformanceRowDto> Page);


/// <summary>Historical posted documents linked to one purchase order line.
/// Quantities are purchase-unit quantities; amounts are base-currency values.
/// Return amounts include supplier tax; invoice amounts are matched net amounts.</summary>
public sealed record PurchasingPerformanceLinkedDocumentDto(
    string Kind, Guid DocumentId, string DocumentCode, DateOnly PostingDate,
    decimal Quantity, decimal? BaseAmount);

public sealed record PurchasingPerformanceOrderLineDrillDownDto(
    Guid PurchaseOrderId, Guid PurchaseOrderLineId, string PurchaseOrderCode,
    string BaseCurrencyCode,
    IReadOnlyList<PurchasingPerformanceLinkedDocumentDto> Receipts,
    IReadOnlyList<PurchasingPerformanceLinkedDocumentDto> Invoices,
    IReadOnlyList<PurchasingPerformanceLinkedDocumentDto> Returns,
    bool IsTruncated);
