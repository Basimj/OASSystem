using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PaymentAllocation : Entity<Guid>
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
        DateTime allocatedAtUtc,
        Guid createdBy)
    {
        Id = id;
        PaymentSourceType = paymentSourceType;
        PaymentSourceId = paymentSourceId;
        TargetDocumentType = targetDocumentType;
        TargetDocumentId = targetDocumentId;
        AllocatedAmount = allocatedAmount;
        AllocatedAtUtc = allocatedAtUtc;
        CreatedBy = createdBy;
    }

    public PaymentSourceType PaymentSourceType { get; private set; }

    public Guid PaymentSourceId { get; private set; }

    public AllocationTargetDocumentType TargetDocumentType { get; private set; }

    public Guid TargetDocumentId { get; private set; }

    public decimal AllocatedAmount { get; private set; }

    public DateTime AllocatedAtUtc { get; private set; }

    public Guid CreatedBy { get; private set; }

    public static PaymentAllocation Create(
        Guid id,
        PaymentSourceType paymentSourceType,
        Guid paymentSourceId,
        AllocationTargetDocumentType targetDocumentType,
        Guid targetDocumentId,
        decimal allocatedAmount,
        DateTime allocatedAtUtc,
        Guid createdBy)
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

        if (createdBy == Guid.Empty)
            throw new ArgumentException("Created by is required.", nameof(createdBy));

        return new PaymentAllocation(
            id,
            paymentSourceType,
            paymentSourceId,
            targetDocumentType,
            targetDocumentId,
            allocatedAmount,
            allocatedAtUtc,
            createdBy);
    }
}