using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.MultiCurrency;

public sealed class SettlementAccountResolver(
    IReadRepository<CashAccount, Guid> cashAccounts,
    IReadRepository<BankAccount, Guid> bankAccounts,
    IReadRepository<Account, Guid> accounts) : ISettlementAccountResolver
{
    public async Task<SettlementAccountResolution> ResolveAsync(PaymentMethod paymentMethod, Guid currencyId, Guid? cashAccountId, Guid? bankAccountId, Guid? otherSettlementAccountId, string? referenceNumber, CancellationToken cancellationToken = default)
    {
        return paymentMethod switch
        {
            PaymentMethod.Cash => await FromCashAsync(currencyId, cashAccountId, bankAccountId, otherSettlementAccountId, cancellationToken),
            PaymentMethod.BankTransfer or PaymentMethod.Card or PaymentMethod.Cheque => await FromBankAsync(paymentMethod, currencyId, cashAccountId, bankAccountId, otherSettlementAccountId, referenceNumber, cancellationToken),
            PaymentMethod.Other => await FromOtherAsync(cashAccountId, bankAccountId, otherSettlementAccountId, cancellationToken),
            _ => throw new ConflictException("payment_method_invalid", "Payment method is invalid.")
        };
    }

    private async Task<SettlementAccountResolution> FromCashAsync(Guid currencyId, Guid? cashId, Guid? bankId, Guid? otherId, CancellationToken ct)
    {
        if (!cashId.HasValue || bankId.HasValue || otherId.HasValue) throw new ConflictException("cash_payment_account_invalid", "Cash payment requires one cash account only.");
        var cash = await cashAccounts.GetByIdAsync(cashId.Value, ct) ?? throw new NotFoundException(nameof(CashAccount), cashId.Value);
        if (!cash.IsActive) throw new ConflictException("cash_account_inactive", "Selected cash account is inactive.");
        if (!cash.CurrencyId.HasValue || cash.CurrencyId.Value != currencyId) throw new ConflictException("cash_currency_mismatch", "Cash account currency must match line currency.");
        await EnsurePostingAsync(cash.AccountId, ct);
        return new(cash.AccountId, cash.Id, null);
    }

    private async Task<SettlementAccountResolution> FromBankAsync(PaymentMethod method, Guid currencyId, Guid? cashId, Guid? bankId, Guid? otherId, string? referenceNumber, CancellationToken ct)
    {
        if (cashId.HasValue || !bankId.HasValue || otherId.HasValue) throw new ConflictException("bank_payment_account_invalid", "Bank/Card/Cheque payment requires one bank account only.");
        if (method == PaymentMethod.Cheque && string.IsNullOrWhiteSpace(referenceNumber)) throw new ConflictException("cheque_reference_required", "Cheque reference number is required.");
        var bank = await bankAccounts.GetByIdAsync(bankId.Value, ct) ?? throw new NotFoundException(nameof(BankAccount), bankId.Value);
        if (!bank.IsActive) throw new ConflictException("bank_account_inactive", "Selected bank account is inactive.");
        if (!bank.CurrencyId.HasValue || bank.CurrencyId.Value != currencyId) throw new ConflictException("bank_currency_mismatch", "Bank account currency must match line currency.");
        await EnsurePostingAsync(bank.AccountId, ct);
        return new(bank.AccountId, null, bank.Id);
    }

    private async Task<SettlementAccountResolution> FromOtherAsync(Guid? cashId, Guid? bankId, Guid? otherId, CancellationToken ct)
    {
        if (cashId.HasValue || bankId.HasValue || !otherId.HasValue) throw new ConflictException("other_settlement_account_required", "Other payment method requires a settlement account and no cash/bank account.");
        var account = await accounts.GetByIdAsync(otherId.Value, ct) ?? throw new NotFoundException(nameof(Account), otherId.Value);
        if (!account.CanReceiveManualPosting()) throw new ConflictException("other_settlement_account_invalid", "Other settlement account must be active and allow manual posting.");
        return new(account.Id, null, null);
    }

    private async Task EnsurePostingAsync(Guid id, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Account), id);
        if (!account.CanReceivePosting()) throw new ConflictException("settlement_account_invalid", "Settlement account is inactive or cannot receive posting.");
    }
}
