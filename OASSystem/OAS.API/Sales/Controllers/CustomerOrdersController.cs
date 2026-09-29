using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.CustomerOrders.Commands;
using OAS.Application.Sales.CustomerOrders.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/customer-orders")]
public sealed class CustomerOrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerOrderDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerOrdersQuery(request), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerOrderDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerOrderByIdQuery(id), cancellationToken));

    [HttpPost("code/reserve")]
    public async Task<ActionResult<SalesCodeReservationDto>> ReserveCode(
        [FromQuery] DateOnly orderDate,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new ReserveCustomerOrderCodeCommand(orderDate), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CustomerOrderDto>> Create(
        [FromBody] CreateCustomerOrderRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateCustomerOrderCommand(request), cancellationToken);
        var dto = await sender.Send(new GetCustomerOrderByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerOrderDto>> Update(
        Guid id,
        [FromBody] UpdateCustomerOrderRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateCustomerOrderCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetCustomerOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<CustomerOrderDto>> Confirm(
        Guid id,
        [FromBody] ConfirmCustomerOrderRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new ConfirmCustomerOrderCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CustomerOrderDto>> Cancel(
        Guid id,
        [FromBody] CancelCustomerOrderRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new CancelCustomerOrderCommand(id, request), cancellationToken));
}
