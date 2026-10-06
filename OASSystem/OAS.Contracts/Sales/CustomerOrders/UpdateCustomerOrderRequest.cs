using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record UpdateCustomerOrderRequest(
    Guid CustomerId,
    Guid? PrescriptionRevisionId,
    DateOnly OrderDate,
    DateOnly? RequiredDate,
    Guid CurrencyId,
    TaxCalculationMode TaxCalculationMode,
    SalesPaymentTermType PaymentTermType,
    string? Notes,
    IReadOnlyList<CustomerOrderLineRequest> Lines,
    string RowVersion)
{
    // Transitional default keeps legacy callers valid until the Checkout/Application stage is wired.
    // New callers should send PaymentPlan explicitly.
    public SalesPaymentPlan PaymentPlan { get; init; } =
        PaymentTermType == SalesPaymentTermType.Credit
            ? SalesPaymentPlan.AccountCredit
            : SalesPaymentPlan.FullNow;
}
