using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Sales.Checkout;

public sealed record CheckoutPaymentLineRequest(
    PaymentMethod PaymentMethod,
    Guid CurrencyId,
    decimal Amount,
    Guid? CashAccountId,
    Guid? BankAccountId,
    string? ReferenceNumber,
    DateOnly? ReferenceDate,
    string? Description)
{
    // Used only with PaymentMethod.Other. Kept optional so the task-defined constructor
    // remains source-compatible for Cash/Card/BankTransfer/Cheque callers.
    public Guid? SettlementAccountId { get; init; }
}
