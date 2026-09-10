namespace OAS.Contracts.Features.Employees.JobTitles;

public sealed record JobTitleDto(
    Guid Id,
    string Name,
    bool IsActive,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastModifiedAtUtc);
