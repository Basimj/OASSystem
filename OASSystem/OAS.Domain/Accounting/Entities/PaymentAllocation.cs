using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PaymentAllocation : AuditableEntity<Guid>
{
    private PaymentAllocation()
    {
    }

    private PaymentAllocation(
        Guid id,
        PaymentSourceType paymentSourceType,
        Guid paymentSourceId,
        AllocationTargetDocumentType targetDocumentType,
        Guid targetDocumentId,
        decimal allocatedAmount,
        DateTime allocatedAtUtc)
    {
        Id = id;
        PaymentSourceType = paymentSourceType;
        PaymentSourceId = paymentSourceId;
        TargetDocumentType = targetDocumentType;
        TargetDocumentId = targetDocumentId;
        AllocatedAmount = allocatedAmount;
        AllocatedAtUtc = allocatedAtUtc;
    }

    public PaymentSourceType PaymentSourceType { get; private set; }

    public Guid PaymentSourceId { get; private set; }

    public AllocationTargetDocumentType TargetDocumentType { get; private set; }

    public Guid TargetDocumentId { get; private set; }

    public decimal AllocatedAmount { get; private set; }

    public DateTime AllocatedAtUtc { get; private set; }



    public void UpdateAllocatedAmount(decimal allocatedAmount)
    {
        if (allocatedAmount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(allocatedAmount),
                "Allocated amount must be greater than zero.");

        AllocatedAmount = allocatedAmount;
    }

    public static PaymentAllocation Create(
        Guid id,
        PaymentSourceType paymentSourceType,
        Guid paymentSourceId,
        AllocationTargetDocumentType targetDocumentType,
        Guid targetDocumentId,
        decimal allocatedAmount,
        DateTime allocatedAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (paymentSourceId == Guid.Empty)
            throw new ArgumentException(
                "Payment source id is required.",
                nameof(paymentSourceId));

        if (targetDocumentId == Guid.Empty)
            throw new ArgumentException(
                "Target document id is required.",
                nameof(targetDocumentId));

        if (allocatedAmount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(allocatedAmount),
                "Allocated amount must be greater than zero.");

        return new PaymentAllocation(
            id,
            paymentSourceType,
            paymentSourceId,
            targetDocumentType,
            targetDocumentId,
            allocatedAmount,
            allocatedAtUtc);
    }
}
