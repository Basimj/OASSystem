using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.OrderOperations.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OrderOperations;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/order-operations")]
public sealed class CustomerOrderOperationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerOrderOperationsItemDto>>> Get(
        [FromQuery] CustomerOrderOperationsQueryRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerOrderOperationsQuery(request), cancellationToken));

    [HttpGet("summary")]
    public async Task<ActionResult<CustomerOrderOperationsSummaryDto>> GetSummary(
        [FromQuery] CustomerOrderOperationsQueryRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerOrderOperationsSummaryQuery(request), cancellationToken));

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<CustomerOrderOperationsDetailsDto>> GetById(
        Guid orderId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerOrderOperationsDetailsQuery(orderId), cancellationToken));
}
