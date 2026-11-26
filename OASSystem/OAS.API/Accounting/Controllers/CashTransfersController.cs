using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.CashTransfers;
using OAS.Contracts.Accounting.CashTransfers;

namespace OAS.API.Accounting.Controllers;

[ApiController, Authorize, EnableRateLimiting("api"), Route("api/accounting/cash-transfers")]
public sealed class CashTransfersController(ISender sender) : ControllerBase
{
    [HttpGet("lookups")]
    public async Task<ActionResult<CashTransferLookupsDto>> Lookups(CancellationToken ct, [FromQuery] DateOnly? date = null) => Ok(await sender.Send(new GetCashTransferLookupsQuery(date), ct));
    [HttpGet]
    public async Task<ActionResult<CashTransferPageDto>> List([FromQuery] int page = 1, [FromQuery] string? search = null, [FromQuery] string? type = null, [FromQuery] int? status = null, CancellationToken ct = default)
        => Ok(await sender.Send(new GetCashTransfersQuery(page, search, type, status), ct));
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CashTransferRequest data, CancellationToken ct) => Ok(await sender.Send(new SaveCashTransferCommand(null, data), ct));
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Guid>> Update(Guid id, CashTransferRequest data, CancellationToken ct) => Ok(await sender.Send(new SaveCashTransferCommand(id, data), ct));
    [HttpPost("{id:guid}/{actionName}")]
    public async Task<ActionResult<Guid>> Action(Guid id, string actionName, CashTransferActionRequest data, CancellationToken ct)
        => Ok(await sender.Send(new CashTransferActionCommand(id, actionName, data), ct));
}
