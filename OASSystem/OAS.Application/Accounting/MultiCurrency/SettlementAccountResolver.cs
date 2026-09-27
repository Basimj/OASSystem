using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.MultiCurrency;

public sealed class SettlementAccountResolver(
    IReadRepository<CashAccount, Guid> cashAccounts,
    IReadRepository<BankAccount, Guid> bankAccounts,
    IReadRepository<Account, Guid> accounts)
    : ISettlementAccountResolver
{
    public async Task<SettlementAccountResolution> ResolveAsync(
        PaymentMethod paymentMethod,
        Guid currencyId,
        Guid? cashAccountId,
        Guid? bankAccountId,
        Guid? otherSettlementAccountId,
        string? referenceNumber,
        CancellationToken cancellationToken = default)
    {
        return paymentMethod switch
        {
            PaymentMethod.Cash =>
                await FromCashAsync(
                    currencyId,
                    cashAccountId,
                    bankAccountId,
                    otherSettlementAccountId,
                    cancellationToken),

            PaymentMethod.BankTransfer
                or PaymentMethod.Card
                or PaymentMethod.Cheque =>
                    await FromBankAsync(
                        paymentMethod,
                        currencyId,
                        cashAccountId,
                        bankAccountId,
                        otherSettlementAccountId,
                        referenceNumber,
                        cancellationToken),

            PaymentMethod.Other =>
                await FromOtherAsync(
                    cashAccountId,
                    bankAccountId,
                    otherSettlementAccountId,
                    cancellationToken),

            _ =>
                throw new ConflictException(
                    "payment_method_invalid",
                    "ÿ—Ìﬁ… «·œ›⁄ €Ì— ’«·Õ….")
        };
    }


    // ============================================================
    // Cash
    // ============================================================

    private async Task<SettlementAccountResolution> FromCashAsync(
        Guid currencyId,
        Guid? cashId,
        Guid? bankId,
        Guid? otherId,
        CancellationToken cancellationToken)
    {
        /*
         * ›Ì «·œ›⁄ «·‰ﬁœÌ ÌÃ» √‰ ÌÊÃœ ’‰œÊﬁ Ê«Õœ ›ﬁÿ.
         *
         * „·«ÕŸ…:
         * »⁄÷ «·”‰œ«  «·ﬁœÌ„… ﬁœ  ⁄Ìœ SettlementAccountId
         * ÊÂÊ Õ”«» GL «·‰« Ã „‰ «·’‰œÊﬁ ‰›”Â.
         *
         * ·–·ﬂ ·« ‰—›÷Â „»«‘—….
         * ‰ Õﬁﬁ √Ê·« Â· Ìÿ«»ﬁ Õ”«» GL «·Œ«’ »«·’‰œÊﬁ.
         */

        if (!cashId.HasValue)
        {
            throw new ConflictException(
                "cash_payment_account_required",
                "ÌÃ»  ÕœÌœ «·’‰œÊﬁ ⁄‰œ «Œ Ì«— ÿ—Ìﬁ… «·œ›⁄ «·‰ﬁœÌ.");
        }


        if (bankId.HasValue)
        {
            throw new ConflictException(
                "cash_payment_bank_not_allowed",
                "·« Ì„ﬂ‰  ÕœÌœ Õ”«» »‰ﬂÌ „⁄ ÿ—Ìﬁ… «·œ›⁄ «·‰ﬁœÌ.");
        }


        var cash =
            await cashAccounts.GetByIdAsync(
                cashId.Value,
                cancellationToken)
            ??
            throw new NotFoundException(
                nameof(CashAccount),
                cashId.Value);


        /*
         * ≈–« √—”· «·‹Client SettlementAccountId ﬁœÌ„«°
         * ‰ﬁ»·Â ›ﬁÿ ≈–« ﬂ«‰ ÂÊ ‰›” Õ”«» GL «·„— »ÿ »«·’‰œÊﬁ.
         */
        if (otherId.HasValue &&
            otherId.Value != cash.AccountId)
        {
            throw new ConflictException(
                "cash_payment_account_invalid",
                "Õ”«» «· ”ÊÌ… «·„—”· ·« Ìÿ«»ﬁ «·Õ”«» «·„Õ«”»Ì «·„— »ÿ »«·’‰œÊﬁ.");
        }


        if (!cash.IsActive)
        {
            throw new ConflictException(
                "cash_account_inactive",
                "«·’‰œÊﬁ «·„Õœœ €Ì— ‰‘ÿ.");
        }


        if (!cash.CurrencyId.HasValue ||
            cash.CurrencyId.Value != currencyId)
        {
            throw new ConflictException(
                "cash_currency_mismatch",
                "⁄„·… «·’‰œÊﬁ ÌÃ» √‰  ÿ«»ﬁ ⁄„·… ”ÿ— «·”‰œ.");
        }


        await EnsurePostingAsync(
            cash.AccountId,
            cancellationToken);


        return new SettlementAccountResolution(
            cash.AccountId,
            cash.Id,
            null);
    }


    // ============================================================
    // Bank / Card / Cheque
    // ============================================================

    private async Task<SettlementAccountResolution> FromBankAsync(
        PaymentMethod method,
        Guid currencyId,
        Guid? cashId,
        Guid? bankId,
        Guid? otherId,
        string? referenceNumber,
        CancellationToken cancellationToken)
    {
        if (cashId.HasValue)
        {
            throw new ConflictException(
                "bank_payment_cash_not_allowed",
                "·« Ì„ﬂ‰  ÕœÌœ ’‰œÊﬁ „⁄ ÿ—Ìﬁ… «·œ›⁄ «·»‰ﬂÌ….");
        }


        if (!bankId.HasValue)
        {
            throw new ConflictException(
                "bank_payment_account_required",
                "ÌÃ»  ÕœÌœ «·Õ”«» «·»‰ﬂÌ ·ÿ—Ìﬁ… «·œ›⁄ «·„Õœœ….");
        }


        if (method == PaymentMethod.Cheque &&
            string.IsNullOrWhiteSpace(referenceNumber))
        {
            throw new ConflictException(
                "cheque_reference_required",
                "—ﬁ„ «·‘Ìﬂ „ÿ·Ê».");
        }


        var bank =
            await bankAccounts.GetByIdAsync(
                bankId.Value,
                cancellationToken)
            ??
            throw new NotFoundException(
                nameof(BankAccount),
                bankId.Value);


        /*
         * „À· «·’‰œÊﬁ:
         * ﬁœ ÌﬂÊ‰ SettlementAccountId ﬁœ ⁄«œ „‰ ”‰œ ﬁœÌ„.
         * ‰ﬁ»·Â ›ﬁÿ ≈–« ﬂ«‰ ‰›” Õ”«» GL «·Œ«’ »«·»‰ﬂ.
         */
        if (otherId.HasValue &&
            otherId.Value != bank.AccountId)
        {
            throw new ConflictException(
                "bank_payment_account_invalid",
                "Õ”«» «· ”ÊÌ… «·„—”· ·« Ìÿ«»ﬁ «·Õ”«» «·„Õ«”»Ì «·„— »ÿ »«·»‰ﬂ.");
        }


        if (!bank.IsActive)
        {
            throw new ConflictException(
                "bank_account_inactive",
                "«·Õ”«» «·»‰ﬂÌ «·„Õœœ €Ì— ‰‘ÿ.");
        }


        if (!bank.CurrencyId.HasValue ||
            bank.CurrencyId.Value != currencyId)
        {
            throw new ConflictException(
                "bank_currency_mismatch",
                "⁄„·… «·Õ”«» «·»‰ﬂÌ ÌÃ» √‰  ÿ«»ﬁ ⁄„·… ”ÿ— «·”‰œ.");
        }


        await EnsurePostingAsync(
            bank.AccountId,
            cancellationToken);


        return new SettlementAccountResolution(
            bank.AccountId,
            null,
            bank.Id);
    }


    // ============================================================
    // Other
    // ============================================================

    private async Task<SettlementAccountResolution> FromOtherAsync(
        Guid? cashId,
        Guid? bankId,
        Guid? otherId,
        CancellationToken cancellationToken)
    {
        if (cashId.HasValue)
        {
            throw new ConflictException(
                "other_payment_cash_not_allowed",
                "·« Ì„ﬂ‰  ÕœÌœ ’‰œÊﬁ ⁄‰œ «” Œœ«„ ÿ—Ìﬁ… «·œ›⁄ ´√Œ—Ïª.");
        }


        if (bankId.HasValue)
        {
            throw new ConflictException(
                "other_payment_bank_not_allowed",
                "·« Ì„ﬂ‰  ÕœÌœ Õ”«» »‰ﬂÌ ⁄‰œ «” Œœ«„ ÿ—Ìﬁ… «·œ›⁄ ´√Œ—Ïª.");
        }


        if (!otherId.HasValue)
        {
            throw new ConflictException(
                "other_settlement_account_required",
                "ÌÃ»  ÕœÌœ Õ”«» «· ”ÊÌ… ⁄‰œ «” Œœ«„ ÿ—Ìﬁ… «·œ›⁄ ´√Œ—Ïª.");
        }


        var account =
            await accounts.GetByIdAsync(
                otherId.Value,
                cancellationToken)
            ??
            throw new NotFoundException(
                nameof(Account),
                otherId.Value);


        if (!account.CanReceiveManualPosting())
        {
            throw new ConflictException(
                "other_settlement_account_invalid",
                "Õ”«» «· ”ÊÌ… «·„Õœœ €Ì— ‰‘ÿ √Ê ·« Ì”„Õ »«· —ÕÌ· «·ÌœÊÌ.");
        }


        return new SettlementAccountResolution(
            account.Id,
            null,
            null);
    }


    // ============================================================
    // Posting validation
    // ============================================================

    private async Task EnsurePostingAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account =
            await accounts.GetByIdAsync(
                accountId,
                cancellationToken)
            ??
            throw new NotFoundException(
                nameof(Account),
                accountId);


        if (!account.CanReceivePosting())
        {
            throw new ConflictException(
                "settlement_account_invalid",
                "«·Õ”«» «·„Õ«”»Ì «·„— »ÿ »«·’‰œÊﬁ √Ê «·»‰ﬂ €Ì— ’«·Õ ·· —ÕÌ·.");
        }
    }
}