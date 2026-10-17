using System.Globalization;
using System.Text;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Spreadsheets;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Reports;

namespace OAS.Application.Purchasing.Reports.Queries;

public sealed record GetSupplierPerformanceSummaryQuery(SupplierPerformanceQueryRequest Request)
    : IQuery<SupplierPerformanceSummaryDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.View];
}

public sealed record GetSupplierPerformanceLinesQuery(SupplierPerformanceQueryRequest Request)
    : IQuery<PagedResult<SupplierPerformanceLineDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.View];
}

public sealed record GetSupplierPerformanceDrillDownQuery(Guid PurchaseOrderLineId, SupplierPerformanceQueryRequest Request)
    : IQuery<SupplierPerformanceDrillDownDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.View];
}

public sealed record GetSupplierPriceHistoryReportQuery(SupplierPerformanceQueryRequest Request)
    : IQuery<PagedResult<SupplierPriceHistoryItemDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.View];
}

public sealed record GetSupplierPerformancePrintQuery(SupplierPerformanceQueryRequest Request)
    : IQuery<SupplierPerformancePrintDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.View];
}

public sealed record ExportSupplierPerformanceQuery(
    SupplierPerformanceQueryRequest Request,
    SupplierPerformanceExportFormat Format)
    : IQuery<SupplierPerformanceExportFile>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.View];
}

internal static class SupplierPerformancePermission
{
    public static Task<bool> CanViewCostsAsync(IPermissionChecker permissions, CancellationToken ct) =>
        permissions.HasPermissionAsync(PurchasingPermissions.CustomerDemand.ViewCosts, ct);
}

public sealed class GetSupplierPerformanceSummaryQueryHandler(
    ISupplierPerformanceReportingQueryService reports,
    IPermissionChecker permissions)
    : IRequestHandler<GetSupplierPerformanceSummaryQuery, SupplierPerformanceSummaryDto>
{
    public async Task<SupplierPerformanceSummaryDto> Handle(GetSupplierPerformanceSummaryQuery query, CancellationToken ct) =>
        await reports.GetSummaryAsync(query.Request, await SupplierPerformancePermission.CanViewCostsAsync(permissions, ct), ct);
}

public sealed class GetSupplierPerformanceLinesQueryHandler(
    ISupplierPerformanceReportingQueryService reports,
    IPermissionChecker permissions)
    : IRequestHandler<GetSupplierPerformanceLinesQuery, PagedResult<SupplierPerformanceLineDto>>
{
    public async Task<PagedResult<SupplierPerformanceLineDto>> Handle(GetSupplierPerformanceLinesQuery query, CancellationToken ct) =>
        await reports.GetLinesAsync(query.Request, await SupplierPerformancePermission.CanViewCostsAsync(permissions, ct), ct);
}

public sealed class GetSupplierPerformanceDrillDownQueryHandler(
    ISupplierPerformanceReportingQueryService reports,
    IPermissionChecker permissions)
    : IRequestHandler<GetSupplierPerformanceDrillDownQuery, SupplierPerformanceDrillDownDto>
{
    public async Task<SupplierPerformanceDrillDownDto> Handle(GetSupplierPerformanceDrillDownQuery query, CancellationToken ct) =>
        await reports.GetDrillDownAsync(query.PurchaseOrderLineId, query.Request,
            await SupplierPerformancePermission.CanViewCostsAsync(permissions, ct), ct);
}

public sealed class GetSupplierPriceHistoryReportQueryHandler(
    ISupplierPerformanceReportingQueryService reports,
    IPermissionChecker permissions)
    : IRequestHandler<GetSupplierPriceHistoryReportQuery, PagedResult<SupplierPriceHistoryItemDto>>
{
    public async Task<PagedResult<SupplierPriceHistoryItemDto>> Handle(GetSupplierPriceHistoryReportQuery query, CancellationToken ct) =>
        await reports.GetPriceHistoryAsync(query.Request, await SupplierPerformancePermission.CanViewCostsAsync(permissions, ct), ct);
}

public sealed class GetSupplierPerformancePrintQueryHandler(
    ISupplierPerformanceReportingQueryService reports,
    IPermissionChecker permissions,
    TimeProvider timeProvider)
    : IRequestHandler<GetSupplierPerformancePrintQuery, SupplierPerformancePrintDto>
{
    public async Task<SupplierPerformancePrintDto> Handle(GetSupplierPerformancePrintQuery query, CancellationToken ct)
    {
        var includeCosts = await SupplierPerformancePermission.CanViewCostsAsync(permissions, ct);
        var summary = await reports.GetSummaryAsync(query.Request, includeCosts, ct);
        var rows = await reports.GetExportRowsAsync(query.Request, includeCosts, 5000, ct);
        return new SupplierPerformancePrintDto(summary, rows, query.Request, timeProvider.GetUtcNow().UtcDateTime);
    }
}

public sealed class ExportSupplierPerformanceQueryHandler(
    ISupplierPerformanceReportingQueryService reports,
    IPermissionChecker permissions,
    ISpreadsheetWorkbook workbook,
    TimeProvider timeProvider)
    : IRequestHandler<ExportSupplierPerformanceQuery, SupplierPerformanceExportFile>
{
    public async Task<SupplierPerformanceExportFile> Handle(ExportSupplierPerformanceQuery query, CancellationToken ct)
    {
        var includeCosts = await SupplierPerformancePermission.CanViewCostsAsync(permissions, ct);
        var rows = await reports.GetExportRowsAsync(query.Request, includeCosts, 50000, ct);
        var stamp = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        return query.Format switch
        {
            SupplierPerformanceExportFormat.Csv => new SupplierPerformanceExportFile(
                BuildCsv(rows, includeCosts), "text/csv; charset=utf-8", $"supplier-purchasing-performance-{stamp}.csv"),
            SupplierPerformanceExportFormat.Xlsx => new SupplierPerformanceExportFile(
                BuildXlsx(workbook, rows, includeCosts),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"supplier-purchasing-performance-{stamp}.xlsx"),
            _ => throw new ArgumentOutOfRangeException(nameof(query.Format))
        };
    }

    private static byte[] BuildCsv(IReadOnlyList<SupplierPerformanceLineDto> rows, bool includeCosts)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        var headers = BaseHeaders().ToList();
        if (includeCosts) headers.AddRange(CostHeaders());
        sb.AppendLine(string.Join(',', headers.Select(Escape)));

        foreach (var row in rows)
        {
            var values = BaseValues(row).ToList();
            if (includeCosts) values.AddRange(CostValues(row));
            sb.AppendLine(string.Join(',', values.Select(Escape)));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] BuildXlsx(ISpreadsheetWorkbook workbook, IReadOnlyList<SupplierPerformanceLineDto> rows, bool includeCosts)
    {
        var columns = BaseHeaders().Select((header, i) => new SpreadsheetColumn($"C{i + 1}", Header: header)).ToList();
        if (includeCosts)
        {
            var offset = columns.Count;
            columns.AddRange(CostHeaders().Select((header, i) => new SpreadsheetColumn($"C{offset + i + 1}", Header: header)));
        }
        var sheet = new SpreadsheetSheet("SupplierPerformance", columns, "أداء المشتريات والموردين");
        var tableRows = rows.Select(row =>
        {
            var values = BaseValues(row).ToList();
            if (includeCosts) values.AddRange(CostValues(row));
            return (IReadOnlyDictionary<string, string>)values
                .Select((value, i) => new KeyValuePair<string, string>($"C{i + 1}", value))
                .ToDictionary(x => x.Key, x => x.Value);
        }).ToArray();
        return workbook.Write([new SpreadsheetTable(sheet, tableRows)]);
    }

    private static IEnumerable<string> BaseHeaders() =>
    [
        "كود المورد", "المورد", "المخزن", "العملة", "كود الصنف", "الصنف", "أمر الشراء", "الحالة",
        "تاريخ الطلب", "تاريخ التسليم المتوقع", "الكمية الأساسية المطلوبة", "المستلمة", "المرتجعة", "صافي المستلم",
        "المفتوح", "متأخر", "أول استلام", "آخر استلام", "متوسط زمن التوريد", "نسبة الالتزام بالموعد"
    ];

    private static IEnumerable<string> CostHeaders() =>
    [
        "قيمة الطلب الأساسية", "تكلفة الاستلام الأساسية", "صافي الفواتير المرحلة", "إجمالي الفواتير المرحلة",
        "مرتجعات المورد", "GRNI", "فرق سعر الشراء PPV"
    ];

    private static IEnumerable<string> BaseValues(SupplierPerformanceLineDto r) =>
    [
        r.SupplierCode, r.SupplierName, r.WarehouseName, r.CurrencyCode, r.ProductCode ?? string.Empty, r.ProductName,
        r.PurchaseOrderCode, r.PurchaseOrderStatus, D(r.OrderDate), D(r.ExpectedDeliveryDate), N(r.PeriodOrderedBaseQuantity),
        N(r.PeriodReceivedBaseQuantity), N(r.PeriodReturnedBaseQuantity), N(r.PeriodNetReceivedBaseQuantity),
        N(r.OpenBaseQuantity), r.IsLate ? "نعم" : "لا", D(r.FirstReceiptDate), D(r.LastReceiptDate),
        N(r.AverageLeadTimeDays), N(r.OnTimeDeliveryRatePercent)
    ];

    private static IEnumerable<string> CostValues(SupplierPerformanceLineDto r) =>
    [
        N(r.OrderedBaseAmount), N(r.ReceivedCostBaseAmount), N(r.PostedInvoicedNetBaseAmount), N(r.PostedInvoicedGrossBaseAmount),
        N(r.PurchaseReturnBaseAmount), N(r.GrniBaseAmount), N(r.PurchasePriceVarianceBaseAmount)
    ];

    private static string D(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string D(DateOnly? value) => value.HasValue ? D(value.Value) : string.Empty;
    private static string N(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    private static string N(decimal? value) => value.HasValue ? N(value.Value) : string.Empty;
    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) >= 0
        ? $"\"{value.Replace("\"", "\"\"")}\""
        : value;
}
