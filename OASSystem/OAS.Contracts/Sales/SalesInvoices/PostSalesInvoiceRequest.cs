using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record PostSalesInvoiceRequest(
    string RowVersion,
    SalesImmediatePaymentMethod? ImmediatePaymentMethod = null,
    Guid? CashAccountId = null,
    Guid? BankAccountId = null)
{
    /// <summary>
    /// Modern settlement input for standalone/direct sales invoices.
    /// Kept as an init property so older clients using the legacy Cash/Bank fields remain compatible.
    /// </summary>
    public IReadOnlyList<CheckoutPaymentLineRequest> PaymentLines { get; init; } = [];
}
