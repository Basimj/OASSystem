using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Reports;
using OAS.Application.Accounting.Abstractions;

namespace OAS.API.Accounting.Controllers;

/// <summary>
/// SA-04 customer financial reporting. All endpoints use the existing Customers.View
/// server permission; never expose customer ledger data to merely authenticated users.
/// This read-only query service depends on OasDbContext (already scoped in Infrastructure DI).
/// </summary>
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/reports/customers")]
public sealed class CustomerArReportsController(
    ICustomerArReportingQueryService reports,
    IPermissionChecker permissions) : ControllerBase
{
    private ICustomerArReportingQueryService Reports => reports;

    private Task<bool> Allowed(CancellationToken ct) =>
        permissions.HasPermissionAsync(AccountingPermissions.Customers.View, ct);

    [HttpGet("{customerId:guid}/statement")]
    public async Task<ActionResult<CustomerStatementReportDto>> Statement(
        Guid customerId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!await Allowed(ct)) return Forbid();
        if (toDate < fromDate) return BadRequest("تاريخ النهاية يجب ألا يسبق تاريخ البداية.");
        return Ok(await Reports.StatementAsync(customerId, fromDate, toDate, pageNumber, pageSize, ct));
    }

    [HttpGet("balances")]
    public async Task<ActionResult<CustomerBalancesReportDto>> Balances(
        [FromQuery] DateOnly asOfDate, [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!await Allowed(ct)) return Forbid();
        return Ok(await Reports.BalancesAsync(asOfDate, search, pageNumber, pageSize, ct));
    }

    [HttpGet("aging")]
    public async Task<ActionResult<CustomerAgingReportDto>> Aging(
        [FromQuery] DateOnly asOfDate, [FromQuery] Guid? customerId = null,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!await Allowed(ct)) return Forbid();
        return Ok(await Reports.AgingAsync(asOfDate, customerId, pageNumber, pageSize, ct));
    }

    [HttpGet("reconciliation")]
    public async Task<ActionResult<CustomerArReconciliationDto>> Reconciliation(
        [FromQuery] DateOnly asOfDate, [FromQuery] Guid? controlAccountId = null,
        CancellationToken ct = default)
    {
        if (!await Allowed(ct)) return Forbid();
        return Ok(await Reports.ReconcileAsync(asOfDate, controlAccountId, ct));
    }

    [HttpGet("{customerId:guid}/workspace")]
    public async Task<ActionResult<CustomerArWorkspaceDto>> Workspace(
        Guid customerId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!await Allowed(ct)) return Forbid();
        if (toDate < fromDate) return BadRequest("تاريخ النهاية يجب ألا يسبق تاريخ البداية.");
        return Ok(await Reports.WorkspaceAsync(customerId, fromDate, toDate, pageNumber, pageSize, ct));
    }

    [HttpGet("{customerId:guid}/documents")]
    public async Task<ActionResult<CustomerArDocumentPageDto>> Documents(
        Guid customerId, [FromQuery] string kind, [FromQuery] DateOnly asOfDate,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!await Allowed(ct)) return Forbid();
        if (kind is not ("invoices" or "returns" or "receipts" or "advances" or "credits"))
            return BadRequest("نوع المستند غير مدعوم.");
        return Ok(await Reports.DocumentsAsync(customerId, kind, asOfDate, pageNumber, pageSize, ct));
    }

    // The CSV is generated from the EXACT same statement query/DTO as UI/print.
    // Stream bounded pages rather than buffering a million ledger lines in RAM.
    [HttpGet("{customerId:guid}/statement.csv")]
    public async Task<IActionResult> StatementCsv(
        Guid customerId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        CancellationToken ct = default)
    {
        if (!await Allowed(ct)) return Forbid();
        if (toDate < fromDate) return BadRequest("تاريخ النهاية يجب ألا يسبق تاريخ البداية.");
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = $"attachment; filename=customer-{customerId:N}-statement.csv";
        await Response.Body.WriteAsync(new byte[] { 0xEF, 0xBB, 0xBF }, ct);
        await Response.WriteAsync("PostingDate,Journal,SourceType,Description,DebitBase,CreditBase,BalanceBase,Currency,TransactionDebit,TransactionCredit\r\n", ct);
        for (var page = 1; ; page++)
        {
            var report = await Reports.StatementAsync(customerId, fromDate, toDate, page, 200, ct);
            var text = new StringBuilder();
            foreach (var row in report.Movements)
            {
                text.Append(Csv(row.PostingDate.ToString("yyyy-MM-dd"))).Append(',')
                    .Append(Csv(row.JournalNumber)).Append(',')
                    .Append(Csv(row.SourceDocumentType)).Append(',')
                    .Append(Csv(row.Description)).Append(',')
                    .Append(Number(row.DebitBase)).Append(',').Append(Number(row.CreditBase)).Append(',')
                    .Append(Number(row.BalanceBase)).Append(',').Append(Csv(row.TransactionCurrency))
                    .Append(',').Append(Number(row.TransactionDebit)).Append(',')
                    .Append(Number(row.TransactionCredit)).Append("\r\n");
            }
            await Response.WriteAsync(text.ToString(), ct);
            if ((long)page * 200 >= report.TotalCount) break;
        }
        return new EmptyResult();
    }

    [HttpGet("aging.csv")]
    public async Task<IActionResult> AgingCsv([FromQuery] DateOnly asOfDate,
        [FromQuery] Guid? customerId = null, CancellationToken ct = default)
    {
        if (!await Allowed(ct)) return Forbid();
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = "attachment; filename=customer-ar-aging.csv";
        await Response.Body.WriteAsync(new byte[] { 0xEF, 0xBB, 0xBF }, ct);
        await Response.WriteAsync("Customer,Invoice,PostingDate,DueDate,Bucket,OriginalBase,PaymentsBase,ReturnsBase,AppliedCreditBase,OutstandingBase\r\n", ct);
        for (int page = 1; ; page++)
        {
            var data = await Reports.AgingAsync(asOfDate, customerId, page, 200, ct);
            var csv = new StringBuilder();
            foreach (var x in data.Invoices)
                csv.Append(Csv(x.CustomerCode)).Append(',').Append(Csv(x.InvoiceNumber)).Append(',')
                   .Append(Csv(x.PostingDate.ToString("yyyy-MM-dd"))).Append(',')
                   .Append(Csv(x.DueDate?.ToString("yyyy-MM-dd"))).Append(',').Append(Csv(x.Bucket)).Append(',')
                   .Append(Number(x.OriginalBase)).Append(',').Append(Number(x.PaymentBase)).Append(',')
                   .Append(Number(x.ReturnBase)).Append(',').Append(Number(x.CreditAppliedBase)).Append(',')
                   .Append(Number(x.OutstandingBase)).Append("\r\n");
            await Response.WriteAsync(csv.ToString(), ct);
            if ((long)page * 200 >= data.TotalCount) break;
        }
        return new EmptyResult();
    }

    [HttpGet("balances.csv")]
    public async Task<IActionResult> BalancesCsv([FromQuery] DateOnly asOfDate,
        [FromQuery] string? search = null, CancellationToken ct = default)
    {
        if (!await Allowed(ct)) return Forbid();
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = "attachment; filename=customer-ar-balances.csv";
        await Response.Body.WriteAsync(new byte[] { 0xEF, 0xBB, 0xBF }, ct);
        await Response.WriteAsync("Customer,CustomerName,Account,DebitBase,CreditBase,NetBase\r\n", ct);
        for (int page = 1; ; page++)
        {
            var data = await Reports.BalancesAsync(asOfDate, search, page, 200, ct);
            var csv = new StringBuilder();
            foreach (var x in data.Customers)
                csv.Append(Csv(x.CustomerCode)).Append(',').Append(Csv(x.CustomerName)).Append(',')
                   .Append(Csv(x.AccountCode)).Append(',').Append(Number(x.DebitBase)).Append(',')
                   .Append(Number(x.CreditBase)).Append(',').Append(Number(x.BalanceBase)).Append("\r\n");
            await Response.WriteAsync(csv.ToString(), ct);
            if ((long)page * 200 >= data.TotalCount) break;
        }
        return new EmptyResult();
    }

    private static string Number(decimal? n) => n?.ToString("0.####", CultureInfo.InvariantCulture) ?? "";

    private static string Csv(string? input)
    {
        var text = (input ?? "").Replace("\r", " ").Replace("\n", " ");
        // Prevent Excel interpreting user-provided journal text as executable formulas.
        var leading = text.TrimStart(' ', '\t');
        if (leading.Length > 0 && "=+-@".Contains(leading[0])) text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
