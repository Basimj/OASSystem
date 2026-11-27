using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed class CashShiftBalanceService(OasDbContext db) : ICashShiftBalanceService, ICashShiftOpeningBalanceService
{
    public async Task<decimal> GetBookBalanceAsync(Guid cashAccountId, CancellationToken cancellationToken = default)
    {
        var cash = await db.Set<CashAccount>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == cashAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(CashAccount), cashAccountId);
        if (cash.CurrencyId is not Guid currencyId)
            throw new ConflictException("cash_shift_currency_missing", "عملة الصندوق غير محددة.");
        if (await db.Set<CashAccount>().AnyAsync(x => x.Id != cash.Id && x.AccountId == cash.AccountId, cancellationToken) ||
            await db.Set<BankAccount>().AnyAsync(x => x.AccountId == cash.AccountId, cancellationToken))
            throw new ConflictException("cash_shift_shared_ledger", "الحساب المحاسبي مشترك.");
        var rows = await (from line in db.Set<JournalEntryLine>().AsNoTracking()
                          join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
                          where line.AccountId == cash.AccountId && journal.Status == JournalEntryStatus.Posted
                          select new { journal.JournalNumber, journal.BaseCurrencyId, line.TransactionCurrencyId,
                              line.TransactionDebitAmount, line.TransactionCreditAmount, line.DebitAmount, line.CreditAmount })
                          .ToListAsync(cancellationToken);
        decimal balance = 0m;
        foreach (var row in rows)
            balance += ResolveMovement(currencyId, row.BaseCurrencyId, row.TransactionCurrencyId,
                row.TransactionDebitAmount, row.TransactionCreditAmount, row.DebitAmount, row.CreditAmount, row.JournalNumber);
        return balance;
    }

    public async Task<decimal> CalculateExpectedAsync(CashShift shift, DateTime throughUtc, CancellationToken cancellationToken = default)
    {
        if (throughUtc < shift.OpenedAtUtc)
            throw new ConflictException("cash_shift_invalid_time", "Shift closing time precedes opening time.");

        var cash = await db.Set<CashAccount>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == shift.CashAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(CashAccount), shift.CashAccountId);
        if (cash.CurrencyId is not Guid currencyId)
            throw new ConflictException("cash_shift_currency_missing", "Cashbox currency must be configured before closing the shift.");

        if (await db.Set<CashAccount>().AnyAsync(x => x.Id != cash.Id && x.AccountId == cash.AccountId, cancellationToken) ||
            await db.Set<BankAccount>().AnyAsync(x => x.AccountId == cash.AccountId, cancellationToken))
            throw new ConflictException("cash_shift_shared_ledger", "The cashbox ledger is shared with another cash/bank account. Its movements cannot safely be isolated.");

        var cashLines = from line in db.Set<JournalEntryLine>().AsNoTracking()
                        join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
                        where line.AccountId == cash.AccountId && journal.Status == JournalEntryStatus.Posted
                        select new { line, journal };

        // Older journals without a posting timestamp cannot safely be assigned by time.
        // Include adjacent posting dates because posting date is a business date, not UTC.
        var earliestDate = DateOnly.FromDateTime(shift.OpenedAtUtc).AddDays(-1);
        var latestDate = DateOnly.FromDateTime(throughUtc).AddDays(1);
        if (!shift.UsesLinkedMovements && await cashLines.AnyAsync(x => x.line.CashShiftId == null && x.journal.PostedAtUtc == null &&
            x.journal.PostingDate >= earliestDate && x.journal.PostingDate <= latestDate, cancellationToken))
            throw new ConflictException("cash_shift_posting_time_missing", "A posted cash movement near this shift has no posting timestamp. Reconcile it before closing.");

        // An opening ledger entry is not an additional physical receipt during a shift.
        // Do not guess the physical effect of opening/closing journals posted during it.
        if (await cashLines.AnyAsync(x => x.line.CashShiftId == shift.Id &&
            (x.journal.PostedAtUtc == null || x.journal.PostedAtUtc > throughUtc), cancellationToken))
            throw new ConflictException("cash_shift_new_movement", "توجد حركة نقدية أحدث من وقت الإغلاق. أعد تحميل الوردية وأعد الجرد.");

        var rows = await cashLines.Where(x => x.line.CashShiftId == shift.Id ||
            (!shift.UsesLinkedMovements && x.line.CashShiftId == null &&
             x.journal.PostedAtUtc >= shift.OpenedAtUtc && x.journal.PostedAtUtc <= throughUtc))
            .Select(x => new
            {
                x.journal.JournalNumber, x.journal.JournalType,
                x.journal.BaseCurrencyId, x.line.TransactionCurrencyId,
                x.line.TransactionDebitAmount, x.line.TransactionCreditAmount,
                x.line.DebitAmount, x.line.CreditAmount
            }).ToListAsync(cancellationToken);

        decimal net = 0m;
        foreach (var row in rows)
        {
            if (row.JournalType is JournalType.Opening or JournalType.Closing)
                throw new ConflictException("cash_shift_non_operating_journal", $"Journal {row.JournalNumber} is an opening/closing entry on the cashbox. Reconcile it before closing.");
            net += ResolveMovement(currencyId, row.BaseCurrencyId, row.TransactionCurrencyId,
                row.TransactionDebitAmount, row.TransactionCreditAmount, row.DebitAmount, row.CreditAmount, row.JournalNumber);
        }
        // Keep negative expected amounts visible: they signal a bookkeeping/opening-count issue.
        return shift.OpeningBalance + net;
    }

    public static decimal ResolveMovement(Guid cashCurrencyId, Guid? baseCurrencyId, Guid? transactionCurrencyId,
        decimal? transactionDebit, decimal? transactionCredit, decimal debit, decimal credit, string journalNumber)
    {
        if (TryResolveMovement(cashCurrencyId, baseCurrencyId, transactionCurrencyId, transactionDebit, transactionCredit, debit, credit, out var movement))
            return movement;
        throw new ConflictException("cash_shift_movement_currency_unknown", $"Journal {journalNumber} has no reliable amount in the cashbox currency. Reconcile it before closing.");
    }

    internal static bool TryResolveMovement(Guid cashCurrencyId, Guid? baseCurrencyId, Guid? transactionCurrencyId,
        decimal? transactionDebit, decimal? transactionCredit, decimal debit, decimal credit, out decimal movement)
    {
        if (transactionCurrencyId == cashCurrencyId && transactionDebit.HasValue && transactionCredit.HasValue)
        {
            movement = transactionDebit.Value - transactionCredit.Value;
            return true;
        }
        // Base-only legacy line: use only when the historical base currency is known.
        if (transactionCurrencyId == null && baseCurrencyId == cashCurrencyId)
        {
            movement = debit - credit;
            return true;
        }
        movement = 0m;
        return false;
    }
}
