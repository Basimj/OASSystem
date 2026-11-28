using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.CashShifts;

public sealed record CashShiftDto(
    Guid Id,
    string ShiftNumber,
    Guid CashAccountId,
    Guid OpenedBy,
    DateTimeOffset OpenedAtUtc,
    decimal OpeningBalance,
    decimal? ExpectedClosingBalance,
    decimal? ActualClosingBalance,
    decimal? DifferenceAmount,
    Guid? ClosedBy,
    DateTimeOffset? ClosedAtUtc,
    CashShiftStatus Status,
    string RowVersion,
    decimal? BookOpeningBalance = null,
    string? OpeningDifferenceReason = null,
    Guid? SettlementJournalEntryId = null,
    Guid? SettlementAccountId = null,
    DateOnly? SettlementDate = null,
    string? SettlementReason = null,
    bool UsesLinkedMovements = false, Guid? CashBoxShiftId = null);