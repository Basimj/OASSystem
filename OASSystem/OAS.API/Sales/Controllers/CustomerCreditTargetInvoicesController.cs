using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Returns.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.SalesInvoices;

namespace OAS.API.Sales.Controllers;

/// <summary>Server-filtered eligible invoice selector for customer-credit allocation.</summary>
[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/customer-credit-target-invoices")]
public sealed class CustomerCreditTargetInvoicesController(ISender sender) : ControllerBase
{
    [HttpGet("{customerId:guid}")]
    public async Task<ActionResult<PagedResult<SalesInvoiceDto>>> Get(
        Guid customerId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await sender.Send(
            new GetCustomerCreditTargetInvoicesQuery(customerId, pageNumber, pageSize), ct));
}
