using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.PaymentAllocations;

public sealed record CreatePaymentAllocationRequest(
    PaymentSourceType PaymentSourceType,
    Guid PaymentSourceId,
    AllocationTargetDocumentType TargetDocumentType,
    Guid TargetDocumentId,
    decimal AllocatedAmount);