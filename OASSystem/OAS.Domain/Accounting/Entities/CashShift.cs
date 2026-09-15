using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class CashShift : Entity<Guid>
{
    private CashShift()
    {
    }

    private CashShift(
        Guid id,
        string shiftNumber,
        Guid cashAccountId,
        Guid openedBy,
        DateTime openedAtUtc,
        decimal openingBalance,
        CashShiftStatus status)
    {
        Id = id;
        ShiftNumber = shiftNumber;
        CashAccountId = cashAccountId;
        OpenedBy = openedBy;
        OpenedAtUtc = openedAtUtc;
        OpeningBalance = openingBalance;
        Status = status;
    }

    public string ShiftNumber { get; private set; } = null!;

    public Guid CashAccountId { get; private set; }

    public Guid OpenedBy { get; private set; }

    public DateTime OpenedAtUtc { get; private set; }

    public decimal OpeningBalance { get; private set; }

    public decimal? ExpectedClosingBalance { get; private set; }

    public decimal? ActualClosingBalance { get; private set; }

    public decimal? DifferenceAmount { get; private set; }

    public Guid? ClosedBy { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public CashShiftStatus Status { get; private set; }

    public static CashShift Create(
        Guid id,
        string shiftNumber,
        Guid cashAccountId,
        Guid openedBy,
        DateTime openedAtUtc,
        decimal openingBalance,
        CashShiftStatus status)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(shiftNumber))
            throw new ArgumentException(
                "Shift number is required.",
                nameof(shiftNumber));

        if (cashAccountId == Guid.Empty)
            throw new ArgumentException(
                "Cash account id is required.",
                nameof(cashAccountId));

        if (openedBy == Guid.Empty)
            throw new ArgumentException(
                "Opened by is required.",
                nameof(openedBy));

        if (openingBalance < 0)
            throw new ArgumentOutOfRangeException(nameof(openingBalance));

        return new CashShift(
            id,
            shiftNumber.Trim(),
            cashAccountId,
            openedBy,
            openedAtUtc,
            openingBalance,
            status);
    }

    public void StartClosing(decimal expectedClosingBalance)
    {
        if (Status != CashShiftStatus.Open)
            throw new InvalidOperationException(
                "Only an open cash shift can enter closing state.");

        if (expectedClosingBalance < 0)
            throw new ArgumentOutOfRangeException(
                nameof(expectedClosingBalance));

        ExpectedClosingBalance = expectedClosingBalance;
        Status = CashShiftStatus.Closing;
    }

    public void Close(
        decimal actualClosingBalance,
        Guid closedBy,
        DateTime closedAtUtc)
    {
        if (Status != CashShiftStatus.Closing)
            throw new InvalidOperationException(
                "Only a cash shift in closing state can be closed.");

        if (actualClosingBalance < 0)
            throw new ArgumentOutOfRangeException(
                nameof(actualClosingBalance));

        if (closedBy == Guid.Empty)
            throw new ArgumentException(
                "Closed by is required.",
                nameof(closedBy));

        ActualClosingBalance = actualClosingBalance;
        DifferenceAmount = ExpectedClosingBalance.HasValue
            ? actualClosingBalance - ExpectedClosingBalance.Value
            : null;

        ClosedBy = closedBy;
        ClosedAtUtc = closedAtUtc;
        Status = CashShiftStatus.Closed;
    }

    public void Approve()
    {
        if (Status != CashShiftStatus.Closed)
            throw new InvalidOperationException(
                "Only closed cash shifts can be approved.");

        Status = CashShiftStatus.Approved;
    }
}