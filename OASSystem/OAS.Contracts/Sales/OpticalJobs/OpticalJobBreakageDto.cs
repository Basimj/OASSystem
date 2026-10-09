using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalJobBreakageDto(
    Guid Id,
    Guid OpticalJobId,
    Guid OpticalJobLineId,
    Guid ProductVariantId,
    EyeSide? Eye,
    decimal Quantity,
    string ReasonCode,
    string? ReasonText,
    Guid? TechnicianId,
    OpticalBreakageStatus Status,
    bool RequiresReplacement,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    string RowVersion);
