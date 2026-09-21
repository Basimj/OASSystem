using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;
using OAS.Application.Accounting.Journals.Commands.ReverseJournalEntry;
using OAS.Application.Accounting.Journals.Commands.SetJournalEntryStatus;
using OAS.Application.Accounting.Journals.Commands.UpdateJournalEntry;
using OAS.Application.Accounting.Journals.Queries.GetJournalEntries;
using OAS.Application.Accounting.Journals.Queries.GetJournalEntryById;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/journals")]
public sealed class JournalEntriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<JournalEntryDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetJournalEntriesQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JournalEntryDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetJournalEntryByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<JournalEntryDto>> Create(
        [FromBody] CreateJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateJournalEntryCommand(request), cancellationToken);
        var dto = await sender.Send(new GetJournalEntryByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JournalEntryDto>> Update(
        Guid id,
        [FromBody] UpdateJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateJournalEntryCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetJournalEntryByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<JournalEntryDto>> SetStatus(
        Guid id,
        [FromBody] SetJournalEntryStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetJournalEntryStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetJournalEntryByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<JournalEntryDto>> Reverse(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reversalId = await sender.Send(new ReverseJournalEntryCommand(id), cancellationToken);
        var dto = await sender.Send(new GetJournalEntryByIdQuery(reversalId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = reversalId }, dto);
    }
}
