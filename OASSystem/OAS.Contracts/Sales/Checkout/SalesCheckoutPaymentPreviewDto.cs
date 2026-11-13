namespace OAS.Contracts.Sales.Checkout;

/// <summary>Read-only estimate using the same FX service as checkout. No receipts are created.</summary>
public sealed record SalesCheckoutPaymentPreviewDto(
    decimal PaidBaseAmount,
    decimal RequiredBaseAmount,
    decimal PaidInOrderCurrency,
    decimal RemainingInOrderCurrency);
