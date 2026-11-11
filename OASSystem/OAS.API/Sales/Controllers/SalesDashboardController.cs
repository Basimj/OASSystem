using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Reports;
using OAS.Application.Sales.SalesInvoices.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Contracts.Sales.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Production;
using OAS.Infrastructure.Persistence;

namespace OAS.API.Sales.Controllers;

/// <summary>
/// Authoritative sales-home aggregates. Never derive dashboard metrics from the
/// paginated document list. The report query enforces SalesPermissions.View.
/// </summary>
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/dashboard")]
public sealed class SalesDashboardController(ISender sender, OasDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SalesDashboardDto>> Get(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        // Day boundaries match the existing report's posting-date convention.
        // A company-wide business-timezone setting should replace this server date
        // if configured by the host; no browser-local date is mixed into the SQL.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var from = fromDate ?? today;
        var to = toDate ?? today;
        if (from > to || to < new DateOnly(1900, 1, 8) || to.DayNumber - from.DayNumber > 366)
            return BadRequest("حدد فترة صحيحة لا تتجاوز سنة واحدة.");

        // Use the same authoritative sales-report computation for net sales and
        // returns. The request passes through the MediatR authorization behavior.
        var report = await sender.Send(new GetSalesReportQuery(
            new SalesReportFilterDto(from, to, SalesReportGroupBy.Day, PageSize: 1)), ct);
        var weekFrom = to.AddDays(-6);
        var weekReport = from == weekFrom && to == report.Filter.ToDate
            ? report
            : await sender.Send(new GetSalesReportQuery(
                new SalesReportFilterDto(weekFrom, to, SalesReportGroupBy.Day, PageSize: 1)), ct);
        var weekGroups = weekReport.Breakdown.ToDictionary(x => x.Key, x => x.Totals.NetRevenueBase);
        var week = Enumerable.Range(0, 7).Select(i =>
        {
            var date = weekFrom.AddDays(i);
            return new SalesDashboardDayDto(date,
                weekGroups.GetValueOrDefault(date.ToString("yyyy-MM-dd")));
        }).ToArray();

        // Posted invoice balances use the same sources and fallbacks as
        // SalesInvoiceBalanceService. Group each source independently to avoid
        // multiplying balances by joining payments to returns.
        var invoices = await db.Set<SalesInvoice>().AsNoTracking()
            .Where(x => x.Status == SalesInvoiceStatus.Posted)
            .Select(x => new { x.Id, x.BaseTotalAmount, x.DueDate })
            .ToListAsync(ct);

        var returned = await db.Set<SalesReturn>().AsNoTracking()
            .Where(x => x.Status == SalesReturnStatus.Posted)
            .GroupBy(x => x.SalesInvoiceId)
            .Select(g => new { Id = g.Key, Total = g.Sum(x => x.BaseTotalAmount) })
            .ToDictionaryAsync(x => x.Id, x => x.Total, ct);

        var allocated = await db.Set<PaymentAllocation>().AsNoTracking()
            .Where(x => x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice
                && x.ReversedAtUtc == null)
            .GroupBy(x => x.TargetDocumentId)
            .Select(g => new
            {
                Id = g.Key,
                Total = g.Sum(x => x.TargetBaseAllocatedAmount ?? x.BaseAllocatedAmount
                    ?? (x.AllocatedAmount * (x.ExchangeRate ?? 1m)))
            })
            .ToDictionaryAsync(x => x.Id, x => x.Total, ct);

        var credits = await db.Set<CustomerCreditApplication>().AsNoTracking()
            .Where(x => x.TargetSalesInvoiceId.HasValue
                && x.Status == CustomerCreditApplicationStatus.Active)
            .GroupBy(x => x.TargetSalesInvoiceId!.Value)
            .Select(g => new { Id = g.Key, Total = g.Sum(x => x.TargetBaseAmount ?? 0m) })
            .ToDictionaryAsync(x => x.Id, x => x.Total, ct);

        decimal outstanding = 0m;
        long outstandingCount = 0, overdueCount = 0;
        foreach (var invoice in invoices)
        {
            var net = Math.Max(0m, invoice.BaseTotalAmount - Math.Min(invoice.BaseTotalAmount,
                Math.Max(0m, returned.GetValueOrDefault(invoice.Id))));
            var balance = Math.Max(0m, net - Math.Max(0m,
                allocated.GetValueOrDefault(invoice.Id) + credits.GetValueOrDefault(invoice.Id)));
            if (balance <= 0m) continue;
            outstanding += balance;
            outstandingCount++;
            if (invoice.DueDate.HasValue && invoice.DueDate.Value < today) overdueCount++;
        }

        var orders = db.Set<CustomerOrder>().AsNoTracking();
        var inProgressOrders = await orders.LongCountAsync(x =>
            x.Status != CustomerOrderStatus.Draft &&
            x.Status != CustomerOrderStatus.Completed &&
            x.Status != CustomerOrderStatus.Cancelled, ct);
        var ready = await orders.LongCountAsync(x => x.Status == CustomerOrderStatus.ReadyForDelivery, ct);
        var pendingReturns = await db.Set<SalesReturn>().AsNoTracking()
            .LongCountAsync(x => x.Status == SalesReturnStatus.Confirmed, ct);
        var jobs = await db.Set<OpticalProductionJob>().AsNoTracking()
            .LongCountAsync(x => x.IsActive &&
                (x.Status == OpticalProductionStatus.Released ||
                 x.Status == OpticalProductionStatus.InProgress ||
                 x.Status == OpticalProductionStatus.QualityControl ||
                 x.Status == OpticalProductionStatus.QualityControlFailed), ct);

        var currency = await db.Set<Currency>().AsNoTracking()
            .Where(x => x.Code == report.BaseCurrencyCode)
            .Select(x => new { x.NameAr, x.Symbol }).FirstOrDefaultAsync(ct);
        var currencyLabel = !string.IsNullOrWhiteSpace(currency?.Symbol) ? currency.Symbol! :
            !string.IsNullOrWhiteSpace(currency?.NameAr) ? currency.NameAr : report.BaseCurrencyCode;

        return Ok(new SalesDashboardDto(
            from, to, report.BaseCurrencyCode, currencyLabel,
            report.Totals.InvoiceRevenueBase, report.Totals.ReturnedRevenueBase,
            report.Totals.NetRevenueBase, report.Totals.InvoiceCount,
            report.Totals.ReturnCount, outstanding, outstandingCount, overdueCount,
            inProgressOrders, ready, jobs, pendingReturns, week, DateTimeOffset.UtcNow));
    }

    /// <summary>Search across all invoice numbers AND customer names/codes, not the last page.</summary>
    [HttpGet("invoice-search")]
    public async Task<ActionResult<IReadOnlyList<SalesInvoiceDto>>> SearchInvoices(
        [FromQuery] string search, CancellationToken ct)
    {
        var term = search?.Trim() ?? string.Empty;
        if (term.Length is < 1 or > 100)
            return BadRequest("أدخل كلمة بحث من 1 إلى 100 حرف.");

        // Enforce the same application permission as the invoice list before querying.
        await sender.Send(new GetSalesInvoicesQuery(new PageRequest
        { PageNumber = 1, PageSize = 1 }), ct);

        var ids = await (from invoice in db.Set<SalesInvoice>().AsNoTracking()
                         join customer in db.Set<Customer>().AsNoTracking()
                             on invoice.CustomerId equals customer.Id
                         where invoice.InvoiceCode.Contains(term) ||
                               customer.NameAr.Contains(term) ||
                               customer.CustomerCode.Contains(term)
                         orderby invoice.InvoiceDate descending, invoice.CreatedAtUtc descending
                         select invoice.Id).Take(8).ToArrayAsync(ct);
        var list = new List<SalesInvoiceDto>(ids.Length);
        foreach (var id in ids)
            list.Add(await sender.Send(new GetSalesInvoiceByIdQuery(id), ct));
        return Ok(list);
    }

}
