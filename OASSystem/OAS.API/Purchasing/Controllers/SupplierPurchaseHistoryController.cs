using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.CustomerDemand.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.CustomerDemand;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/suppliers")]
public sealed class SupplierPurchaseHistoryController(ISender sender) : ControllerBase
{
    [HttpGet("{supplierId:guid}/purchase-history")]
    public async Task<ActionResult<PagedResult<SupplierPurchaseHistoryItemDto>>> GetPurchaseHistory(
        Guid supplierId,
        [FromQuery] SupplierPurchaseHistoryQueryRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = request with { SupplierId = supplierId };
        return Ok(await sender.Send(new GetSupplierPurchaseHistoryQuery(normalized), cancellationToken));
    }
}
