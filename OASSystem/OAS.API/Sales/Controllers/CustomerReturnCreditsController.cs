using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Returns.Queries;
using OAS.Contracts.Sales.Returns;

namespace OAS.API.Sales.Controllers;

/// <summary>
/// Read-only credit preview. Financial settlements must use the future audited
/// customer-credit application/refund commands, never the preview as authority.
/// </summary>
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/customer-credits")]
public sealed class CustomerReturnCreditsController(ISender sender) : ControllerBase
{
    [HttpGet("{customerId:guid}/preview")]
    public async Task<ActionResult<CustomerReturnCreditPreviewDto>> GetPreview(
        Guid customerId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(
            new GetCustomerReturnCreditPreviewQuery(customerId), cancellationToken));
}
