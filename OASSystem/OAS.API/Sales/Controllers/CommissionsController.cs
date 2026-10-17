using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Commissions.Commands;
using OAS.Application.Sales.Commissions.Queries;
using OAS.Contracts.Sales.Commissions;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/commissions")]
public sealed class CommissionsController(ISender sender) : ControllerBase
{
    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<CommissionRuleDto>>> Rules([FromQuery] Guid? employeeId, CancellationToken ct) =>
        Ok(await sender.Send(new GetCommissionRulesQuery(employeeId), ct));

    [HttpPost("rules")]
    public async Task<ActionResult<CommissionRuleDto>> CreateRule([FromBody] CreateCommissionRuleRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new CreateCommissionRuleCommand(request), ct));

    [HttpGet("statements")]
    public async Task<ActionResult<IReadOnlyList<CommissionStatementDto>>> Statements([FromQuery] Guid? employeeId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct) =>
        Ok(await sender.Send(new GetCommissionStatementsQuery(employeeId, fromDate, toDate), ct));

    [HttpGet("statements/{id:guid}")]
    public async Task<ActionResult<CommissionStatementDto>> Statement(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetCommissionStatementQuery(id), ct));

    [HttpPost("statements/calculate")]
    public async Task<ActionResult<CommissionStatementDto>> Calculate([FromBody] CalculateCommissionStatementRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new CalculateCommissionStatementCommand(request), ct));

    [HttpPost("statements/{id:guid}/review")]
    public async Task<ActionResult<CommissionStatementDto>> Review(Guid id, [FromBody] CommissionStatementActionRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new ReviewCommissionStatementCommand(id, request), ct));

    [HttpPost("statements/{id:guid}/approve")]
    public async Task<ActionResult<CommissionStatementDto>> Approve(Guid id, [FromBody] CommissionStatementActionRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new ApproveCommissionStatementCommand(id, request), ct));

    [HttpPost("statements/{id:guid}/settle")]
    public async Task<ActionResult<CommissionStatementDto>> Settle(Guid id, [FromBody] CommissionStatementActionRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new SettleCommissionStatementCommand(id, request), ct));

    // Legacy endpoint retained for old clients/data flows.
    [HttpPost("statements/{id:guid}/finalize")]
    public async Task<ActionResult<CommissionStatementDto>> Finalize(Guid id, [FromBody] CommissionStatementActionRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new FinalizeCommissionStatementCommand(id, request), ct));
}
