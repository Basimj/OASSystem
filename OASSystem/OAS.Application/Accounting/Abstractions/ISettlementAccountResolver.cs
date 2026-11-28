using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Abstractions;

public sealed record SettlementAccountResolution(
    Guid AccountId,
    Guid? CashAccountId,
    Guid? BankAccountId);

public interface ISettlementAccountResolver
{
    Task<SettlementAccountResolution> ResolveAsync(
        PaymentMethod paymentMethod,
        Guid currencyId,
        Guid? cashAccountId,
        Guid? bankAccountId,
        Guid? otherSettlementAccountId,
        string? referenceNumber,
        CancellationToken cancellationToken = default);

    Task<SettlementAccountResolution> ResolveAsync(
        PaymentMethod paymentMethod,
        Guid currencyId,
        Guid? cashAccountId,
        Guid? bankAccountId,
        Guid? otherSettlementAccountId,
        string? referenceNumber,
        CancellationToken cancellationToken, bool isReceipt) =>
        ResolveAsync(paymentMethod, currencyId, cashAccountId, bankAccountId, otherSettlementAccountId, referenceNumber, cancellationToken);
}
