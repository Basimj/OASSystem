using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.SupplierOpenings;
using OAS.Contracts.Accounting.SupplierOpenings;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/supplier-openings")]
public sealed class SupplierOpeningsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierOpeningBatchDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetSupplierOpeningBatchQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SupplierOpeningBatchDto>> Create(
        [FromBody] CreateSupplierOpeningBatchRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateSupplierOpeningBatchCommand(request), cancellationToken);
        var dto = await sender.Send(new GetSupplierOpeningBatchQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<Guid>> Post(Guid id, [FromBody] PostSupplierOpeningBatchRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new PostSupplierOpeningBatchCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/link-existing-journal")]
    public async Task<ActionResult<Guid>> LinkExistingJournal(Guid id, [FromBody] LinkSupplierOpeningJournalRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new LinkSupplierOpeningJournalCommand(id, request), cancellationToken));

    [HttpPost("items/{itemId:guid}/apply")]
    public async Task<ActionResult<Guid>> Apply(Guid itemId, [FromBody] ApplySupplierOpeningRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ApplySupplierOpeningCommand(itemId, request), cancellationToken));

    [HttpPost("applications/{applicationId:guid}/reverse")]
    public async Task<IActionResult> Reverse(Guid applicationId, [FromBody] ReverseSupplierOpeningApplicationRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ReverseSupplierOpeningApplicationCommand(applicationId, request), cancellationToken);
        return NoContent();
    }
}
