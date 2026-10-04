using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.CustomerAdvances.Commands;
using OAS.Contracts.Accounting.CustomerAdvances;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/customer-advances")]
public sealed class CustomerAdvancesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CustomerAdvanceDto>> Create(
        [FromBody] CreateCustomerAdvanceRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new CreateCustomerAdvanceCommand(request), cancellationToken));

    [HttpPost("{id:guid}/apply")]
    public async Task<ActionResult<CustomerAdvanceApplicationDto>> Apply(
        Guid id,
        [FromBody] ApplyCustomerAdvanceRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new ApplyCustomerAdvanceCommand(id, request), cancellationToken));
}
