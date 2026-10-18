using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Returns.Commands;
using OAS.Application.Sales.Returns.Queries;
using OAS.Contracts.Sales.Returns;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/customer-credits")]
public sealed class CustomerReturnCreditsController(ISender sender) : ControllerBase
{
    [HttpGet("{customerId:guid}/preview")]
    public async Task<ActionResult<CustomerReturnCreditPreviewDto>> GetPreview(Guid customerId,CancellationToken ct)
        => Ok(await sender.Send(new GetCustomerReturnCreditPreviewQuery(customerId),ct));

    [HttpGet("{customerId:guid}")]
    public async Task<ActionResult<CustomerCreditBalanceDto>> GetBalance(Guid customerId,CancellationToken ct)
        => Ok(await sender.Send(new GetCustomerCreditBalanceQuery(customerId),ct));

    [HttpPost("{customerId:guid}/apply")]
    public async Task<ActionResult<CustomerCreditSettlementDto>> Apply(Guid customerId,
        [FromBody] ApplyCustomerReturnCreditRequest request,CancellationToken ct)
        => Ok(await sender.Send(new ApplyCustomerReturnCreditCommand(customerId,request),ct));

    [HttpPost("{customerId:guid}/refund")]
    public async Task<ActionResult<CustomerCreditSettlementDto>> Refund(Guid customerId,
        [FromBody] RefundCustomerReturnCreditRequest request,CancellationToken ct)
        => Ok(await sender.Send(new RefundCustomerReturnCreditCommand(customerId,request),ct));

    [HttpPost("{customerId:guid}/applications/{id:guid}/reverse")]
    public async Task<ActionResult<CustomerCreditSettlementDto>> Reverse(Guid customerId,Guid id,
        [FromBody] ReverseCustomerReturnCreditRequest request,CancellationToken ct)
        => Ok(await sender.Send(new ReverseCustomerReturnCreditCommand(customerId,id,request),ct));
}
