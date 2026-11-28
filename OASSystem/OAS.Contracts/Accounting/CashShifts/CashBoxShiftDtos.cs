namespace OAS.Contracts.Accounting.CashShifts;
public sealed record OpenCashBoxShiftRequest(Guid CashBoxId, IReadOnlyList<CreateCashShiftRequest> Currencies);
public sealed record CashBoxShiftCloseLine(Guid ShiftId, decimal? ActualClosingBalance, string RowVersion);
public sealed record CloseCashBoxShiftRequest(IReadOnlyList<CashBoxShiftCloseLine> Currencies);
public sealed record CashBoxShiftVersion(Guid ShiftId, string RowVersion);
public sealed record ApproveCashBoxShiftRequest(IReadOnlyList<CashBoxShiftVersion> Currencies);
