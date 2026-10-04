using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.CustomerOrders.Queries.AssessCustomerOrderAvailability;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/orders")]
public sealed class CustomerOrderAvailabilityController(ISender sender) : ControllerBase
{
    [HttpPost("{orderId:guid}/availability")]
    public async Task<ActionResult<CustomerOrderAvailabilityDto>> Assess(
        Guid orderId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new AssessCustomerOrderAvailabilityQuery(orderId), cancellationToken));
}
