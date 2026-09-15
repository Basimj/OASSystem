using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.FiscalPeriods;

public sealed record FiscalPeriodDto(
    Guid Id,
    Guid FiscalYearId,
    byte PeriodNumber,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    FiscalPeriodStatus Status,
    bool SalesLocked,
    bool InventoryLocked,
    bool AccountingLocked,
    DateTimeOffset? ClosedAtUtc,
    Guid? ClosedBy,
    string RowVersion);