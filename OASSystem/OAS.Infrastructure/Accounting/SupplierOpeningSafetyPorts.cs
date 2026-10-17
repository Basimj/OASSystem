using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting;

public sealed class SupplierOpeningJournalVerificationPort(OasDbContext db) : ISupplierOpeningJournalVerificationPort
{
    public async Task VerifyExistingJournalAsync(
        Guid journalEntryId,
        DateOnly asOfDate,
        IReadOnlyCollection<SupplierOpeningJournalExpectation> expectations,
        CancellationToken cancellationToken = default)
    {
        if (journalEntryId == Guid.Empty) throw new ArgumentException("Journal entry id is required.", nameof(journalEntryId));
        if (expectations.Count == 0) throw new ConflictException("supplier_opening_items_required", "لا توجد أرصدة افتتاحية للتحقق منها.");

        var journal = await db.Set<JournalEntry>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == journalEntryId, cancellationToken)
            ?? throw new NotFoundException(nameof(JournalEntry), journalEntryId);
        if (journal.Status != JournalEntryStatus.Posted)
            throw new ConflictException("supplier_opening_journal_not_posted", "القيد المحدد للأرصدة الافتتاحية غير مرحل.");
        if (journal.PostingDate != asOfDate)
            throw new ConflictException("supplier_opening_journal_date_mismatch", "تاريخ القيد الافتتاحي لا يطابق تاريخ الدفعة الافتتاحية.");

        var lines = await db.Set<JournalEntryLine>().AsNoTracking()
            .Where(x => x.JournalEntryId == journalEntryId)
            .ToListAsync(cancellationToken);
        if (lines.Count == 0 || Math.Round(lines.Sum(x => x.DebitAmount) - lines.Sum(x => x.CreditAmount), 4) != 0m)
            throw new ConflictException("supplier_opening_journal_unbalanced", "القيد الافتتاحي غير متوازن.");

        var expectedAccountIds = expectations.Select(x => x.SupplierAccountId).ToHashSet();
        var supplierAccountIds = await db.Set<Supplier>().AsNoTracking().Select(x => x.AccountId).ToListAsync(cancellationToken);
        if (lines.Any(x => supplierAccountIds.Contains(x.AccountId) && !expectedAccountIds.Contains(x.AccountId)))
            throw new ConflictException("supplier_opening_journal_unexpected_supplier", "القيد الافتتاحي يحتوي حركة مورد غير موجودة في تفاصيل الدفعة.");

        foreach (var expected in expectations)
        {
            var actual = Math.Round(lines
                .Where(x => x.AccountId == expected.SupplierAccountId && x.SupplierId == expected.SupplierId)
                .Sum(x => x.CreditAmount - x.DebitAmount), 4, MidpointRounding.AwayFromZero);
            if (actual != Math.Round(expected.BaseAmount, 4, MidpointRounding.AwayFromZero))
                throw new ConflictException("supplier_opening_journal_supplier_mismatch", "أرصدة الموردين في القيد الافتتاحي لا تطابق تفاصيل الدفعة.");
        }
    }
}

public sealed class SupplierOpeningConcurrencyPort(OasDbContext db) : ISupplierOpeningConcurrencyPort
{
    public async Task LockPaymentLineAndOpeningItemAsync(Guid paymentVoucherLineId, Guid supplierOpeningItemId, CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Supplier opening allocation requires an active database transaction.");
        await db.Database.OpenConnectionAsync(cancellationToken);
        await LockAsync("tbl_PaymentVoucherLines", paymentVoucherLineId, cancellationToken);
        await LockAsync("tbl_SupplierOpeningItems", supplierOpeningItemId, cancellationToken);
    }

    private async Task LockAsync(string tableName, Guid id, CancellationToken ct)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = $"SELECT [Id] FROM [dbo].[{tableName}] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = id;
        command.Parameters.Add(parameter);
        await command.ExecuteScalarAsync(ct);
    }
}
