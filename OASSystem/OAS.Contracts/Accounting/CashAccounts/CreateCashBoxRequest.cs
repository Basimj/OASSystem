namespace OAS.Contracts.Accounting.CashAccounts;
public sealed record CashBoxCurrencyRequest(Guid CurrencyId, CashAccountMovementType MovementType, bool IsDefault = false, bool IsActive = true);
public sealed record CreateCashBoxRequest(string Name, bool RequiresOpenShift, IReadOnlyList<CashBoxCurrencyRequest> Currencies, string? Code = null);
