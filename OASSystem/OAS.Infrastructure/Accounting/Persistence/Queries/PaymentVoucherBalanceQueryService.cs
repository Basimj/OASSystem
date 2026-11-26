using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed class PaymentVoucherBalanceQueryService(OasDbContext db) : IPaymentVoucherBalanceQueryService
{
    public async Task<PaymentVoucherBalanceDto> GetAsync(Guid definitionId, bool isBank, DateOnly date, CancellationToken ct)
    {
        Guid? ledgerId, currencyId;
        bool active;
        string? postingError = null;
        if (isBank)
        {
            var bank = await db.Set<BankAccount>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == definitionId, ct);
            ledgerId = bank?.AccountId; currencyId = bank?.CurrencyId; active = bank?.IsActive == true;
        }
        else
        {
            var cash = await db.Set<CashAccount>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == definitionId, ct);
            ledgerId = cash?.AccountId; currencyId = cash?.CurrencyId; active = cash?.IsActive == true;
            if (cash is not null)
            {
                var shifts = await db.Set<CashShift>().AsNoTracking().Where(x => x.CashAccountId == cash.Id &&
                    (x.Status == CashShiftStatus.Open || x.Status == CashShiftStatus.Closing)).Select(x => x.Status).ToListAsync(ct);
                if (shifts.Count > 1 || shifts.Contains(CashShiftStatus.Closing))
                    postingError = "الصندوق قيد الإغلاق أو لديه ورديات نشطة متعارضة.";
                else if (cash.RequiresOpenShift && shifts.Count == 0)
                    postingError = "يجب فتح وردية لهذا الصندوق قبل الترحيل.";
            }
        }
        PaymentVoucherBalanceDto Unavailable(string message) => new(definitionId, ledgerId, currencyId, null, null, null, message);
        if (!active || ledgerId is not Guid accountId || currencyId is not Guid currency)
            return Unavailable("الحساب النقدي غير نشط أو غير محدد العملة.");
        var account = await db.Set<Account>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, ct);
        if (account is null || !account.CanReceivePosting())
            return Unavailable("الحساب المحاسبي المرتبط لا يقبل الترحيل.");
        var definitions = await db.Set<CashAccount>().CountAsync(x => x.AccountId == accountId, ct)
            + await db.Set<BankAccount>().CountAsync(x => x.AccountId == accountId, ct);
        if (definitions != 1) return Unavailable("الحساب المحاسبي مشترك بين أكثر من صندوق أو بنك.");
        var settings = await db.Set<AccountingSettings>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == AccountingSettings.SingletonId, ct);
        var curr = await db.Set<Currency>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == currency, ct);
        if (settings is null || curr is null || !curr.IsActive || curr.DecimalPlaces > 4)
            return Unavailable("إعدادات العملة غير مكتملة أو غير صالحة.");
        var balance = await CashTransferService.ReadBalanceAsync(db, accountId, currency, settings.BaseCurrencyId, date, ct);
        return new(definitionId, accountId, currency, curr.Code, curr.DecimalPlaces,
            balance.ErrorCode is null ? balance.Native : null, balance.ErrorMessage, postingError);
    }

    // Run while the existing cash-posting transaction lock is held. Recheck every debit of cash caused by a payment voucher.
    internal static async Task ValidatePostingsAsync(OasDbContext db, IReadOnlyCollection<JournalEntry> journals, CancellationToken ct)
    {
        var outgoing = journals.Where(j => j.SourceDocumentType == "PaymentVoucher")
            .SelectMany(j => j.Lines.Where(l => l.CreditAmount > 0 || l.TransactionCreditAmount > 0)
                .Select(l => (Journal: j, Line: l))).ToArray();
        if (outgoing.Length == 0) return;
        var ids = outgoing.Select(x => x.Line.AccountId).Distinct().ToArray();
        var cash = await db.Set<CashAccount>().AsNoTracking().Where(x => ids.Contains(x.AccountId)).ToListAsync(ct);
        var banks = await db.Set<BankAccount>().AsNoTracking().Where(x => ids.Contains(x.AccountId)).ToListAsync(ct);
        var definitions = cash.Select(x => (x.AccountId, x.CurrencyId, x.IsActive, x.Id, IsBank: false))
            .Concat(banks.Select(x => (x.AccountId, x.CurrencyId, x.IsActive, x.Id, IsBank: true))).ToLookup(x => x.AccountId);
        var service = new PaymentVoucherBalanceQueryService(db);
        foreach (var group in outgoing.GroupBy(x => x.Line.AccountId))
        {
            var matched = definitions[group.Key].ToArray();
            if (matched.Length == 0) continue; // Non-cash settlement account; not a withdrawal.
            if (matched.Length != 1 || !matched[0].IsActive || matched[0].CurrencyId is not Guid currency)
                throw new ConflictException("payment_voucher_account_invalid", "حساب الصرف النقدي غير صالح أو مشترك.");
            // A pending batch may include more than one voucher on the same account.
            // Do not fund a payment with an unrelated, not-yet-posted credit in the same batch.
            var date = group.Min(x => x.Journal.PostingDate);
            var balance = await service.GetAsync(matched[0].Id, matched[0].IsBank, date, ct);
            if (balance.AvailableAmount is not decimal available)
                throw new ConflictException("payment_voucher_balance_unavailable", balance.Error ?? "تعذر التحقق من رصيد الصرف.");
            var required = group.Sum(x => -CashShiftBalanceService.ResolveMovement(currency,
                x.Journal.BaseCurrencyId, x.Line.TransactionCurrencyId, x.Line.TransactionDebitAmount,
                x.Line.TransactionCreditAmount, x.Line.DebitAmount, x.Line.CreditAmount, x.Journal.JournalNumber));
            if (required > available)
                throw new ConflictException("payment_voucher_insufficient_balance",
                    $"الرصيد الدفتري المتاح لا يكفي للصرف. المتاح: {available:0.####} {balance.CurrencyCode}، المطلوب: {required:0.####} {balance.CurrencyCode}.");
        }
    }
}
