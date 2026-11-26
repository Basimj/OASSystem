namespace OAS.Contracts.Accounting.CashShifts;

public sealed record CashShiftSettlementAccountDto(Guid AccountId, string Name);
public sealed record CashShiftSettlementRequest(Guid AccountId, DateOnly Date, string Reason, string RowVersion);
