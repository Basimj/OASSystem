using MediatR;
using OAS.Application.Accounting.Reports.Queries.GetSupplierAging;
using OAS.Application.Accounting.Reports.Queries.GetSupplierBalances;
using OAS.Application.Accounting.Reports.Queries.GetSupplierStatement;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Printing;

/// <summary>
/// Collects the *authorized* existing report query pages. No accounting figures are
/// calculated here, and no other ledger is introduced for printing or export.
/// </summary>
public sealed class SupplierReportSnapshotService(ISender sender)
{
    private const int PageSize = PageRequest.MaximumPageSize;
    public const int MaximumExportRows = 20_000;

    public async Task<SupplierStatementDto> StatementAsync(
        Guid supplierId, DateOnly fromDate, DateOnly toDate, CancellationToken ct)
    {
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier is required.", nameof(supplierId));
        if (toDate < fromDate) throw new ArgumentException("Invalid report date range.", nameof(toDate));

        var first = await sender.Send(new GetSupplierStatementQuery(
            SupplierId: supplierId, FromDate: fromDate, ToDate: toDate,
            PageNumber: 1, PageSize: PageSize,
            SortDirection: SortDirection.Ascending), ct);
        CheckMaximum(first.TotalCount);
        var all = new List<SupplierStatementMovementDto>(checked((int)first.TotalCount));
        all.AddRange(first.Movements);
        for (var page = 2; all.Count < first.TotalCount; page++)
        {
            ct.ThrowIfCancellationRequested();
            var current = await sender.Send(new GetSupplierStatementQuery(
                SupplierId: supplierId, FromDate: fromDate, ToDate: toDate,
                PageNumber: page, PageSize: PageSize,
                SortDirection: SortDirection.Ascending), ct);
            if (current.TotalCount != first.TotalCount ||
                current.OpeningLiabilityBaseAmount != first.OpeningLiabilityBaseAmount ||
                current.ClosingLiabilityBaseAmount != first.ClosingLiabilityBaseAmount ||
                current.Movements.Count == 0)
                throw new InvalidOperationException("تغيرت بيانات كشف المورد أثناء إنشاء التقرير. يرجى إعادة المحاولة.");
            all.AddRange(current.Movements);
        }
        if (all.Count != first.TotalCount)
            throw new InvalidOperationException("عدد حركات كشف المورد تغير أثناء إعداد التقرير.");
        if (all.Select(x => (x.JournalEntryId, x.LineNumber)).Distinct().Count() != all.Count)
            throw new InvalidOperationException("تغير ترتيب حركات كشف المورد أثناء إعداد التقرير؛ يرجى إعادة المحاولة.");
        return first with { Movements = all, PageNumber = 1, PageSize = all.Count };
    }

    public async Task<SupplierBalancesDto> BalancesAsync(DateOnly asOfDate, string? search, CancellationToken ct)
    {
        var first = await sender.Send(new GetSupplierBalancesQuery(
            asOfDate, Search: search, PageNumber: 1, PageSize: PageSize), ct);
        CheckMaximum(first.TotalCount);
        var all = new List<SupplierBalanceRowDto>(checked((int)first.TotalCount));
        all.AddRange(first.Rows);
        for (var page = 2; all.Count < first.TotalCount; page++)
        {
            ct.ThrowIfCancellationRequested();
            var current = await sender.Send(new GetSupplierBalancesQuery(
                asOfDate, Search: search, PageNumber: page, PageSize: PageSize), ct);
            if (current.TotalCount != first.TotalCount ||
                current.TotalLiabilityBaseAmount != first.TotalLiabilityBaseAmount ||
                current.Rows.Count == 0)
                throw new InvalidOperationException("تغيرت أرصدة الموردين أثناء التصدير.");
            all.AddRange(current.Rows);
        }
        if (all.Count != first.TotalCount)
            throw new InvalidOperationException("تغير عدد الموردين أثناء التصدير.");
        return first with { Rows = all, PageNumber = 1, PageSize = all.Count };
    }

    public async Task<SupplierAgingReportDto> AgingAsync(DateOnly asOfDate, Guid? supplierId, string? search, CancellationToken ct)
    {
        var first = await sender.Send(new GetSupplierAgingQuery(
            new SupplierAgingFilterDto(asOfDate, supplierId, Search: search, PageNumber: 1, PageSize: PageSize)), ct);
        CheckMaximum(first.TotalCount);
        var all = new List<SupplierAgingLineDto>(checked((int)first.TotalCount));
        all.AddRange(first.Lines);
        for (var page = 2; all.Count < first.TotalCount; page++)
        {
            ct.ThrowIfCancellationRequested();
            var current = await sender.Send(new GetSupplierAgingQuery(
                new SupplierAgingFilterDto(asOfDate, supplierId, Search: search, PageNumber: page, PageSize: PageSize)), ct);
            if (current.TotalCount != first.TotalCount ||
                current.TotalOutstanding != first.TotalOutstanding || current.Lines.Count == 0)
                throw new InvalidOperationException("تغير تقرير أعمار الذمم أثناء التصدير.");
            all.AddRange(current.Lines);
        }
        if (all.Count != first.TotalCount)
            throw new InvalidOperationException("تغير عدد حركات أعمار الذمم أثناء التصدير.");
        return first with { Lines = all, PageNumber = 1, PageSize = all.Count };
    }

    private static void CheckMaximum(long count)
    {
        if (count > MaximumExportRows)
            throw new ArgumentOutOfRangeException(nameof(count),
                $"التقرير يحتوي {count:N0} سجلًا؛ الحد الأقصى {MaximumExportRows:N0} للحفاظ على ذاكرة الخادم. قلّص الفترة.");
    }
}
