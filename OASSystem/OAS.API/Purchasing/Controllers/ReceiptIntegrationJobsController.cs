using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Purchasing.PurchaseReceipts.Queries;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Domain.Purchasing.Entities;

namespace OAS.API.Purchasing.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/purchasing/receipt-follow-ups")]
public sealed class ReceiptIntegrationJobsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PurchaseReceiptFollowUpPageDto>> List(
        [FromQuery] Guid? purchaseReceiptId, [FromQuery] ReceiptFollowUpStatus? status,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetPurchaseReceiptFollowUpsQuery(purchaseReceiptId, status, pageNumber, pageSize), ct));

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
        => await sender.Send(new RetryPurchaseReceiptFollowUpCommand(id), ct) ? Ok() : NotFound();
}
