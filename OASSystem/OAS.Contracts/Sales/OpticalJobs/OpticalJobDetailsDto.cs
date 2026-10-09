using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalJobDetailsDto(
    Guid Id,
    string JobCode,
    Guid CustomerOrderId,
    Guid? SalesInvoiceId,
    Guid CustomerId,
    DateOnly? RequiredDate,
    OpticalJobStatus Status,
    Guid? AssignedTechnicianId,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Notes,
    bool IsActive,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy,
    IReadOnlyList<OpticalJobLineDto> Lines,
    string? CustomerOrderCode = null,
    string? CustomerCode = null,
    string? CustomerName = null,
    string? Mobile = null,
    string? AssignedTechnicianName = null,
    DateTimeOffset? AssignedAtUtc = null,
    DateTimeOffset? DeliveredAtUtc = null,
    IReadOnlyList<OpticalJobTimelineItemDto>? Timeline = null,
    IReadOnlyList<OpticalJobBreakageDto>? OpenBreakages = null,
    IReadOnlyList<OpticalJobRemakeDto>? OpenRemakes = null,
    OpticalQualityCheckDto? LatestQualityCheck = null);
