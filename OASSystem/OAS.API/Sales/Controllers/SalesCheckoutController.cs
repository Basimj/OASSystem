using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Sales.Checkout.Commands;
using OAS.Application.Sales.Checkout.Queries;
using OAS.Application.Sales.Prescriptions.Queries.CustomerPrescriptionContext;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.API.Sales.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/sales")]
public sealed class SalesCheckoutController(ISender sender) : ControllerBase
{
    [HttpGet("checkout/customers/{customerId:guid}/context")]
    public async Task<ActionResult<CustomerPrescriptionContextDto>> GetCustomerContext(
        Guid customerId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCustomerPrescriptionContextQuery(customerId), cancellationToken));

    [HttpGet("orders/{orderId:guid}/checkout-context")]
    public async Task<ActionResult<SalesCheckoutContextDto>> GetCheckoutContext(
        Guid orderId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetSalesCheckoutContextQuery(orderId), cancellationToken));

    [HttpPost("orders/{orderId:guid}/checkout")]
    public async Task<ActionResult<CheckoutCustomerOrderResultDto>> Checkout(
        Guid orderId,
        [FromBody] CheckoutCustomerOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CustomerOrderId != Guid.Empty && request.CustomerOrderId != orderId)
        {
            ModelState.AddModelError(nameof(request.CustomerOrderId), "معرف طلب العميل في المسار لا يطابق المعرف الموجود في الطلب.");
            return ValidationProblem(ModelState);
        }

        var normalized = request with { CustomerOrderId = orderId };
        return Ok(await sender.Send(new CheckoutCustomerOrderCommand(normalized), cancellationToken));
    }

    [HttpPost("orders/{orderId:guid}/delivery")]
    public async Task<ActionResult<CheckoutCustomerOrderResultDto>> Deliver(
        Guid orderId,
        [FromBody] DeliverCustomerOrderRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new DeliverCustomerOrderCommand(orderId, request), cancellationToken));
}
