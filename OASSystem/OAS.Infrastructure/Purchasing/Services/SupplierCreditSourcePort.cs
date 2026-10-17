using Microsoft.EntityFrameworkCore;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Services;

public sealed class SupplierCreditSourcePort(OasDbContext db) : ISupplierCreditSourcePort
{
    private const string PurchasingModule = "Purchasing";
    private const string PurchaseReturnDocumentType = "PurchaseReturn";

    public async Task<decimal> GetPostedApCreditBaseAmountAsync(
        Guid purchaseReturnId,
        Guid journalEntryId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        if (purchaseReturnId == Guid.Empty || journalEntryId == Guid.Empty || supplierId == Guid.Empty)
            return 0m;

        var supplierAccountId = db.ChangeTracker.Entries<Supplier>()
            .Select(x => x.Entity)
            .Where(x => x.Id == supplierId)
            .Select(x => (Guid?)x.AccountId)
            .SingleOrDefault();

        supplierAccountId ??= await db.Suppliers
            .AsNoTracking()
            .Where(x => x.Id == supplierId)
            .Select(x => (Guid?)x.AccountId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!supplierAccountId.HasValue || supplierAccountId.Value == Guid.Empty)
            return 0m;

        // During purchase-return posting the journal can still be tracked but not yet flushed.
        // Inspect the tracked aggregate first so AutoApply remains inside the posting transaction.
        var trackedJournal = db.ChangeTracker.Entries<JournalEntry>()
            .Select(x => x.Entity)
            .SingleOrDefault(x => x.Id == journalEntryId);

        if (trackedJournal is not null)
        {
            if (!IsExpectedPostedReturnJournal(trackedJournal, purchaseReturnId))
                return 0m;

            return PositiveRound(trackedJournal.Lines
                .Where(x => x.AccountId == supplierAccountId.Value)
                .Sum(x => x.DebitAmount - x.CreditAmount));
        }

        var validJournal = await db.Set<JournalEntry>()
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == journalEntryId &&
                     x.Status == JournalEntryStatus.Posted &&
                     x.SourceModule == PurchasingModule &&
                     x.SourceDocumentType == PurchaseReturnDocumentType &&
                     x.SourceDocumentId == purchaseReturnId,
                cancellationToken);

        if (!validJournal)
            return 0m;

        var movement = await db.Set<JournalEntryLine>()
            .AsNoTracking()
            .Where(x => x.JournalEntryId == journalEntryId && x.AccountId == supplierAccountId.Value)
            .Select(x => (decimal?)(x.DebitAmount - x.CreditAmount))
            .SumAsync(cancellationToken) ?? 0m;

        return PositiveRound(movement);
    }

    private static bool IsExpectedPostedReturnJournal(JournalEntry journal, Guid purchaseReturnId) =>
        journal.Status == JournalEntryStatus.Posted &&
        string.Equals(journal.SourceModule, PurchasingModule, StringComparison.Ordinal) &&
        string.Equals(journal.SourceDocumentType, PurchaseReturnDocumentType, StringComparison.Ordinal) &&
        journal.SourceDocumentId == purchaseReturnId;

    private static decimal PositiveRound(decimal value) =>
        Math.Max(0m, Math.Round(value, 4, MidpointRounding.AwayFromZero));
}
