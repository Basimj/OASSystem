using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.PostingProfiles.Commands.CreatePostingProfile;
using OAS.Application.Accounting.PostingProfiles.Commands.SetPostingProfileStatus;
using OAS.Application.Accounting.PostingProfiles.Commands.UpdatePostingProfile;
using OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfileById;
using OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfiles;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/posting-profiles")]
public sealed class PostingProfilesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PostingProfileDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
        var result = await sender.Send(new GetPostingProfilesQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostingProfileDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPostingProfileByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PostingProfileDto>> Create(
        [FromBody] CreatePostingProfileRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreatePostingProfileCommand(request), cancellationToken);
        var dto = await sender.Send(new GetPostingProfileByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PostingProfileDto>> Update(
        Guid id,
        [FromBody] UpdatePostingProfileRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdatePostingProfileCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetPostingProfileByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<PostingProfileDto>> SetStatus(
        Guid id,
        [FromBody] SetPostingProfileStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetPostingProfileStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetPostingProfileByIdQuery(id), cancellationToken);
        return Ok(dto);
    }
}
