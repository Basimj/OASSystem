namespace OAS.Contracts.Accounting.CashShifts;

public sealed record CashShiftMovementDto(Guid JournalId, string JournalNumber, int LineNumber,
    DateOnly PostingDate, string? Description, decimal DebitBase, decimal CreditBase,
    string? CurrencyCode, decimal? DebitOriginal, decimal? CreditOriginal);

public sealed record CashShiftMovementsDto(bool UsesLinkedMovements, int TotalCount,
    IReadOnlyList<CashShiftMovementDto> Rows);
