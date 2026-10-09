using OAS.Client.Services.Http;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.Client.Sales.Checkout.Services;

public sealed class SalesCheckoutClientService(OasApiClient api) : ISalesCheckoutClientService
{
    public Task<CustomerPrescriptionContextDto?> GetCustomerContextAsync(Guid customerId, CancellationToken ct = default)
        => api.GetAsync<CustomerPrescriptionContextDto>($"api/sales/checkout/customers/{customerId:D}/context", ct);

    public Task<SalesCheckoutContextDto?> GetCheckoutContextAsync(Guid orderId, CancellationToken ct = default)
        => api.GetAsync<SalesCheckoutContextDto>($"api/sales/orders/{orderId:D}/checkout-context", ct);

    public Task<CustomerOrderAvailabilityDto?> AssessAvailabilityAsync(Guid orderId, CancellationToken ct = default)
        => api.PostAsync<object, CustomerOrderAvailabilityDto>($"api/sales/orders/{orderId:D}/availability", new { }, ct);

    public Task<CheckoutCustomerOrderResultDto?> CheckoutAsync(Guid orderId, CheckoutCustomerOrderRequest request, CancellationToken ct = default)
        => api.PostAsync<CheckoutCustomerOrderRequest, CheckoutCustomerOrderResultDto>($"api/sales/orders/{orderId:D}/checkout", request, ct);

    public Task<CheckoutCustomerOrderResultDto?> DeliverAsync(Guid orderId, DeliverCustomerOrderRequest request, CancellationToken ct = default)
        => api.PostAsync<DeliverCustomerOrderRequest, CheckoutCustomerOrderResultDto>($"api/sales/orders/{orderId:D}/delivery", request, ct);

    public Task<ApiCallResult<CheckoutCustomerOrderResultDto>> CheckoutResultAsync(Guid orderId, CheckoutCustomerOrderRequest request, CancellationToken ct = default)
        => api.PostResultAsync<CheckoutCustomerOrderRequest, CheckoutCustomerOrderResultDto>($"api/sales/orders/{orderId:D}/checkout", request, ct);

    public Task<ApiCallResult<CheckoutCustomerOrderResultDto>> DeliverResultAsync(Guid orderId, DeliverCustomerOrderRequest request, CancellationToken ct = default)
        => api.PostResultAsync<DeliverCustomerOrderRequest, CheckoutCustomerOrderResultDto>($"api/sales/orders/{orderId:D}/delivery", request, ct);
}
