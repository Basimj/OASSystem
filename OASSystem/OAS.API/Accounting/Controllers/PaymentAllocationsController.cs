using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.PaymentAllocations.Commands.CreatePaymentAllocation;
using OAS.Application.Accounting.PaymentAllocations.Queries.GetPaymentAllocationById;
using OAS.Application.Accounting.PaymentAllocations.Queries.GetPaymentAllocations;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/payment-allocations")]
public sealed class PaymentAllocationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentAllocationDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentAllocationsQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentAllocationDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentAllocationByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentAllocationDto>> Create(
        [FromBody] CreatePaymentAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreatePaymentAllocationCommand(request), cancellationToken);
        var dto = await sender.Send(new GetPaymentAllocationByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }
}
