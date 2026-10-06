using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalJobWorkQueueDto(
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
    string RowVersion,
    string? CustomerOrderCode = null,
    string? CustomerCode = null,
    string? CustomerName = null,
    string? Mobile = null,
    string? FrameSummary = null,
    string? ODSummary = null,
    string? OSSummary = null,
    string? AssignedTechnicianName = null);
