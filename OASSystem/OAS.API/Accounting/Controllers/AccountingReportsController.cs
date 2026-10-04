using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.Reports.Queries.GetFiscalPeriodCloseReadiness;
using OAS.Application.Accounting.Reports.Queries.GetFiscalYearCloseReadiness;
using OAS.Application.Accounting.Reports.Queries.GetGeneralLedger;
using OAS.Application.Accounting.Reports.Queries.GetTrialBalance;
using OAS.Contracts.Accounting.Reports;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/reports")]
public sealed class AccountingReportsController(ISender sender) : ControllerBase
{
    [HttpGet("general-ledger")]
    public async Task<ActionResult<GeneralLedgerReportDto>> GeneralLedger(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetGeneralLedgerQuery(fromDate, toDate, accountId),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceReportDto>> TrialBalance(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetTrialBalanceQuery(fromDate, toDate),
            cancellationToken);
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
