using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.EndOfService;
using OAS.Contracts.Features.Employees.EndOfService;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/end-of-service")]
public sealed class EndOfServiceController(ISender sender):ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<EndOfServiceSettlementDto>>> List([FromQuery]Guid? employeeId,CancellationToken ct)=>Ok(await sender.Send(new GetEndOfServiceSettlementsQuery(employeeId),ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<EndOfServiceSettlementDto>> Get(Guid id,CancellationToken ct)=>Ok(await sender.Send(new GetEndOfServiceSettlementByIdQuery(id),ct));
    [HttpPost] public async Task<ActionResult<Guid>> Create(CreateEndOfServiceSettlementRequest r,CancellationToken ct)=>Ok(await sender.Send(new CreateEndOfServiceSettlementCommand(r),ct));
    [HttpPost("{id:guid}/calculate")] public async Task<ActionResult<Guid>> Calculate(Guid id,CalculateEndOfServiceRequest r,CancellationToken ct)=>Ok(await sender.Send(new CalculateEndOfServiceSettlementCommand(id,r),ct));
    [HttpPost("{id:guid}/review")] public async Task<ActionResult<Guid>> Review(Guid id,EndOfServiceTransitionRequest r,CancellationToken ct)=>Ok(await sender.Send(new ReviewEndOfServiceSettlementCommand(id,r),ct));
    [HttpPost("{id:guid}/approve")] public async Task<ActionResult<Guid>> Approve(Guid id,EndOfServiceTransitionRequest r,CancellationToken ct)=>Ok(await sender.Send(new ApproveEndOfServiceSettlementCommand(id,r),ct));
    [HttpPost("{id:guid}/post")] public async Task<ActionResult<Guid>> Post(Guid id,EndOfServiceTransitionRequest r,CancellationToken ct)=>Ok(await sender.Send(new PostEndOfServiceSettlementCommand(id,r),ct));
    [HttpPost("{id:guid}/payment")] public async Task<ActionResult<Guid>> Pay(Guid id,EndOfServicePaymentRequest r,CancellationToken ct)=>Ok(await sender.Send(new PayEndOfServiceSettlementCommand(id,r),ct));
    [HttpPost("{id:guid}/complete")] public async Task<ActionResult<Guid>> Complete(Guid id,EndOfServiceTransitionRequest r,CancellationToken ct)=>Ok(await sender.Send(new CompleteEndOfServiceSettlementCommand(id,r),ct));
}
