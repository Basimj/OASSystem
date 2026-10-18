namespace OAS.Contracts.Sales.Returns;

public sealed record ApplyCustomerReturnCreditRequest(
    Guid SalesReturnId,
    Guid TargetSalesInvoiceId,
    decimal TargetInvoiceCurrencyAmount,
    string? IdempotencyKey = null);

/// <summary>Refund values are in payment-currency units; the available credit is base currency.</summary>
public sealed record RefundCustomerReturnCreditRequest(
    Guid SalesReturnId,
    Guid CurrencyId,
    decimal Amount,
    byte PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    Guid? SettlementAccountId,
    string? ReferenceNumber = null,
    string? Description = null,
    string? IdempotencyKey = null);

public sealed record ReverseCustomerReturnCreditRequest(string RowVersion, string Reason, Guid? CompensatingReceiptVoucherLineId = null);

public sealed record CustomerCreditSettlementDto(
    Guid Id,
    Guid SalesReturnId,
    Guid CustomerId,
    byte SettlementKind,
    Guid? TargetSalesInvoiceId,
    Guid? RefundPaymentVoucherLineId,
    Guid? ReversalReceiptVoucherLineId,
    decimal AppliedSourceBaseAmount,
    decimal? TargetBaseAmount,
    decimal? TargetInvoiceCurrencyAmount,
    decimal? RefundSettlementBaseAmount,
    DateTime AppliedAtUtc,
    byte Status,
    string RowVersion);

public sealed record CustomerCreditBalanceDto(
    Guid CustomerId,
    decimal EarnedBase,
    decimal ConsumedBase,
    decimal AvailableBase,
    IReadOnlyList<CustomerReturnCreditItemDto> Returns,
    IReadOnlyList<CustomerCreditSettlementDto> Applications);
