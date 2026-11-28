namespace OAS.Contracts.Accounting.CashAccounts;

public sealed record CashAccountDto(
    Guid Id,
    string Code,
    string Name,
    Guid AccountId,
    Guid? CurrencyId,
    bool IsDefault,
    bool IsActive,
    string RowVersion,
    bool RequiresOpenShift = false,
    CashAccountMovementType MovementType = CashAccountMovementType.ReceiptAndPayment,
    Guid? CashBoxId = null,
    string? CashBoxCode = null,
    string? CashBoxName = null);
