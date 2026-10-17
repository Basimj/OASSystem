using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Abstractions;

public sealed record PaymentAllocationTargetValidation(
    decimal TargetBaseAllocatedAmount,
    decimal TargetDocumentAmount);

public interface IPaymentAllocationTargetValidator
{
    AllocationTargetDocumentType TargetDocumentType { get; }

    Task<PaymentAllocationTargetValidation> ValidateAsync(
        Guid targetDocumentId,
        Guid sourceCurrencyId,
        decimal allocatedAmount,
        decimal sourceBaseAllocatedAmount,
        Guid? excludingAllocationId = null,
        decimal? requestedTargetDocumentAmount = null,
        CancellationToken cancellationToken = default);
}
