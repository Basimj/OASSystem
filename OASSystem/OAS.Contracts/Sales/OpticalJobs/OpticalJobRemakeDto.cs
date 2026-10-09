using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalJobRemakeDto(
    Guid Id,
    Guid OpticalJobId,
    Guid? SourceQualityCheckId,
    Guid? SourceBreakageId,
    Guid OpticalJobLineId,
    Guid? ProductVariantId,
    decimal Quantity,
    OpticalRemakeStatus Status,
    string Reason,
    Guid? ReplacementPurchaseRequestLineId,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RowVersion);
