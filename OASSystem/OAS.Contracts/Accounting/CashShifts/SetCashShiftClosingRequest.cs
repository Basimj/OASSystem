namespace OAS.Contracts.Accounting.CashShifts;

public sealed record SetCashShiftClosingRequest(
    decimal ActualClosingBalance,
    string RowVersion);