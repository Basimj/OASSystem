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
        [FromQuery] PageRequest request,
        [FromQuery] Guid? salesInvoiceId,
        [FromQuery] Guid? customerId,
        [FromQuery] SalesReturnStatus? status,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSalesReturnsQuery(request, salesInvoiceId, customerId, status), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesReturnDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSalesReturnByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SalesReturnDto>> Create(
        [FromBody] CreateSalesReturnRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateSalesReturnCommand(request), cancellationToken);
        var dto = await sender.Send(new GetSalesReturnByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<SalesReturnDto>> Confirm(
        Guid id,
        [FromBody] SalesReturnActionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ConfirmSalesReturnCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<SalesReturnPostingResultDto>> Post(
        Guid id,
        [FromBody] SalesReturnActionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new PostSalesReturnCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<SalesReturnDto>> Cancel(
        Guid id,
        [FromBody] CancelSalesReturnRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CancelSalesReturnCommand(id, request), cancellationToken));
}
