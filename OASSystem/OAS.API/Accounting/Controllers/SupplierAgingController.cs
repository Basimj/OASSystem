using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.Reports.Queries.GetSupplierAging;
using OAS.Contracts.Accounting.Reports;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/reports/supplier-aging")]
public sealed class SupplierAgingController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SupplierAgingReportDto>> Get(
        [FromQuery] DateOnly asOfDate,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetSupplierAgingQuery(
            new SupplierAgingFilterDto(asOfDate, supplierId, search, pageNumber, pageSize)), cancellationToken));
}
