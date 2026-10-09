using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalJobTimelineItemDto(
    OpticalJobStatus? FromStatus,
    OpticalJobStatus ToStatus,
    string? Reason,
    Guid ChangedBy,
    DateTimeOffset ChangedAtUtc,
    string? CorrelationId);
