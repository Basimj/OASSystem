using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Abstractions;

/// <summary>
/// Describes the typed source line used by a payment allocation.  Target-specific
/// validators can use this context to enforce party/source compatibility without
/// introducing Purchasing/Sales dependencies into the generic accounting handler.
/// </summary>
public sealed record PaymentAllocationSourceContext(
    PaymentSourceType SourceType,
    SettlementPartyType? PartyType,
    Guid? CustomerId,
    Guid? SupplierId,
    Guid? EmployeeId);

public interface IPaymentAllocationSourceTargetValidator
{
    AllocationTargetDocumentType TargetDocumentType { get; }

    Task ValidateSourceAsync(
        Guid targetDocumentId,
        PaymentAllocationSourceContext source,
        CancellationToken cancellationToken = default);
}
