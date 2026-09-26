using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.PaymentAllocations;
public sealed record CreatePaymentAllocationRequest(
    Guid? ReceiptVoucherLineId,
    Guid? PaymentVoucherLineId,
    AllocationTargetDocumentType TargetDocumentType,
    Guid TargetDocumentId,
    decimal AllocatedAmount);
