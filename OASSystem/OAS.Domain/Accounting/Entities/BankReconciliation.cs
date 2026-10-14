using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.Entities;

public sealed class BankReconciliation : AuditableEntity<Guid>
{
    private readonly List<BankReconciliationMatch> _matches = [];

    private BankReconciliation() { }

    public Guid BankAccountId { get; private set; }
    public Guid BankStatementId { get; private set; }
    public DateOnly ReconciliationDate { get; private set; }
    public decimal BookClosingBalance { get; private set; }
    public decimal StatementClosingBalance { get; private set; }
    public decimal Difference { get; private set; }
    public BankReconciliationStatus Status { get; private set; }
    public Guid? SubmittedBy { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public Guid? ReopenedBy { get; private set; }
    public DateTimeOffset? ReopenedAtUtc { get; private set; }
    public string? ReopenReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<BankReconciliationMatch> Matches => _matches.AsReadOnly();

    public static BankReconciliation Create(
        Guid id,
        Guid bankAccountId,
        Guid bankStatementId,
        DateOnly reconciliationDate,
        decimal statementClosingBalance)
    {
        if (id == Guid.Empty) throw new DomainException("Bank reconciliation id is required.");
        if (bankAccountId == Guid.Empty) throw new DomainException("Bank account is required.");
        if (bankStatementId == Guid.Empty) throw new DomainException("Bank statement is required.");

        return new BankReconciliation
        {
            Id = id,
            BankAccountId = bankAccountId,
            BankStatementId = bankStatementId,
            ReconciliationDate = reconciliationDate,
            StatementClosingBalance = statementClosingBalance,
            BookClosingBalance = 0m,
            Difference = statementClosingBalance,
            Status = BankReconciliationStatus.Draft
        };
    }

    public void AddMatch(BankReconciliationMatch match)
    {
        ArgumentNullException.ThrowIfNull(match);
        EnsureMatchingAllowed();
        if (match.BankReconciliationId != Id) throw new DomainException("Match does not belong to this reconciliation.");
        if (_matches.Any(x => x.Id == match.Id)) throw new DomainException("Reconciliation match already exists.");
        _matches.Add(match);
    }

    public void UpdateDraftSnapshot(decimal bookClosingBalance, decimal statementClosingBalance)
    {
        if (Status is BankReconciliationStatus.Reconciled or BankReconciliationStatus.Cancelled)
            throw new DomainException("Closed or cancelled reconciliation cannot be updated.");

        BookClosingBalance = bookClosingBalance;
        StatementClosingBalance = statementClosingBalance;
        Difference = statementClosingBalance - bookClosingBalance;
    }

    public void Submit(Guid submittedBy, DateTimeOffset submittedAtUtc)
    {
        if (submittedBy == Guid.Empty) throw new DomainException("Submitted by user is required.");
        if (Status != BankReconciliationStatus.Draft)
            throw new DomainException("Only draft reconciliation can be submitted.");

        Status = BankReconciliationStatus.InReview;
        SubmittedBy = submittedBy;
        SubmittedAtUtc = submittedAtUtc;
    }

    public void Approve(Guid approvedBy, DateTimeOffset approvedAtUtc)
    {
        if (approvedBy == Guid.Empty) throw new DomainException("Approver is required.");
        if (Status != BankReconciliationStatus.InReview)
            throw new DomainException("Only reconciliation in review can be approved.");

        Status = BankReconciliationStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = approvedAtUtc;
    }

    public void Close(
        decimal bookClosingBalance,
        decimal statementClosingBalance,
        bool unresolvedDifferenceAllowed,
        Guid closedBy,
        DateTimeOffset closedAtUtc)
    {
        if (closedBy == Guid.Empty) throw new DomainException("Closed by user is required.");
        if (Status != BankReconciliationStatus.Approved)
            throw new DomainException("Only approved reconciliation can be closed.");

        var difference = statementClosingBalance - bookClosingBalance;
        if (difference != 0m && !unresolvedDifferenceAllowed)
            throw new DomainException("Reconciliation cannot be closed with an unresolved difference.");

        BookClosingBalance = bookClosingBalance;
        StatementClosingBalance = statementClosingBalance;
        Difference = difference;
        Status = BankReconciliationStatus.Reconciled;
        ClosedBy = closedBy;
        ClosedAtUtc = closedAtUtc;
    }

    public void Reopen(Guid reopenedBy, DateTimeOffset reopenedAtUtc, string reason)
    {
        if (reopenedBy == Guid.Empty) throw new DomainException("Reopened by user is required.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Reopen reason is required.");
        if (Status != BankReconciliationStatus.Reconciled)
            throw new DomainException("Only reconciled records can be reopened.");

        Status = BankReconciliationStatus.Draft;
        ReopenedBy = reopenedBy;
        ReopenedAtUtc = reopenedAtUtc;
        ReopenReason = reason.Trim();
        ClosedBy = null;
        ClosedAtUtc = null;
        ApprovedBy = null;
        ApprovedAtUtc = null;
        SubmittedBy = null;
        SubmittedAtUtc = null;
    }

    public void Cancel()
    {
        if (Status == BankReconciliationStatus.Reconciled)
            throw new DomainException("Reconciled record must be reopened before cancellation.");
        if (Status == BankReconciliationStatus.Cancelled) return;
        Status = BankReconciliationStatus.Cancelled;
    }

    private void EnsureMatchingAllowed()
    {
        if (Status is not (BankReconciliationStatus.Draft or BankReconciliationStatus.InReview))
            throw new DomainException("Matching is not allowed in the current reconciliation status.");
    }
}
