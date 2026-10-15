using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.Entities;

public sealed class BankStatement : AuditableEntity<Guid>
{
    private readonly List<BankStatementLine> _lines = [];

    private BankStatement() { }

    public Guid BankAccountId { get; private set; }
    public string? StatementNumber { get; private set; }
    public DateOnly FromDate { get; private set; }
    public DateOnly ToDate { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public Guid CurrencyId { get; private set; }
    public BankStatementStatus Status { get; private set; }
    public BankStatementSourceType SourceType { get; private set; }
    public string? ImportedFileName { get; private set; }
    public string? ImportedFileHash { get; private set; }
    public DateTimeOffset? ImportedAtUtc { get; private set; }
    public Guid? ImportedBy { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReconciledAtUtc { get; private set; }
    public Guid? ReconciledBy { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<BankStatementLine> Lines => _lines.AsReadOnly();

    public static BankStatement CreateManual(
        Guid id,
        Guid bankAccountId,
        string? statementNumber,
        DateOnly fromDate,
        DateOnly toDate,
        decimal openingBalance,
        decimal closingBalance,
        Guid currencyId)
    {
        ValidateIdentity(id, bankAccountId, currencyId);
        ValidatePeriod(fromDate, toDate);

        return new BankStatement
        {
            Id = id,
            BankAccountId = bankAccountId,
            StatementNumber = Optional(statementNumber),
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            CurrencyId = currencyId,
            Status = BankStatementStatus.Draft,
            SourceType = BankStatementSourceType.Manual
        };
    }

    public static BankStatement CreateImported(
        Guid id,
        Guid bankAccountId,
        string? statementNumber,
        DateOnly fromDate,
        DateOnly toDate,
        decimal openingBalance,
        decimal closingBalance,
        Guid currencyId,
        string importedFileName,
        string importedFileHash,
        Guid importedBy,
        DateTimeOffset importedAtUtc)
    {
        ValidateIdentity(id, bankAccountId, currencyId);
        ValidatePeriod(fromDate, toDate);
        if (string.IsNullOrWhiteSpace(importedFileName)) throw new DomainException("Imported file name is required.");
        if (string.IsNullOrWhiteSpace(importedFileHash)) throw new DomainException("Imported file hash is required.");
        if (importedBy == Guid.Empty) throw new DomainException("Imported by user is required.");

        return new BankStatement
        {
            Id = id,
            BankAccountId = bankAccountId,
            StatementNumber = Optional(statementNumber),
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            CurrencyId = currencyId,
            Status = BankStatementStatus.Imported,
            SourceType = BankStatementSourceType.Imported,
            ImportedFileName = importedFileName.Trim(),
            ImportedFileHash = importedFileHash.Trim(),
            ImportedAtUtc = importedAtUtc,
            ImportedBy = importedBy
        };
    }

    public void UpdateDraft(
        string? statementNumber,
        DateOnly fromDate,
        DateOnly toDate,
        decimal openingBalance,
        decimal closingBalance,
        Guid currencyId)
    {
        EnsureEditable();
        ValidatePeriod(fromDate, toDate);
        if (currencyId == Guid.Empty) throw new DomainException("Currency is required.");

        StatementNumber = Optional(statementNumber);
        FromDate = fromDate;
        ToDate = toDate;
        OpeningBalance = openingBalance;
        ClosingBalance = closingBalance;
        CurrencyId = currencyId;
    }

    public void AddLine(BankStatementLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        EnsureEditable();
        if (line.BankStatementId != Id) throw new DomainException("Statement line does not belong to this bank statement.");
        if (_lines.Any(x => x.Id == line.Id)) throw new DomainException("Bank statement line already exists.");
        _lines.Add(line);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureEditable();
        var line = _lines.FirstOrDefault(x => x.Id == lineId)
            ?? throw new DomainException("Bank statement line was not found.");
        _lines.Remove(line);
    }

    public void Review(Guid reviewedBy, DateTimeOffset reviewedAtUtc)
    {
        if (reviewedBy == Guid.Empty) throw new DomainException("Reviewer is required.");
        if (Status is not (BankStatementStatus.Draft or BankStatementStatus.Imported))
            throw new DomainException("Only draft or imported bank statements can be reviewed.");

        Status = BankStatementStatus.Reviewed;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = reviewedAtUtc;
    }

    public void MarkReconciled(Guid reconciledBy, DateTimeOffset reconciledAtUtc)
    {
        if (reconciledBy == Guid.Empty) throw new DomainException("Reconciled by user is required.");
        if (Status != BankStatementStatus.Reviewed)
            throw new DomainException("Only reviewed bank statements can be reconciled.");

        Status = BankStatementStatus.Reconciled;
        ReconciledBy = reconciledBy;
        ReconciledAtUtc = reconciledAtUtc;
    }

    public void ReopenFromReconciliation()
    {
        if (Status != BankStatementStatus.Reconciled)
            throw new DomainException("Only reconciled bank statements can be reopened.");

        Status = BankStatementStatus.Reviewed;
        ReconciledBy = null;
        ReconciledAtUtc = null;
    }

    public void Cancel()
    {
        if (Status == BankStatementStatus.Reconciled)
            throw new DomainException("A reconciled bank statement cannot be cancelled before reopening its reconciliation.");
        if (Status == BankStatementStatus.Cancelled) return;
        Status = BankStatementStatus.Cancelled;
    }

    private void EnsureEditable()
    {
        if (Status is not (BankStatementStatus.Draft or BankStatementStatus.Imported))
            throw new DomainException("Only draft or imported bank statements can be edited.");
    }

    private static void ValidateIdentity(Guid id, Guid bankAccountId, Guid currencyId)
    {
        if (id == Guid.Empty) throw new DomainException("Bank statement id is required.");
        if (bankAccountId == Guid.Empty) throw new DomainException("Bank account is required.");
        if (currencyId == Guid.Empty) throw new DomainException("Currency is required.");
    }

    private static void ValidatePeriod(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate > toDate) throw new DomainException("Bank statement start date cannot be after end date.");
    }

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
