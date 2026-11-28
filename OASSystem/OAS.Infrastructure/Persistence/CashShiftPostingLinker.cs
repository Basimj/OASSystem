using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OAS.Application.Common.Exceptions;
using OAS.Application.Accounting.CashAccounts;
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

    internal static bool HasCashConfigurationChanges(OasDbContext db) =>
        db.ChangeTracker.Entries<CashAccount>().Any(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) ||
        db.ChangeTracker.Entries<BankAccount>().Any(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

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
        var hasBankPosting = accountIds.Length != 0 && await db.Set<BankAccount>().AnyAsync(x => accountIds.Contains(x.AccountId), ct);
        if (cash.Count == 0 && !hasBankPosting && changes.Length == 0 && !HasCashConfigurationChanges(db)) return;

        if (db.Database.IsSqlServer())
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = N'OAS.CashShift.Posting.v1',
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
                IF @result < 0 THROW 51052, 'Cash shift posting lock unavailable. Retry the operation.', 1;
                """, ct);

        await ValidateCashConfigurationAsync(db, ct);
        await PaymentVoucherBalanceQueryService.ValidatePostingsAsync(db, postings.Select(x => x.Entity).ToArray(), ct);

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
            var bookBalance = await new CashShiftBalanceService(db).GetBookBalanceAsync(box.Id, ct);
            if (shift.ExpectedBookOpeningBalance is decimal preview && preview != bookBalance)
                throw new ConflictException("cash_shift_opening_balance_changed", "تغير الرصيد الدفتري. حدّث الرصيد قبل فتح الوردية.");
            if (bookBalance != shift.OpeningBalance && string.IsNullOrWhiteSpace(shift.OpeningDifferenceReason))
                throw new ConflictException("cash_shift_opening_reason_required", "أدخل سبب الفرق بين الرصيد الدفتري والجرد الفعلي عند الافتتاح.");
            shift.RecordBookOpeningBalance(bookBalance);
            // Opening and posting in one unit of work is not an existing application flow.
            if (cash.Any(x => x.Id == box.Id))
                throw new ConflictException("cash_shift_mixed_operation", "افتح الوردية واحفظها قبل ترحيل الحركة النقدية.");
        }

        foreach (var posting in postings)
        foreach (var line in posting.Entity.Lines)
        {
            var boxes = cash.Where(x => x.AccountId == line.AccountId).ToArray();
            if (boxes.Length == 0) continue; // Banks and other accounts have no cash shifts.
            if (boxes.Length != 1 || bankAccountIds.Contains(line.AccountId))
                throw new ConflictException("cash_shift_shared_ledger", "الحساب المحاسبي مشترك بين أكثر من صندوق أو بنك.");
            if (!boxes[0].IsActive)
                throw new ConflictException("cash_account_inactive", "لا يمكن الترحيل إلى صندوق غير نشط.");
            var journal = posting.Entity;
            // Internal transfers, verified reversals and shift settlements are not operating receipts/payments.
            // Manual journals still pass the direction check (linked accounts also disallow manual posting).
            if (journal.JournalType is not (JournalType.Opening or JournalType.Closing or JournalType.Reversal) &&
                journal.SourceDocumentType is not ("CashTransfer" or "CashShiftSettlement"))
            {
                if (line.DebitAmount > 0) CashAccountMovementPolicy.EnsureAllowed(boxes[0], true);
                if (line.CreditAmount > 0) CashAccountMovementPolicy.EnsureAllowed(boxes[0], false);
            }
            var shifts = active.Where(x => boxes.Any(b => b.Id == x.CashAccountId)).ToArray();
            if (shifts.Length == 0)
            {
                if (line.CashShiftId.HasValue)
                    throw new ConflictException("cash_shift_not_open", "لا يمكن إسناد حركة إلى وردية مغلقة.");
                var settlement = posting.Entity.SourceDocumentType == "CashShiftSettlement" && posting.Entity.SourceDocumentId is Guid settlementShiftId &&
                    db.ChangeTracker.Entries<CashShift>().Any(x => x.Entity.Id == settlementShiftId && x.Entity.CashAccountId == boxes[0].Id &&
                        x.Entity.SettlementJournalEntryId == posting.Entity.Id && x.Entity.Status == CashShiftStatus.Approved);
                if (posting.Entity.SourceDocumentType == "CashShiftSettlement" && !settlement)
                    throw new ConflictException("cash_shift_settlement_invalid", "قيد تسوية الوردية غير مرتبط بتسوية معتمدة.");
                // Opening, closing and reviewed shift adjustments are accounting-only movements.
                if (boxes[0].RequiresOpenShift && !settlement && posting.Entity.JournalType is not (JournalType.Opening or JournalType.Closing))
                    throw new ConflictException("cash_shift_required", "يجب فتح وردية لهذا الصندوق قبل ترحيل الحركة.");
                if (settlement) continue;
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
            if (posting.Entity.SourceDocumentType == "CashShiftSettlement")
                throw new ConflictException("cash_shift_settlement_active", "لا تسوى فروقات الورديات أثناء وردية نشطة.");
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
    private static async Task ValidateCashConfigurationAsync(OasDbContext db, CancellationToken ct)
    {
        var cash = db.ChangeTracker.Entries<CashAccount>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var banks = db.ChangeTracker.Entries<BankAccount>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        if (cash.Length == 0 && banks.Length == 0) return;
        var activeStatuses = new[] { CashShiftStatus.Open, CashShiftStatus.Closing };
        foreach (var entry in cash)
        {
            var box = entry.Entity;
            if (!await db.Set<CashShift>().AnyAsync(x => x.CashAccountId == box.Id && activeStatuses.Contains(x.Status), ct))
                continue;

            // DbSet.Update marks all properties modified, including unchanged keys.
            // Compare persisted values under the posting lock, also covering detached updates.
            var stored = entry.State == EntityState.Modified
                ? await db.Set<CashAccount>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == box.Id, ct)
                : null;
            if (entry.State == EntityState.Deleted || !box.IsActive ||
                (entry.State == EntityState.Modified && (stored is null || stored.AccountId != box.AccountId ||
                    stored.CurrencyId != box.CurrencyId || stored.MovementType != box.MovementType)))
                throw new ConflictException("cash_account_active_shift", "أغلق الوردية النشطة قبل تعطيل الصندوق أو تغيير حسابه أو عملته أو نوع حركته.");
        }
        var cashIds = cash.Select(x => x.Entity.Id).ToArray();
        var bankIds = banks.Select(x => x.Entity.Id).ToArray();
        var pending = cash.Where(x => x.State != EntityState.Deleted).Select(x => x.Entity.AccountId)
            .Concat(banks.Where(x => x.State != EntityState.Deleted).Select(x => x.Entity.AccountId)).ToArray();
        if (pending.Distinct().Count() != pending.Length ||
            await db.Set<CashAccount>().AnyAsync(x => !cashIds.Contains(x.Id) && pending.Contains(x.AccountId), ct) ||
            await db.Set<BankAccount>().AnyAsync(x => !bankIds.Contains(x.Id) && pending.Contains(x.AccountId), ct))
            throw new ConflictException("cash_bank_shared_ledger", "لا يجوز ربط أكثر من صندوق أو بنك بالحساب المحاسبي نفسه.");
    }
}
