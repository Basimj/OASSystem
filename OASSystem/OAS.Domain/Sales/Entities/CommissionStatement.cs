using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Commissions;

namespace OAS.Domain.Sales.Entities;

public sealed class CommissionStatement : AuditableEntity<Guid>
{
    private readonly List<CommissionEntry> _entries = [];
    private CommissionStatement() { }

    private CommissionStatement(Guid id, string statementCode, Guid employeeId, DateOnly fromDate, DateOnly toDate)
    {
        if (id == Guid.Empty) throw new DomainException("Commission statement id is required.");
        if (employeeId == Guid.Empty) throw new DomainException("Employee id is required.");
        if (toDate < fromDate) throw new DomainException("Commission statement end date cannot precede start date.");
        Id = id;
        StatementCode = Required(statementCode, 40, "Commission statement code");
        EmployeeId = employeeId;
        FromDate = fromDate;
        ToDate = toDate;
        Status = CommissionStatementStatus.Draft;
        IsActive = true;
    }

    public string StatementCode { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public DateOnly FromDate { get; private set; }
    public DateOnly ToDate { get; private set; }
    public CommissionStatementStatus Status { get; private set; }
    public decimal SalesBaseAmount { get; private set; }
    public decimal ReturnsBaseAmount { get; private set; }
    public decimal CommissionBaseAmount { get; private set; }
    public DateTimeOffset? CalculatedAtUtc { get; private set; }
    public string? CalculatedBy { get; private set; }
    public DateTimeOffset? FinalizedAtUtc { get; private set; }
    public string? FinalizedBy { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<CommissionEntry> Entries => _entries.AsReadOnly();

    public static CommissionStatement Create(Guid id, string code, Guid employeeId, DateOnly fromDate, DateOnly toDate) =>
        new(id, code, employeeId, fromDate, toDate);

    public void AddEntry(CommissionEntry entry)
    {
        if (Status != CommissionStatementStatus.Draft) throw new DomainException("Only draft commission statements can be calculated.");
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.CommissionStatementId != Id) throw new DomainException("Commission entry does not belong to this statement.");
        if (_entries.Any(x => x.SourceDocumentType == entry.SourceDocumentType && x.SourceLineId == entry.SourceLineId))
            throw new DomainException("Commission source line already exists in this statement.");
        _entries.Add(entry);
        Recalculate();
    }

    public void MarkCalculated(DateTimeOffset atUtc, string? user)
    {
        if (Status != CommissionStatementStatus.Draft) throw new DomainException("Only draft commission statements can be calculated.");
        Recalculate();
        Status = CommissionStatementStatus.Calculated;
        CalculatedAtUtc = atUtc;
        CalculatedBy = NormalizeUser(user);
    }

    public void Finalize(DateTimeOffset atUtc, string? user)
    {
        if (Status != CommissionStatementStatus.Calculated) throw new DomainException("Only calculated commission statements can be finalized.");
        Status = CommissionStatementStatus.Finalized;
        FinalizedAtUtc = atUtc;
        FinalizedBy = NormalizeUser(user);
    }

    public void Cancel()
    {
        if (Status == CommissionStatementStatus.Finalized) throw new DomainException("Finalized commission statements cannot be cancelled.");
        Status = CommissionStatementStatus.Cancelled;
        IsActive = false;
    }

    private void Recalculate()
    {
        SalesBaseAmount = decimal.Round(_entries.Where(x => !x.IsReversal).Sum(x => x.BaseSalesAmount), 4);
        ReturnsBaseAmount = decimal.Round(_entries.Where(x => x.IsReversal).Sum(x => decimal.Abs(x.BaseSalesAmount)), 4);
        CommissionBaseAmount = decimal.Round(_entries.Sum(x => x.CommissionBaseAmount), 4);
    }

    private static string Required(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{name} is required.");
        value = value.Trim();
        if (value.Length > max) throw new DomainException($"{name} cannot exceed {max} characters.");
        return value;
    }
    private static string? NormalizeUser(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
