using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Reports;

namespace OAS.Application.Purchasing.Abstractions;

public interface ISupplierPerformanceReportingQueryService
{
    Task<SupplierPerformanceSummaryDto> GetSummaryAsync(
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SupplierPerformanceLineDto>> GetLinesAsync(
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<SupplierPerformanceDrillDownDto> GetDrillDownAsync(
        Guid purchaseOrderLineId,
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SupplierPriceHistoryItemDto>> GetPriceHistoryAsync(
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierPerformanceLineDto>> GetExportRowsAsync(
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        int maximumRows,
        CancellationToken cancellationToken = default);
}
