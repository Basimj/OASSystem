using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.FiscalYears;

public sealed record FiscalYearDto(
    Guid Id,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    FiscalYearStatus Status,
    DateTimeOffset? ClosedAtUtc,
    Guid? ClosedBy,
    string RowVersion);