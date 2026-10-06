namespace OAS.Contracts.Sales.Checkout;

public sealed record DeliverCustomerOrderRequest(
    IReadOnlyList<CheckoutPaymentLineRequest> PaymentLines,
    string RowVersion,
    string IdempotencyKey);
