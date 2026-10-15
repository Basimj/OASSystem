using OAS.Contracts.Sales.Checkout;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record CollectSalesInvoicePaymentRequest(
    IReadOnlyList<CheckoutPaymentLineRequest> PaymentLines);
