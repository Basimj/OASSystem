namespace OAS.Contracts.Accounting.CashAccounts;
public sealed record UpdateCashBoxCurrencyRequest(Guid? Id, Guid CurrencyId, CashAccountMovementType MovementType, bool IsActive, bool IsDefault, string? RowVersion);
public sealed record UpdateCashBoxRequest(string Name, bool RequiresOpenShift, IReadOnlyList<UpdateCashBoxCurrencyRequest> Currencies);
