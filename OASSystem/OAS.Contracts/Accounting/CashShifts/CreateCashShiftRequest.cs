namespace OAS.Contracts.Accounting.CashShifts;

public sealed record CreateCashShiftRequest(
    Guid CashAccountId,
    decimal OpeningBalance);