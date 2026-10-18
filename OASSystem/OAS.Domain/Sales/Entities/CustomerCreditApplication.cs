using OAS.Domain.Common.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Domain.Sales.Entities;

/// <summary>
/// A use of credit already posted by a sales return. Applying the credit does NOT
/// post the return again; any difference in historical base values is posted as FX.
/// </summary>
public sealed class CustomerCreditApplication : AuditableEntity<Guid>
{
    private CustomerCreditApplication() { }
    public Guid SalesReturnId { get; private set; }
    public Guid CustomerId { get; private set; }
    public CustomerCreditSettlementKind SettlementKind { get; private set; }
    public Guid? TargetSalesInvoiceId { get; private set; }
    public Guid? RefundPaymentVoucherLineId { get; private set; }
    public decimal AppliedSourceBaseAmount { get; private set; }
    public decimal? TargetBaseAmount { get; private set; }
    public decimal? TargetInvoiceCurrencyAmount { get; private set; }
    public decimal? RefundSettlementBaseAmount { get; private set; }
    public Guid? ReversalReceiptVoucherLineId { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }
    public string AppliedBy { get; private set; } = string.Empty;
    public CustomerCreditApplicationStatus Status { get; private set; }
    public DateTime? ReversedAtUtc { get; private set; }
    public string? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static CustomerCreditApplication Apply(Guid id, Guid returnId, Guid customerId,
        Guid targetInvoiceId, decimal sourceBase, decimal targetBase, decimal invoiceCurrencyAmount,
        string actor, DateTime now, string? idempotencyKey = null)
        => New(id, returnId, customerId, CustomerCreditSettlementKind.ApplyToInvoice,
            targetInvoiceId, null, sourceBase, targetBase, invoiceCurrencyAmount, null,
            actor, now, idempotencyKey);

    public static CustomerCreditApplication Refund(Guid id, Guid returnId, Guid customerId,
        Guid paymentVoucherLineId, decimal sourceBase, decimal cashBase,
        string actor, DateTime now, string? idempotencyKey = null)
        => New(id, returnId, customerId, CustomerCreditSettlementKind.RefundByPaymentVoucherLine,
            null, paymentVoucherLineId, sourceBase, null, null, cashBase,
            actor, now, idempotencyKey);

    private static CustomerCreditApplication New(Guid id, Guid returnId, Guid customerId,
        CustomerCreditSettlementKind kind, Guid? invoiceId, Guid? paymentLineId,
        decimal sourceBase, decimal? targetBase, decimal? targetAmount, decimal? refundBase,
        string actor, DateTime now, string? key)
    {
        if (id == Guid.Empty || returnId == Guid.Empty || customerId == Guid.Empty)
            throw new ArgumentException("Credit, return and customer IDs are required.");
        if (sourceBase <= 0m || (targetBase.HasValue && targetBase <= 0m) ||
            (targetAmount.HasValue && targetAmount <= 0m) ||
            (refundBase.HasValue && refundBase <= 0m))
            throw new ArgumentOutOfRangeException(nameof(sourceBase));
        if (kind == CustomerCreditSettlementKind.ApplyToInvoice &&
            (invoiceId is null || paymentLineId is not null || !targetBase.HasValue || !targetAmount.HasValue))
            throw new ArgumentException("An invoice credit must have exactly one target invoice and its historical values.");
        if (kind == CustomerCreditSettlementKind.RefundByPaymentVoucherLine &&
            (paymentLineId is null || invoiceId is not null || !refundBase.HasValue))
            throw new ArgumentException("A refund must have exactly one payment voucher line.");
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 64)
            throw new ArgumentException("Actor is required (64 chars max).", nameof(actor));
        if (now == default) throw new ArgumentException("UTC timestamp required.", nameof(now));
        if (key?.Length > 80) throw new ArgumentOutOfRangeException(nameof(key));
        return new CustomerCreditApplication
        {
            Id = id, SalesReturnId = returnId, CustomerId = customerId,
            SettlementKind = kind, TargetSalesInvoiceId = invoiceId,
            RefundPaymentVoucherLineId = paymentLineId,
            AppliedSourceBaseAmount = Math.Round(sourceBase,4,MidpointRounding.AwayFromZero),
            TargetBaseAmount = targetBase, TargetInvoiceCurrencyAmount = targetAmount,
            RefundSettlementBaseAmount = refundBase, AppliedAtUtc = now,
            AppliedBy = actor.Trim(), IdempotencyKey = string.IsNullOrWhiteSpace(key)?null:key.Trim(),
            Status = CustomerCreditApplicationStatus.Active
        };
    }
    public void Reverse(DateTime now, string actor, string reason, Guid? compensatingReceiptVoucherLineId = null)
    {
        if (Status != CustomerCreditApplicationStatus.Active)
            throw new InvalidOperationException("Only active applications can be reversed.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 300)
            throw new ArgumentException("A reversal reason is required (300 chars max).",nameof(reason));
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 64)
            throw new ArgumentException("Actor required.",nameof(actor));
        if (SettlementKind == CustomerCreditSettlementKind.RefundByPaymentVoucherLine &&
            (!compensatingReceiptVoucherLineId.HasValue || compensatingReceiptVoucherLineId == Guid.Empty))
            throw new InvalidOperationException("A posted compensating customer receipt is required to reverse a refund.");
        if (SettlementKind == CustomerCreditSettlementKind.ApplyToInvoice && compensatingReceiptVoucherLineId.HasValue)
            throw new InvalidOperationException("Invoice credit reversal cannot use a compensating receipt.");
        ReversalReceiptVoucherLineId = compensatingReceiptVoucherLineId;
        ReversedAtUtc = now; ReversedBy = actor; ReversalReason = reason;
        Status = CustomerCreditApplicationStatus.Reversed;
    }
}
