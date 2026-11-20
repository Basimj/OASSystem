using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.CustomerOrders;

namespace OAS.Client.Sales.Services;

/// <summary>
/// Pending attempt is saved before network operations. Repeating the attempt uses
/// the same SO code, payment payload and checkout key, never a fresh transaction.
/// </summary>
public sealed record SalesInvoicePendingCheckout(
    string SourceInvoiceCode,
    CreateCustomerOrderRequest Order,
    IReadOnlyList<CheckoutPaymentLineRequest> Payments,
    string CheckoutKey,
    Guid? OrderId = null,
    bool CheckoutSubmitted = false,
    bool DraftOnly = false,
    DateOnly? DraftPostingDate = null,
    Guid? DraftSalesEmployeeId = null)
{
    public const string StoragePrefix = "oas.sales.invoice.first.pending.v2:";
    public static string StorageKey(string profile) => StoragePrefix + profile;

    // Keep clinical measurements only in the active tab, not persistent storage.
    public SalesInvoicePendingCheckoutMarker ToMarker() =>
        new(SourceInvoiceCode, Order.OrderCode, Order.CustomerId, OrderId);

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(SourceInvoiceCode) &&
        Order.OrderCode.StartsWith("SO-", StringComparison.OrdinalIgnoreCase) &&
        Order.CustomerId != Guid.Empty && Order.Lines.Count > 0 &&
        !string.IsNullOrWhiteSpace(CheckoutKey) &&
        Payments.All(x => x.Amount > 0m && x.CurrencyId != Guid.Empty);

    public SalesInvoicePendingCheckout WithOrder(Guid id) => this with { OrderId = id };
}

public sealed record SalesInvoicePendingCheckoutMarker(
    string SourceInvoiceCode, string OrderCode, Guid CustomerId, Guid? OrderId);
