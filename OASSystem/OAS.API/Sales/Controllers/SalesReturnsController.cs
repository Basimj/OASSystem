using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Returns.Commands;
using OAS.Application.Sales.Returns.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Returns;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/returns")]
public sealed class SalesReturnsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SalesReturnDto>>> Get(
        [FromQuery] PageRequest request, [FromQuery] Guid? salesInvoiceId,
        [FromQuery] Guid? customerId, [FromQuery] SalesReturnStatus? status,
        CancellationToken ct) =>
        Ok(await sender.Send(new GetSalesReturnsQuery(request, salesInvoiceId, customerId, status), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesReturnDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetSalesReturnByIdQuery(id), ct));

    [HttpGet("{id:guid}/posting-readiness")]
    public async Task<ActionResult<SalesReturnPostingReadinessDto>> PostingReadiness(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetSalesReturnPostingReadinessQuery(id), ct));

    [HttpGet("invoices/{invoiceId:guid}/return-policy")]
    public async Task<ActionResult<SalesReturnCreationPolicyDto>> ReturnPolicy(Guid invoiceId, CancellationToken ct) =>
        Ok(await sender.Send(new GetSalesReturnCreationPolicyQuery(invoiceId), ct));

    [HttpPost]
    public async Task<ActionResult<SalesReturnDto>> Create([FromBody] CreateSalesReturnRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateSalesReturnCommand(request), ct);
        var dto = await sender.Send(new GetSalesReturnByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<SalesReturnDto>> Confirm(Guid id, [FromBody] SalesReturnActionRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new ConfirmSalesReturnCommand(id, request), ct));

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<SalesReturnPostingResultDto>> Post(Guid id, [FromBody] SalesReturnActionRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new PostSalesReturnCommand(id, request), ct));

    [HttpPost("{id:guid}/lines/{lineId:guid}/inspect")]
    public async Task<ActionResult<SalesReturnDto>> InspectLine(Guid id, Guid lineId, [FromBody] InspectSalesReturnLineRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new InspectSalesReturnLineCommand(id, lineId, request), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<SalesReturnDto>> Cancel(Guid id, [FromBody] CancelSalesReturnRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new CancelSalesReturnCommand(id, request), ct));
}
