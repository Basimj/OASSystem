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
    string RowVersion);
