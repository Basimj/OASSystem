namespace OAS.Contracts.Accounting.CashShifts;

public sealed record CreateCashShiftRequest(
    Guid CashAccountId,
    decimal OpeningBalance,
    string? ShiftNumber = null,
    string? OpeningDifferenceReason = null,
    decimal? ExpectedBookOpeningBalance = null);