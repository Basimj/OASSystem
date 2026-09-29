using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Prescriptions.Commands;
using OAS.Application.Sales.Prescriptions.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/prescriptions")]
public sealed class PrescriptionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PrescriptionDto>>> Get(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPrescriptionsQuery(request), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrescriptionDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPrescriptionByIdQuery(id), cancellationToken));

    [HttpPost("code/reserve")]
    public async Task<ActionResult<SalesCodeReservationDto>> ReserveCode(
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new ReservePrescriptionCodeCommand(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PrescriptionDto>> Create(
        [FromBody] CreatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreatePrescriptionCommand(request), cancellationToken);
        var dto = await sender.Send(new GetPrescriptionByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PrescriptionDto>> Update(
        Guid id,
        [FromBody] UpdatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdatePrescriptionCommand(id, request), cancellationToken);
        return Ok(await sender.Send(new GetPrescriptionByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/revisions")]
    public async Task<ActionResult<PrescriptionDto>> CreateRevision(
        Guid id,
        [FromBody] CreatePrescriptionRevisionRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new CreatePrescriptionRevisionCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<PrescriptionDto>> SetStatus(
        Guid id,
        [FromBody] SetPrescriptionStatusRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new SetPrescriptionStatusCommand(id, request), cancellationToken));
}
