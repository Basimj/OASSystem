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
            pageSize);

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
