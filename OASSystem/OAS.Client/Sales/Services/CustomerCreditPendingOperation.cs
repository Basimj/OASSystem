using OAS.Contracts.Sales.Returns;

namespace OAS.Client.Sales.Services;

/// <summary>
/// Exact settlement request saved in browser storage BEFORE the first network call.
/// Replays must never create a new idempotency key or silently change the payload.
/// </summary>
public sealed record CustomerCreditPendingOperation(
    Guid CustomerId,
    string Kind,
    ApplyCustomerReturnCreditRequest? Apply,
    RefundCustomerReturnCreditRequest? Refund)
{
    // Shared by the customer-credit workspace and the sales-return refund dialog.
    // A pending refund must not be lost or overwritten by changing screens.
    public static string StorageKeyFor(Guid customerId, string? databaseProfileKey) =>
        $"oas.sales.customer-credit.pending.v1:{databaseProfileKey ?? "default"}:{customerId:N}";

    public string IdempotencyKey => Kind == "apply"
        ? Apply?.IdempotencyKey ?? string.Empty
        : Refund?.IdempotencyKey ?? string.Empty;

    public static CustomerCreditPendingOperation ForApply(Guid customerId, Guid returnId,
        Guid invoiceId, decimal amount) => new(customerId, "apply",
            new ApplyCustomerReturnCreditRequest(returnId, invoiceId, amount,
                Guid.NewGuid().ToString("N")), null);

    public static CustomerCreditPendingOperation ForRefund(Guid customerId, Guid returnId,
        Guid currencyId, decimal amount, byte method, Guid? cashId, Guid? bankId) =>
        new(customerId, "refund", null,
            new RefundCustomerReturnCreditRequest(returnId, currencyId, amount, method,
                cashId, bankId, null, null, "استرداد رصيد مرتجع",
                Guid.NewGuid().ToString("N")));

    /// <summary>Reject damaged or foreign-customer snapshots rather than issuing a new payment.</summary>
    public bool IsValidFor(Guid customerId) =>
        customerId != Guid.Empty && CustomerId == customerId &&
        Guid.TryParseExact(IdempotencyKey, "N", out var id) && id != Guid.Empty &&
        (Kind switch
        {
            "apply" => Apply is { } apply && Refund is null &&
                apply.SalesReturnId != Guid.Empty &&
                apply.TargetSalesInvoiceId != Guid.Empty &&
                apply.TargetInvoiceCurrencyAmount > 0m,
            "refund" => Refund is { } refund && Apply is null &&
                refund.SalesReturnId != Guid.Empty && refund.CurrencyId != Guid.Empty &&
                refund.Amount > 0m && refund.PaymentMethod is 1 or 3 &&
                (refund.PaymentMethod == 1
                    ? refund.CashAccountId.HasValue && !refund.BankAccountId.HasValue
                    : refund.BankAccountId.HasValue && !refund.CashAccountId.HasValue),
            _ => false
        });
}
