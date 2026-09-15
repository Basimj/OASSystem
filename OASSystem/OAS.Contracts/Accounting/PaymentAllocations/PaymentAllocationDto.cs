using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.PaymentAllocations;

public sealed record PaymentAllocationDto(
    Guid Id,
    PaymentSourceType PaymentSourceType,
    Guid PaymentSourceId,
    AllocationTargetDocumentType TargetDocumentType,
    Guid TargetDocumentId,
    decimal AllocatedAmount,
    DateTimeOffset AllocatedAtUtc,
    Guid CreatedBy);