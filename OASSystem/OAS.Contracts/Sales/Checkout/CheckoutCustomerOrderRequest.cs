using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Checkout;

public sealed record CheckoutCustomerOrderRequest(
    Guid CustomerOrderId,
    SalesPaymentPlan PaymentPlan,
    IReadOnlyList<CheckoutPaymentLineRequest> PaymentLines,
    IReadOnlyList<CheckoutSupplierScheduleRequest> SupplierSchedulingDecisions,
    string RowVersion,
    string IdempotencyKey);
