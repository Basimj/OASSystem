using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class FiscalPeriod : AuditableEntity<Guid>
{
    private FiscalPeriod()
    {
    }

    private FiscalPeriod(
        Guid id,
        Guid fiscalYearId,
        byte periodNumber,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        FiscalPeriodStatus status,
        bool salesLocked,
        bool inventoryLocked,
        bool accountingLocked)
    {
        Id = id;
        FiscalYearId = fiscalYearId;
        PeriodNumber = periodNumber;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        SalesLocked = salesLocked;
        InventoryLocked = inventoryLocked;
        AccountingLocked = accountingLocked;
    }

    public Guid FiscalYearId { get; private set; }

    public byte PeriodNumber { get; private set; }

    public string Name { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public FiscalPeriodStatus Status { get; private set; }

    public bool SalesLocked { get; private set; }

    public bool InventoryLocked { get; private set; }

    public bool AccountingLocked { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public Guid? ClosedBy { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static FiscalPeriod Create(
        Guid id,
        Guid fiscalYearId,
        byte periodNumber,
        string name,
        DateOnly startDate,
        DateOnly endDate,
        FiscalPeriodStatus status,
        bool salesLocked,
        bool inventoryLocked,
        bool accountingLocked)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Fiscal period id is required.", nameof(id));

        if (fiscalYearId == Guid.Empty)
            throw new ArgumentException("Fiscal year id is required.", nameof(fiscalYearId));

        if (periodNumber == 0)
            throw new ArgumentOutOfRangeException(nameof(periodNumber));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Period name is required.", nameof(name));

        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date.");

        return new FiscalPeriod(
            id,
            fiscalYearId,
            periodNumber,
            name.Trim(),
            startDate,
            endDate,
            status,
            salesLocked,
            inventoryLocked,
            accountingLocked);
    }

    public void SetLocks(
        bool salesLocked,
        bool inventoryLocked,
        bool accountingLocked)
    {
        SalesLocked = salesLocked;
        InventoryLocked = inventoryLocked;
        AccountingLocked = accountingLocked;
    }

    public void SoftClose()
    {
        if (Status != FiscalPeriodStatus.Open)
            throw new InvalidOperationException("Only an open period can be soft closed.");

        Status = FiscalPeriodStatus.SoftClosed;
    }

    public void Close(Guid closedBy, DateTime closedAtUtc)
    {
        if (closedBy == Guid.Empty)
            throw new ArgumentException("Closed by is required.", nameof(closedBy));

        if (Status == FiscalPeriodStatus.Closed)
            throw new InvalidOperationException("Fiscal period is already closed.");

        Status = FiscalPeriodStatus.Closed;
        AccountingLocked = true;
        ClosedBy = closedBy;
        ClosedAtUtc = closedAtUtc;
    }

    public bool CanPostAccounting()
    {
        return Status != FiscalPeriodStatus.Closed
            && !AccountingLocked;
    }
    public void UpdateDetails(
    string name,
    DateOnly startDate,
    DateOnly endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Period name is required.",
                nameof(name));

        if (endDate < startDate)
            throw new ArgumentException(
                "End date cannot be before start date.");

        if (Status != FiscalPeriodStatus.Open)
            throw new InvalidOperationException(
                "Only an open fiscal period can be edited.");

        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
    }
}
