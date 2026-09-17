using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class FiscalYear : Entity<Guid>
{
    private FiscalYear()
    {
    }

    private FiscalYear(
        Guid id,
        string code,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        FiscalYearStatus status)
    {
        Id = id;
        Code = code;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public FiscalYearStatus Status { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public Guid? ClosedBy { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static FiscalYear Create(
        Guid id,
        string code,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        FiscalYearStatus status)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Fiscal year id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Fiscal year code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Fiscal year name is required.", nameof(name));

        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date.");

        return new FiscalYear(
            id,
            code.Trim(),
            name.Trim(),
            startDate,
            endDate,
            status);
    }

    public void Open()
    {
        if (Status == FiscalYearStatus.Closed)
            throw new InvalidOperationException("A closed fiscal year cannot be reopened.");

        Status = FiscalYearStatus.Open;
    }

    public void StartClosing()
    {
        if (Status != FiscalYearStatus.Open)
            throw new InvalidOperationException("Only an open fiscal year can enter closing state.");

        Status = FiscalYearStatus.Closing;
    }

    public void Close(Guid closedBy, DateTime closedAtUtc)
    {
        if (closedBy == Guid.Empty)
            throw new ArgumentException("Closed by is required.", nameof(closedBy));

        if (Status != FiscalYearStatus.Closing)
            throw new InvalidOperationException("Fiscal year must be in closing state.");

        Status = FiscalYearStatus.Closed;
        ClosedBy = closedBy;
        ClosedAtUtc = closedAtUtc;
    }
    public void UpdateDetails(
    string code,
    string name,
    DateOnly startDate,
    DateOnly endDate)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(
                "Fiscal year code is required.",
                nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Fiscal year name is required.",
                nameof(name));

        if (endDate < startDate)
            throw new ArgumentException(
                "End date cannot be before start date.");

        if (Status != FiscalYearStatus.Future)
            throw new InvalidOperationException(
                "Only a future fiscal year can be edited.");

        Code = code.Trim();
        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
    }
}