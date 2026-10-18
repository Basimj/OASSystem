using OAS.Contracts.Common.Pagination;

namespace OAS.Contracts.Purchasing.Reports;

public sealed record SupplierPerformanceQueryRequest
{
    public Guid? SupplierId { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public Guid? CurrencyId { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public bool OnlyOpen { get; init; }
    public bool OnlyLate { get; init; }
    public string? Search { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;
    public string? SortBy { get; init; } = "OrderDate";
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    public PageRequest ToPageRequest() => new()
    {
        PageNumber = Math.Max(1, PageNumber),
        PageSize = Math.Clamp(PageSize, 1, PageRequest.MaximumPageSize),
        Search = Search,
        SortBy = SortBy,
        SortDirection = SortDirection
    }.Normalize();
}

public sealed record SupplierPerformanceSummaryDto(
    DateOnly AsOfDate,
    decimal OrderedBaseQuantity,
    decimal ReceivedBaseQuantity,
    decimal ReturnedBaseQuantity,
    decimal NetReceivedBaseQuantity,
    decimal OpenPurchaseOrderBaseQuantity,
    decimal LatePurchaseOrderBaseQuantity,
    long OpenPurchaseOrders,
    long LatePurchaseOrders,
    long OpenPurchaseOrderLines,
    long LatePurchaseOrderLines,
    long MissingExpectedDateOpenLines,
    decimal? OrderedBaseAmount,
    decimal? ReceivedCostBaseAmount,
    decimal? PostedInvoicedNetBaseAmount,
    decimal? PostedInvoicedGrossBaseAmount,
    decimal? PurchaseReturnBaseAmount,
    decimal? GrniBaseAmount,
    decimal? PurchasePriceVarianceBaseAmount,
    decimal? AverageLeadTimeDays,
    decimal? OnTimeDeliveryRatePercent);

public sealed record SupplierPerformanceLineDto(
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid CurrencyId,
    string CurrencyCode,
    Guid ProductVariantId,
    string? ProductCode,
    string ProductName,
    Guid PurchaseOrderId,
    Guid PurchaseOrderLineId,
    string PurchaseOrderCode,
    string PurchaseOrderStatus,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    decimal OriginalOrderedQuantity,
    decimal OriginalBaseQuantity,
    decimal PeriodOrderedBaseQuantity,
    decimal PeriodReceivedBaseQuantity,
    decimal PeriodReturnedBaseQuantity,
    decimal PeriodNetReceivedBaseQuantity,
    decimal OpenBaseQuantity,
    bool IsLate,
    DateOnly? FirstReceiptDate,
    DateOnly? LastReceiptDate,
    decimal? AverageLeadTimeDays,
    decimal? OnTimeDeliveryRatePercent,
    decimal? OrderedBaseAmount,
    decimal? ReceivedCostBaseAmount,
    decimal? PostedInvoicedNetBaseAmount,
    decimal? PostedInvoicedGrossBaseAmount,
    decimal? PurchaseReturnBaseAmount,
    decimal? GrniBaseAmount,
    decimal? PurchasePriceVarianceBaseAmount,
    Guid? LatestPurchaseReceiptId,
    string? LatestPurchaseReceiptCode,
    Guid? LatestPurchaseInvoiceId,
    string? LatestPurchaseInvoiceCode,
    Guid? LatestPurchaseReturnId,
    string? LatestPurchaseReturnCode);

public sealed record SupplierPerformanceDocumentDto(
    string DocumentType,
    Guid DocumentId,
    string DocumentCode,
    DateOnly DocumentDate,
    string Status,
    decimal? BaseQuantity,
    decimal? BaseAmount,
    Guid? SourceLineId);

public sealed record SupplierPerformanceDrillDownDto(
    Guid PurchaseOrderId,
    Guid PurchaseOrderLineId,
    string PurchaseOrderCode,
    IReadOnlyList<SupplierPerformanceDocumentDto> Receipts,
    IReadOnlyList<SupplierPerformanceDocumentDto> PostedInvoices,
    IReadOnlyList<SupplierPerformanceDocumentDto> PurchaseReturns);

public sealed record SupplierPriceHistoryItemDto(
    Guid SupplierCatalogItemId,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    Guid ProductVariantId,
    string? SupplierProductCode,
    string? SupplierProductName,
    Guid CurrencyId,
    string CurrencyCode,
    decimal? UnitPrice,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsCurrent,
    int? LeadTimeDays);

public sealed record SupplierPerformancePrintDto(
    SupplierPerformanceSummaryDto Summary,
    IReadOnlyList<SupplierPerformanceLineDto> Lines,
    SupplierPerformanceQueryRequest Filters,
    DateTime GeneratedAtUtc);

public sealed record SupplierPerformanceExportFile(
    byte[] Content,
    string ContentType,
    string FileName);

public enum SupplierPerformanceExportFormat : byte
{
    Csv = 1,
    Xlsx = 2
}
