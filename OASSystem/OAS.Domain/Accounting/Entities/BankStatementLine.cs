using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.Entities;

public sealed class BankStatementLine : AuditableEntity<Guid>
{
    private BankStatementLine() { }

    public Guid BankStatementId { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public DateOnly? ValueDate { get; private set; }
    public string? Reference { get; private set; }
    public string? Description { get; private set; }
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }
    public decimal? RunningBalance { get; private set; }
    public string? ExternalTransactionId { get; private set; }
    public BankStatementLineMatchStatus MatchStatus { get; private set; }
    public bool IsIgnored { get; private set; }
    public string? IgnoreReason { get; private set; }
    public DateTimeOffset? IgnoredAtUtc { get; private set; }
    public Guid? IgnoredBy { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public decimal StatementAmount => DebitAmount > 0m ? DebitAmount : CreditAmount;

    public static BankStatementLine Create(
        Guid id,
        Guid bankStatementId,
        DateOnly transactionDate,
        DateOnly? valueDate,
        string? reference,
        string? description,
        decimal debitAmount,
        decimal creditAmount,
        decimal? runningBalance,
        string? externalTransactionId)
    {
        if (id == Guid.Empty) throw new DomainException("Bank statement line id is required.");
        if (bankStatementId == Guid.Empty) throw new DomainException("Bank statement id is required.");
        ValidateAmounts(debitAmount, creditAmount);

        return new BankStatementLine
        {
            Id = id,
            BankStatementId = bankStatementId,
            TransactionDate = transactionDate,
            ValueDate = valueDate,
            Reference = Optional(reference),
            Description = Optional(description),
            DebitAmount = debitAmount,
            CreditAmount = creditAmount,
            RunningBalance = runningBalance,
            ExternalTransactionId = Optional(externalTransactionId),
            MatchStatus = BankStatementLineMatchStatus.Unmatched
        };
    }

    public void Update(
        DateOnly transactionDate,
        DateOnly? valueDate,
        string? reference,
        string? description,
        decimal debitAmount,
        decimal creditAmount,
        decimal? runningBalance,
        string? externalTransactionId)
    {
        if (MatchStatus is BankStatementLineMatchStatus.PartiallyMatched or BankStatementLineMatchStatus.Matched)
            throw new DomainException("A matched bank statement line cannot be edited before its matches are removed.");
        if (IsIgnored) throw new DomainException("Restore the ignored line before editing it.");
        ValidateAmounts(debitAmount, creditAmount);

        TransactionDate = transactionDate;
        ValueDate = valueDate;
        Reference = Optional(reference);
        Description = Optional(description);
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        RunningBalance = runningBalance;
        ExternalTransactionId = Optional(externalTransactionId);
    }

    public void RecalculateMatchStatus(decimal activeMatchedAmount)
    {
        if (activeMatchedAmount < 0m) throw new DomainException("Matched amount cannot be negative.");
        if (activeMatchedAmount > StatementAmount)
            throw new DomainException("Matched amount cannot exceed the bank statement line amount.");

        if (IsIgnored)
        {
            MatchStatus = BankStatementLineMatchStatus.Ignored;
            return;
        }

        MatchStatus = activeMatchedAmount switch
        {
            <= 0m => BankStatementLineMatchStatus.Unmatched,
            _ when activeMatchedAmount < StatementAmount => BankStatementLineMatchStatus.PartiallyMatched,
            _ => BankStatementLineMatchStatus.Matched
        };
    }

    public void Ignore(string reason, Guid ignoredBy, DateTimeOffset ignoredAtUtc)
    {
        if (ignoredBy == Guid.Empty) throw new DomainException("Ignored by user is required.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Ignore reason is required.");
        if (MatchStatus is BankStatementLineMatchStatus.PartiallyMatched or BankStatementLineMatchStatus.Matched)
            throw new DomainException("A matched line cannot be ignored before its matches are removed.");

        IsIgnored = true;
        IgnoreReason = reason.Trim();
        IgnoredBy = ignoredBy;
        IgnoredAtUtc = ignoredAtUtc;
        MatchStatus = BankStatementLineMatchStatus.Ignored;
    }

    public void RestoreIgnored()
    {
        if (!IsIgnored) return;
        IsIgnored = false;
        IgnoreReason = null;
        IgnoredBy = null;
        IgnoredAtUtc = null;
        MatchStatus = BankStatementLineMatchStatus.Unmatched;
    }

    private static void ValidateAmounts(decimal debitAmount, decimal creditAmount)
    {
        if (debitAmount < 0m || creditAmount < 0m)
            throw new DomainException("Debit and credit amounts cannot be negative.");
        if (debitAmount > 0m && creditAmount > 0m)
            throw new DomainException("A bank statement line cannot contain both debit and credit amounts.");
        if (debitAmount == 0m && creditAmount == 0m)
            throw new DomainException("A bank statement line must contain a debit or credit amount.");
    }

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
