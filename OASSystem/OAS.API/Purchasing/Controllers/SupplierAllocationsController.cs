using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.SupplierAllocations.Queries;
using OAS.Contracts.Purchasing.SupplierAllocations;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/supplier-allocations")]
public sealed class SupplierAllocationsController(ISender sender) : ControllerBase
{
    [HttpGet("{supplierId:guid}")]
    public async Task<ActionResult<SupplierAllocationCenterDto>> Get(
        Guid supplierId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetSupplierAllocationCenterQuery(supplierId), cancellationToken));
}
