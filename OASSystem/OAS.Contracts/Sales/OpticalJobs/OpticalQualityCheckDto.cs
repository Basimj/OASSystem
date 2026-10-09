using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalQualityCheckDto(
    Guid Id,
    Guid OpticalJobId,
    int AttemptNumber,
    OpticalQualityCheckResult Result,
    OpticalQcFailureAction? FailureAction,
    string? GeneralNotes,
    Guid? CheckedBy,
    DateTimeOffset? CheckedAtUtc,
    string RowVersion,
    IReadOnlyList<OpticalQualityCheckItemDto> Items);

public sealed record OpticalQualityCheckItemDto(
    Guid Id,
    string CheckCode,
    string CheckName,
    OpticalQualityCheckItemResult Result,
    string? Notes,
    int Sequence,
    string RowVersion);
