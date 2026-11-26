using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.Reports.Queries.GetAccountingDashboard;
using OAS.Application.Accounting.Reports.Queries.GetFiscalPeriodCloseReadiness;
using OAS.Application.Accounting.Reports.Queries.GetFiscalYearCloseReadiness;
using OAS.Application.Accounting.Reports.Queries.GetGeneralLedger;
using OAS.Application.Accounting.Reports.Queries.GetJournalReport;
using OAS.Application.Accounting.Reports.Queries.GetTrialBalance;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Reports;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/reports")]
public sealed class AccountingReportsController(ISender sender) : ControllerBase
{
    [HttpGet("vouchers")]
    public async Task<ActionResult<CashBankReportDto>> Vouchers([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] string mode = "all", [FromQuery] string status = "posted", [FromQuery] string kind = "all", [FromQuery] Guid? currencyId = null, [FromQuery] Guid? accountId = null, [FromQuery] int? method = null, [FromQuery] int? partyType = null, [FromQuery] string? party = null, [FromQuery] Guid? postedBy = null, [FromQuery] Guid? cashAccountId = null, [FromQuery] Guid? bankAccountId = null, CancellationToken ct = default)
        => Ok(await sender.Send(new OAS.Application.Accounting.Reports.Queries.GetVoucherReport.GetVoucherReportQuery(new VoucherReportFilter(fromDate,toDate,mode,status,kind,currencyId,accountId,method,partyType,party,postedBy,cashAccountId,bankAccountId)),ct));

    [HttpGet("cash-bank")]
    public async Task<ActionResult<CashBankReportDto>> CashBank([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] string mode = "balances", [FromQuery] string kind = "all", [FromQuery] Guid? accountId = null,
        [FromQuery] Guid? currencyId = null, [FromQuery] string activity = "all", [FromQuery] bool includeZero = false,
        [FromQuery] string shiftState = "all", [FromQuery] string settlementState = "all", [FromQuery] Guid? openedBy = null, CancellationToken ct = default)
        => Ok(await sender.Send(new OAS.Application.Accounting.Reports.Queries.GetCashBankReport.GetCashBankReportQuery(
            new CashBankReportFilterDto(fromDate, toDate, mode, kind, accountId, currencyId, activity, includeZero, shiftState, settlementState, openedBy)), ct));

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<BalanceSheetReportDto>> BalanceSheet([FromQuery] DateOnly asOfDate,
        [FromQuery] Guid? fiscalYearId = null, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new OAS.Application.Accounting.Reports.Queries.GetBalanceSheet.GetBalanceSheetQuery(
            new BalanceSheetFilterDto(asOfDate, fiscalYearId)), cancellationToken));

    [HttpGet("income-statement")]
    public async Task<ActionResult<IncomeStatementReportDto>> IncomeStatement([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] Guid? fiscalYearId = null, [FromQuery] Guid? fiscalPeriodId = null, [FromQuery] Guid? costCenterId = null, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new OAS.Application.Accounting.Reports.Queries.GetIncomeStatement.GetIncomeStatementQuery(
            new IncomeStatementFilterDto(fromDate, toDate, fiscalYearId, fiscalPeriodId, costCenterId)), cancellationToken));

    [HttpGet("dashboard")]
    public async Task<ActionResult<AccountingDashboardDto>> Dashboard(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountingDashboardQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("general-ledger")]
    public async Task<ActionResult<GeneralLedgerReportDto>> GeneralLedger(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? fiscalYearId,
        [FromQuery] Guid? fiscalPeriodId,
        [FromQuery] bool includeAccountSubtree = false,
        [FromQuery] JournalType? journalType = null,
        [FromQuery] string? sourceModule = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? reference = null,
        [FromQuery] bool includeOpeningBalance = true,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetGeneralLedgerQuery(
                fromDate,
                toDate,
                accountId,
                fiscalYearId,
                fiscalPeriodId,
                includeAccountSubtree,
                journalType,
                sourceModule,
                customerId,
                supplierId,
                reference,
                includeOpeningBalance),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceReportDto>> TrialBalance(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] DateOnly? asOfDate = null,
        [FromQuery] Guid? fiscalYearId = null,
        [FromQuery] Guid? fiscalPeriodId = null,
        [FromQuery] byte? accountLevel = null,
        [FromQuery] bool postingAccountsOnly = false,
        [FromQuery] bool includeZeroBalance = false,
        [FromQuery] bool groupedTree = false,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetTrialBalanceQuery(
                fromDate,
                toDate,
                asOfDate,
                fiscalYearId,
                fiscalPeriodId,
                accountLevel,
                postingAccountsOnly,
                includeZeroBalance,
                groupedTree),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("journal-report")]
    public async Task<ActionResult<JournalReportDto>> JournalReport(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] Guid? fiscalYearId = null,
        [FromQuery] Guid? fiscalPeriodId = null,
        [FromQuery] JournalType? journalType = null,
        [FromQuery] string? sourceModule = null,
        [FromQuery] JournalEntryStatus? status = null,
        [FromQuery] Guid? accountId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? journalNumber = null,
        [FromQuery] string? reference = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] bool includeCompleteEntries = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new JournalReportFilterDto(
            fromDate,
            toDate,
            fiscalYearId,
            fiscalPeriodId,
            journalType,
            sourceModule,
            status,
            accountId,
            userId,
            journalNumber,
            reference,
            pageNumber,
            pageSize,
            includeCompleteEntries);

        var result = await sender.Send(new GetJournalReportQuery(filter), cancellationToken);
        return Ok(result);
    }

    [HttpGet("fiscal-periods/{id:guid}/close-readiness")]
    public async Task<ActionResult<FiscalCloseReadinessDto>> FiscalPeriodCloseReadiness(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetFiscalPeriodCloseReadinessQuery(id),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("fiscal-years/{id:guid}/close-readiness")]
    public async Task<ActionResult<FiscalCloseReadinessDto>> FiscalYearCloseReadiness(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetFiscalYearCloseReadinessQuery(id),
            cancellationToken);
        return Ok(result);
    }
}
