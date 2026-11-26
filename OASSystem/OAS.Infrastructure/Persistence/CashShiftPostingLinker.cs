using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Accounting.Persistence.Queries;

namespace OAS.Infrastructure.Persistence;

internal static class CashShiftPostingLinker
{
    internal static EntityEntry<JournalEntry>[] NewPostings(OasDbContext db) => db.ChangeTracker.Entries<JournalEntry>()
        .Where(e => e.Entity.Status == JournalEntryStatus.Posted &&
            (e.State == EntityState.Added || (e.State == EntityState.Modified && e.Property(x => x.Status).OriginalValue != JournalEntryStatus.Posted)))
        .ToArray();

    internal static EntityEntry<CashShift>[] ShiftChanges(OasDbContext db) => db.ChangeTracker.Entries<CashShift>()
        .Where(e => e.State == EntityState.Added || (e.State == EntityState.Modified && e.Property(x => x.Status).OriginalValue != e.Entity.Status)).ToArray();

    internal static async Task PrepareAsync(OasDbContext db, EntityEntry<JournalEntry>[] postings,
        EntityEntry<CashShift>[] changes, CancellationToken ct)
    {
        // Called inside the save transaction; serialize cash posting and shift boundaries.
        // No lock is taken for unrelated non-cash writes.
        // Existing journals may be loaded without lines. Load missing lines via the tracked collection.
        foreach (var e in postings.Where(e => e.State != EntityState.Added && !e.Collection(x => x.Lines).IsLoaded))
            await e.Collection(x => x.Lines).LoadAsync(ct);
        var accountIds = postings.SelectMany(e => e.Entity.Lines).Select(x => x.AccountId).Distinct().ToArray();
        var cash = accountIds.Length == 0 ? new List<CashAccount>() : await db.Set<CashAccount>().AsNoTracking()
            .Where(x => accountIds.Contains(x.AccountId)).ToListAsync(ct);
        if (cash.Count == 0 && changes.Length == 0) return;

        if (db.Database.IsSqlServer())
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = N'OAS.CashShift.Posting.v1',
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
                IF @result < 0 THROW 51052, 'Cash shift posting lock unavailable. Retry the operation.', 1;
                """, ct);

        // Re-read configuration after lock acquisition.
        if (accountIds.Length != 0)
            cash = await db.Set<CashAccount>().AsNoTracking().Where(x => accountIds.Contains(x.AccountId)).ToListAsync(ct);
        var cashIds = cash.Select(x => x.Id).Concat(changes.Select(x => x.Entity.CashAccountId)).Distinct().ToArray();
        var active = await db.Set<CashShift>().AsNoTracking().Where(x => cashIds.Contains(x.CashAccountId) &&
            (x.Status == CashShiftStatus.Open || x.Status == CashShiftStatus.Closing)).ToListAsync(ct);
        var bankAccountIds = accountIds.Length == 0 ? new List<Guid>() : await db.Set<BankAccount>().AsNoTracking()
            .Where(x => accountIds.Contains(x.AccountId)).Select(x => x.AccountId).ToListAsync(ct);

        foreach (var change in changes.Where(x => x.State == EntityState.Added))
        {
            var shift = change.Entity;
            if (active.Any(x => x.CashAccountId == shift.CashAccountId) || changes.Count(x => x.State == EntityState.Added && x.Entity.CashAccountId == shift.CashAccountId) > 1)
                throw new ConflictException("cash_shift_already_open", "توجد وردية مفتوحة أو قيد الإغلاق لهذا الصندوق.");
            var box = await db.Set<CashAccount>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == shift.CashAccountId, ct);
            if (box is null || !box.IsActive || !box.CurrencyId.HasValue)
                throw new ConflictException("cash_shift_invalid_cashbox", "يجب اختيار صندوق نشط ومحدد العملة.");
            if (await db.Set<CashAccount>().AnyAsync(x => x.Id != box.Id && x.AccountId == box.AccountId, ct) ||
                await db.Set<BankAccount>().AnyAsync(x => x.AccountId == box.AccountId, ct))
                throw new ConflictException("cash_shift_shared_ledger", "لا يمكن فتح وردية لصندوق يشترك في حسابه المحاسبي مع صندوق أو بنك آخر.");
            // Opening and posting in one unit of work is not an existing application flow.
            if (cash.Any(x => x.Id == box.Id))
                throw new ConflictException("cash_shift_mixed_operation", "افتح الوردية واحفظها قبل ترحيل الحركة النقدية.");
        }

        foreach (var posting in postings)
        foreach (var line in posting.Entity.Lines)
        {
            var boxes = cash.Where(x => x.AccountId == line.AccountId).ToArray();
            if (boxes.Length == 0) continue; // Banks and other accounts have no cash shifts.
            var shifts = active.Where(x => boxes.Any(b => b.Id == x.CashAccountId)).ToArray();
            if (shifts.Length == 0)
            {
                if (line.CashShiftId.HasValue)
                    throw new ConflictException("cash_shift_not_open", "لا يمكن إسناد حركة إلى وردية مغلقة.");
                // A posting that began before a concurrent close must be retried, not silently omitted.
                var boxIds = boxes.Select(b => b.Id).ToArray();
                if (posting.Entity.PostedAtUtc is DateTime postedAt && await db.Set<CashShift>().AnyAsync(x =>
                    boxIds.Contains(x.CashAccountId) &&
                    x.ClosedAtUtc != null && x.OpenedAtUtc <= postedAt && x.ClosedAtUtc >= postedAt, ct))
                    throw new ConflictException("cash_shift_posting_after_close", "أغلقت الوردية أثناء الترحيل. راجع الصندوق وأعد العملية.");
                continue; // Preserve cash accounting outside shifts.
            }
            if (boxes.Length != 1 || shifts.Length != 1 || bankAccountIds.Contains(line.AccountId))
                throw new ConflictException("cash_shift_shared_ledger", "تعذر تحديد وردية واحدة للحساب النقدي المشترك.");
            var shift = shifts[0];
            var box = boxes[0];
            if (shift.Status != CashShiftStatus.Open || changes.Any(x => x.Entity.Id == shift.Id))
                throw new ConflictException("cash_shift_closing", "الصندوق قيد الجرد والإغلاق. لا يمكن ترحيل حركة جديدة خلال الإغلاق.");
            if (posting.Entity.PostedAtUtc is not DateTime time || time < shift.OpenedAtUtc)
                throw new ConflictException("cash_shift_posting_time_invalid", "وقت الترحيل لا يتوافق مع الوردية المفتوحة. أعد العملية.");
            if (posting.Entity.JournalType is JournalType.Opening or JournalType.Closing)
                throw new ConflictException("cash_shift_non_operating_journal", "أغلق الوردية قبل ترحيل قيد افتتاح أو إقفال على الصندوق.");
            if (box.CurrencyId is not Guid currencyId)
                throw new ConflictException("cash_shift_currency_missing", "عملة الصندوق غير محددة.");
            _ = CashShiftBalanceService.ResolveMovement(currencyId, posting.Entity.BaseCurrencyId, line.TransactionCurrencyId,
                line.TransactionDebitAmount, line.TransactionCreditAmount, line.DebitAmount, line.CreditAmount, posting.Entity.JournalNumber);
            line.AssignCashShift(shift.Id);
        }

        foreach (var change in changes.Where(x => x.State == EntityState.Modified))
        {
            var shift = change.Entity;
            if (shift.Status == CashShiftStatus.Closing)
                shift.RefreshExpectedClosingBalance(await new CashShiftBalanceService(db).CalculateExpectedAsync(shift, DateTime.UtcNow, ct));
            else if (shift.Status == CashShiftStatus.Closed && shift.ClosedAtUtc is DateTime closedAt)
                shift.ReconcileClosingBalance(await new CashShiftBalanceService(db).CalculateExpectedAsync(shift, closedAt, ct));
        }
    }
}
