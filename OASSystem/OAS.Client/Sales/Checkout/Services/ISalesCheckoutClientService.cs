using OAS.Client.Services.Http;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.Client.Sales.Checkout.Services;

public interface ISalesCheckoutClientService
{
    Task<CustomerPrescriptionContextDto?> GetCustomerContextAsync(Guid customerId, CancellationToken ct = default);
    Task<SalesCheckoutContextDto?> GetCheckoutContextAsync(Guid orderId, CancellationToken ct = default);
    Task<CustomerOrderAvailabilityDto?> AssessAvailabilityAsync(Guid orderId, CancellationToken ct = default);
    Task<CheckoutCustomerOrderResultDto?> CheckoutAsync(Guid orderId, CheckoutCustomerOrderRequest request, CancellationToken ct = default);
    Task<CheckoutCustomerOrderResultDto?> DeliverAsync(Guid orderId, DeliverCustomerOrderRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CheckoutCustomerOrderResultDto>> CheckoutResultAsync(Guid orderId, CheckoutCustomerOrderRequest request, CancellationToken ct = default);
    Task<ApiCallResult<CheckoutCustomerOrderResultDto>> DeliverResultAsync(Guid orderId, DeliverCustomerOrderRequest request, CancellationToken ct = default);
}
