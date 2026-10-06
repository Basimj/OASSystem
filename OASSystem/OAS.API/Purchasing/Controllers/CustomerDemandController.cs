using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.CustomerDemand.Commands;
using OAS.Application.Purchasing.CustomerDemand.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.CustomerDemand;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/customer-demand")]
public sealed class CustomerDemandController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDemandTrackingItemDto>>> Get(
        [FromQuery] CustomerDemandTrackingQueryRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerDemandTrackingQuery(request), cancellationToken));

    [HttpGet("summary")]
    public async Task<ActionResult<CustomerDemandTrackingSummaryDto>> GetSummary(
        [FromQuery] CustomerDemandTrackingQueryRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerDemandTrackingSummaryQuery(request), cancellationToken));

    [HttpGet("{lineId:guid}")]
    public async Task<ActionResult<CustomerDemandTrackingDetailsDto>> GetById(
        Guid lineId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerDemandTrackingDetailsQuery(lineId), cancellationToken));

    [HttpPatch("{lineId:guid}/supplier")]
    public async Task<ActionResult<CustomerDemandTrackingDetailsDto>> AssignSupplier(
        Guid lineId,
        [FromBody] AssignCustomerDemandSupplierRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new AssignCustomerDemandSupplierCommand(lineId, request), cancellationToken);
        return Ok(await sender.Send(new GetCustomerDemandTrackingDetailsQuery(lineId), cancellationToken));
    }

    [HttpPatch("{lineId:guid}/schedule")]
    public async Task<ActionResult<CustomerDemandTrackingDetailsDto>> Schedule(
        Guid lineId,
        [FromBody] ScheduleCustomerDemandRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ScheduleCustomerDemandCommand(lineId, request), cancellationToken);
        return Ok(await sender.Send(new GetCustomerDemandTrackingDetailsQuery(lineId), cancellationToken));
    }

    [HttpPost("{lineId:guid}/create-po")]
    public async Task<ActionResult<Guid>> CreatePurchaseOrder(
        Guid lineId,
        [FromBody] CreateCustomerDemandPurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        var purchaseOrderId = await sender.Send(
            new CreateCustomerDemandPurchaseOrderCommand(lineId, request), cancellationToken);
        return Created($"/api/purchasing/orders/{purchaseOrderId}", purchaseOrderId);
    }

    [HttpPost("{lineId:guid}/resource-remaining")]
    public async Task<ActionResult<Guid>> ResourceRemaining(
        Guid lineId,
        [FromBody] ResourceCustomerDemandRemainingRequest request,
        CancellationToken cancellationToken)
    {
        var purchaseOrderId = await sender.Send(
            new ResourceCustomerDemandRemainingCommand(lineId, request), cancellationToken);
        return Created($"/api/purchasing/orders/{purchaseOrderId}", purchaseOrderId);
    }
}
