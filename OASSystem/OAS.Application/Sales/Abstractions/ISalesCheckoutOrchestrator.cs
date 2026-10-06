using OAS.Contracts.Sales.Checkout;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesCheckoutOrchestrator
{
    Task<CheckoutCustomerOrderResultDto> CheckoutAsync(
        CustomerOrder order,
        CheckoutCustomerOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<CheckoutCustomerOrderResultDto> DeliverAsync(
        CustomerOrder order,
        DeliverCustomerOrderRequest request,
        CancellationToken cancellationToken = default);
}
