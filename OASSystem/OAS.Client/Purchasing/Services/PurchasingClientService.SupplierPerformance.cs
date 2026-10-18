using System.Globalization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Reports;

namespace OAS.Client.Purchasing.Services;

public sealed partial class PurchasingClientService
{
    private static string PerformanceQuery(SupplierPerformanceQueryRequest request)
    {
        var q = new List<string>
        {
            "pageNumber=" + request.PageNumber.ToString(CultureInfo.InvariantCulture),
            "pageSize=" + request.PageSize.ToString(CultureInfo.InvariantCulture),
            "sortBy=" + Uri.EscapeDataString(request.SortBy ?? "OrderDate"),
            "sortDirection=" + Uri.EscapeDataString(request.SortDirection.ToString())
        };
        static void Add(List<string> args, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                args.Add(name + "=" + Uri.EscapeDataString(value));
        }
        Add(q, "supplierId", request.SupplierId?.ToString("D"));
        Add(q, "warehouseId", request.WarehouseId?.ToString("D"));
        Add(q, "productVariantId", request.ProductVariantId?.ToString("D"));
        Add(q, "currencyId", request.CurrencyId?.ToString("D"));
        Add(q, "fromDate", request.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add(q, "toDate", request.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add(q, "search", request.Search);
        if (request.OnlyOpen) q.Add("onlyOpen=true");
        if (request.OnlyLate) q.Add("onlyLate=true");
        return "?" + string.Join("&", q);
    }
    private const string PerformanceApi = "api/purchasing/reports/supplier-performance/";
    public Task<SupplierPerformanceSummaryDto?> GetSupplierPerformanceSummaryAsync(SupplierPerformanceQueryRequest request, CancellationToken ct = default)
        => _api.GetAsync<SupplierPerformanceSummaryDto>(PerformanceApi + "summary" + PerformanceQuery(request), ct);
    public async Task<PagedResult<SupplierPerformanceLineDto>> GetSupplierPerformanceLinesAsync(SupplierPerformanceQueryRequest request, CancellationToken ct = default)
        => await _api.GetAsync<PagedResult<SupplierPerformanceLineDto>>(PerformanceApi + "lines" + PerformanceQuery(request), ct) ?? new();
    public Task<SupplierPerformanceDrillDownDto?> GetSupplierPerformanceDrillDownAsync(Guid id, SupplierPerformanceQueryRequest request, CancellationToken ct = default)
        => _api.GetAsync<SupplierPerformanceDrillDownDto>(PerformanceApi + $"lines/{id:D}/drilldown" + PerformanceQuery(request), ct);
    public async Task<PagedResult<SupplierPriceHistoryItemDto>> GetSupplierPriceHistoryReportAsync(SupplierPerformanceQueryRequest request, CancellationToken ct = default)
        => await _api.GetAsync<PagedResult<SupplierPriceHistoryItemDto>>(PerformanceApi + "price-history" + PerformanceQuery(request), ct) ?? new();
    public Task<SupplierPerformancePrintDto?> GetSupplierPerformancePrintAsync(SupplierPerformanceQueryRequest request, CancellationToken ct = default)
        => _api.GetAsync<SupplierPerformancePrintDto>(PerformanceApi + "print" + PerformanceQuery(request), ct);
    public Task<byte[]> ExportSupplierPerformanceAsync(SupplierPerformanceQueryRequest request, SupplierPerformanceExportFormat format, CancellationToken ct = default)
        => _api.GetBytesAsync(PerformanceApi + "export" + PerformanceQuery(request) + "&format=" + Uri.EscapeDataString(format.ToString()), ct);
}
