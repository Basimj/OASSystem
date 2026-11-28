using Microsoft.EntityFrameworkCore;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed class CashShiftMovementQueryService(OasDbContext db) : ICashShiftMovementQueryService
{
    public async Task<CashShiftMovementsDto> GetAsync(Guid shiftId, int page, CancellationToken ct)
    {
        if (page < 1 || page > int.MaxValue / 100)
            throw new ConflictException("cash_shift_invalid_page", "رقم الصفحة غير صالح.");
        var shift = await db.Set<CashShift>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == shiftId, ct)
            ?? throw new NotFoundException(nameof(CashShift), shiftId);
        var query = from line in db.Set<JournalEntryLine>().AsNoTracking()
                    join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
                    where line.CashShiftId == shiftId && journal.Status == JournalEntryStatus.Posted
                    select new { line, journal };
        var count = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.journal.PostedAtUtc).ThenBy(x => x.journal.Id).ThenBy(x => x.line.LineNumber)
            .Skip((page - 1) * 100).Take(100)
            .Select(x => new CashShiftMovementDto(x.journal.Id, x.journal.JournalNumber, x.line.LineNumber,
                x.journal.PostingDate, x.line.Description ?? x.journal.Description,
                x.line.DebitAmount, x.line.CreditAmount,
                x.line.TransactionCurrencyCodeSnapshot ?? x.journal.BaseCurrencyCodeSnapshot,
                x.line.TransactionDebitAmount ?? (x.line.TransactionCurrencyId == null ? x.line.DebitAmount : (decimal?)null),
                x.line.TransactionCreditAmount ?? (x.line.TransactionCurrencyId == null ? x.line.CreditAmount : (decimal?)null)))
            .ToListAsync(ct);
        CashShiftReconciliationDto? reconciliation = null;
        if (shift.CashBoxShiftId.HasValue && page == 1)
        {
            // Resolve the currency from persisted account data, including inactive historical currencies.
            var currency = await (from cash in db.Set<CashAccount>().AsNoTracking()
                                  join c in db.Set<Currency>().AsNoTracking() on cash.CurrencyId equals (Guid?)c.Id
                                  where cash.Id == shift.CashAccountId
                                  select c).SingleOrDefaultAsync(ct)
                ?? throw new ConflictException("cash_shift_currency_missing", "تعذر تحديد عملة حساب الوردية.");
            var totals = await new CashShiftBalanceService(db).CalculateTotalsAsync(shift,
                shift.ClosedAtUtc ?? DateTime.UtcNow, ct);
            reconciliation = new CashShiftReconciliationDto(currency.Id, currency.Code, currency.NameAr,
                currency.DecimalPlaces, totals.Received, totals.Paid,
                shift.OpeningBalance + totals.Received - totals.Paid);
        }
        return new CashShiftMovementsDto(shift.UsesLinkedMovements, count, rows, reconciliation);
    }
}
