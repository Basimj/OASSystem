using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Prescriptions.Queries.CustomerPrescriptionContext;
using OAS.Application.Sales.Prescriptions.Queries.CustomerPrescriptionHistory;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales/customers/{customerId:guid}")]
public sealed class CustomerPrescriptionContextController(ISender sender) : ControllerBase
{
    [HttpGet("prescription-context")]
    public async Task<ActionResult<CustomerPrescriptionContextDto>> GetPrescriptionContext(
        Guid customerId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerPrescriptionContextQuery(customerId), cancellationToken));

    [HttpGet("prescriptions/history")]
    public async Task<ActionResult<IReadOnlyList<PrescriptionHistoryItemDto>>> GetPrescriptionHistory(
        Guid customerId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerPrescriptionHistoryQuery(customerId), cancellationToken));
}
