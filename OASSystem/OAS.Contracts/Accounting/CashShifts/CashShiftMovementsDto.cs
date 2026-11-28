namespace OAS.Contracts.Accounting.CashShifts;

public sealed record CashShiftMovementDto(Guid JournalId, string JournalNumber, int LineNumber,
    DateOnly PostingDate, string? Description, decimal DebitBase, decimal CreditBase,
    string? CurrencyCode, decimal? DebitOriginal, decimal? CreditOriginal);

public sealed record CashShiftMovementsDto(bool UsesLinkedMovements, int TotalCount,
    IReadOnlyList<CashShiftMovementDto> Rows, CashShiftReconciliationDto? Reconciliation = null);

// Amounts in the cash account currency, not the company base currency.
public sealed record CashShiftReconciliationDto(Guid CurrencyId, string CurrencyCode,
    string CurrencyName, byte DecimalPlaces, decimal Received, decimal Paid, decimal Expected);
