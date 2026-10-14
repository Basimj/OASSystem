using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.Entities;

public sealed class BankReconciliationMatch : AuditableEntity<Guid>
{
    private BankReconciliationMatch() { }

    public Guid BankReconciliationId { get; private set; }
    public Guid BankStatementLineId { get; private set; }
    public Guid? JournalEntryLineId { get; private set; }
    public Guid? ReceiptVoucherLineId { get; private set; }
    public Guid? PaymentVoucherLineId { get; private set; }
    public decimal MatchedAmount { get; private set; }
    public BankMatchType MatchType { get; private set; }
    public BankMatchMethod MatchMethod { get; private set; }
    public bool IsVoided { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }

    public static BankReconciliationMatch Create(
        Guid id,
        Guid bankReconciliationId,
        Guid bankStatementLineId,
        Guid? journalEntryLineId,
        Guid? receiptVoucherLineId,
        Guid? paymentVoucherLineId,
        decimal matchedAmount,
        BankMatchType matchType,
        BankMatchMethod matchMethod)
    {
        if (id == Guid.Empty) throw new DomainException("Bank reconciliation match id is required.");
        if (bankReconciliationId == Guid.Empty) throw new DomainException("Bank reconciliation is required.");
        if (bankStatementLineId == Guid.Empty) throw new DomainException("Bank statement line is required.");
        if (matchedAmount <= 0m) throw new DomainException("Matched amount must be greater than zero.");

        ValidateSource(matchType, journalEntryLineId, receiptVoucherLineId, paymentVoucherLineId);

        return new BankReconciliationMatch
        {
            Id = id,
            BankReconciliationId = bankReconciliationId,
            BankStatementLineId = bankStatementLineId,
            JournalEntryLineId = Normalize(journalEntryLineId),
            ReceiptVoucherLineId = Normalize(receiptVoucherLineId),
            PaymentVoucherLineId = Normalize(paymentVoucherLineId),
            MatchedAmount = matchedAmount,
            MatchType = matchType,
            MatchMethod = matchMethod
        };
    }

    public void Void(Guid voidedBy, DateTimeOffset voidedAtUtc, string reason)
    {
        if (IsVoided) return;
        if (voidedBy == Guid.Empty) throw new DomainException("Voided by user is required.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Void reason is required.");

        IsVoided = true;
        VoidedBy = voidedBy;
        VoidedAtUtc = voidedAtUtc;
        VoidReason = reason.Trim();
    }

    private static void ValidateSource(
        BankMatchType matchType,
        Guid? journalEntryLineId,
        Guid? receiptVoucherLineId,
        Guid? paymentVoucherLineId)
    {
        var journal = Normalize(journalEntryLineId);
        var receipt = Normalize(receiptVoucherLineId);
        var payment = Normalize(paymentVoucherLineId);
        var sourceCount = (journal.HasValue ? 1 : 0) + (receipt.HasValue ? 1 : 0) + (payment.HasValue ? 1 : 0);

        if (sourceCount != 1)
            throw new DomainException("Exactly one reconciliation match source is required.");

        var valid = matchType switch
        {
            BankMatchType.JournalEntry => journal.HasValue,
            BankMatchType.ReceiptVoucher => receipt.HasValue,
            BankMatchType.PaymentVoucher => payment.HasValue,
            _ => false
        };

        if (!valid) throw new DomainException("Match source does not match MatchType.");
    }

    private static Guid? Normalize(Guid? value) => value is { } id && id != Guid.Empty ? id : null;
}
