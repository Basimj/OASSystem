using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Abstractions;

public sealed record PaymentAllocationTargetValidation(decimal TargetBaseAllocatedAmount);

public interface IPaymentAllocationTargetValidator
{
    AllocationTargetDocumentType TargetDocumentType { get; }

    Task<PaymentAllocationTargetValidation> ValidateAsync(
        Guid targetDocumentId,
        Guid sourceCurrencyId,
        decimal allocatedAmount,
        decimal sourceBaseAllocatedAmount,
        Guid? excludingAllocationId = null,
        CancellationToken cancellationToken = default);
}
