using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Accounting.Persistence.Queries;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Accounting.Infrastructure;

[TestFixture, NonParallelizable, Category("CashShiftSqlServer")]
public sealed class CashShiftMovementSqlServerTests
{
    [Test]
    public async Task PostingLinksOnlyCashLineAndClosingUsesLinkedMovements()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_SALES_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_SALES_TEST_SQLSERVER to run this isolated SQL Server test.");
            return;
        }
        var connection = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_Shift_{Guid.NewGuid():N}"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(connection).Options;
        await using var db = new OasDbContext(options);
        try
        {
            await db.Database.MigrateAsync();
            var now = DateTime.UtcNow.AddMinutes(-2);
            var date = DateOnly.FromDateTime(now);
            var currency = Currency.Create(Guid.NewGuid(), "TST", "Test currency", null, null, 2);
            var year = FiscalYear.Create(Guid.NewGuid(), "SHIFT-TEST", "Test year", date.AddDays(-1), date.AddDays(2), FiscalYearStatus.Open);
            var period = FiscalPeriod.Create(Guid.NewGuid(), year.Id, 1, "Test period", date.AddDays(-1), date.AddDays(2), FiscalPeriodStatus.Open, false, false, false);
            Account NewAccount(string code, AccountClass cls, NormalBalance normal) => Account.Create(Guid.NewGuid(), code, code, null,
                null, 1, cls, AccountType.Posting, normal, true, false, false, false, true, null);
            var ledger = NewAccount("SHIFT-CASH", AccountClass.Asset, NormalBalance.Debit);
            var counterpart = NewAccount("SHIFT-REV", AccountClass.Revenue, NormalBalance.Credit);
            var box = CashAccount.Create(Guid.NewGuid(), "SHIFT-BOX", "Test box", ledger.Id, currency.Id, false, true);
            db.AddRange(currency, year, period, ledger, counterpart, box);
            await db.SaveChangesAsync();
            var actor = Guid.NewGuid();
            var shift = CashShift.Create(Guid.NewGuid(), "SHIFT-1", box.Id, actor, now, 100000m, CashShiftStatus.Open);
            shift.SetOpeningDifferenceReason("Test opening cash count");
            db.Add(shift);
            await db.SaveChangesAsync();

            JournalEntry MakeJournal(string number, decimal signedAmount, DateTime at)
            {
                var journal = JournalEntry.Create(Guid.NewGuid(), number, JournalType.Manual, date, date, period.Id,
                    "Test movement", null, null, null, JournalEntryStatus.Draft);
                journal.SetBaseCurrencySnapshot(currency.Id, currency.Code, currency.DecimalPlaces);
                journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 1, ledger.Id,
                    Math.Max(signedAmount, 0m), Math.Max(-signedAmount, 0m), null, null, null, null, null, null));
                journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 2, counterpart.Id,
                    Math.Max(-signedAmount, 0m), Math.Max(signedAmount, 0m), null, null, null, null, null, null));
                journal.SetPendingApproval(); journal.Approve(actor, at); journal.Post(actor, at);
                return journal;
            }
            var receipt = MakeJournal("SHIFT-IN", 50000m, now.AddSeconds(1));
            var payment = MakeJournal("SHIFT-OUT", -20000m, now.AddSeconds(2));
            db.AddRange(receipt, payment);
            await db.SaveChangesAsync();
            var persisted = await db.Set<JournalEntryLine>().AsNoTracking().Where(x => x.JournalEntryId == receipt.Id).ToListAsync();
            Assert.That(persisted.Single(x => x.AccountId == ledger.Id).CashShiftId, Is.EqualTo(shift.Id));
            Assert.That(persisted.Single(x => x.AccountId == counterpart.Id).CashShiftId, Is.Null);
            var expected = await new CashShiftBalanceService(db).CalculateExpectedAsync(shift, DateTime.UtcNow);
            Assert.That(expected, Is.EqualTo(130000m));
            shift.StartClosing(expected);
            await db.SaveChangesAsync();
            var blocked = MakeJournal("SHIFT-BLOCKED", 1m, DateTime.UtcNow);
            db.Add(blocked);
            Assert.ThrowsAsync<ConflictException>(async () => await db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            shift = await db.Set<CashShift>().SingleAsync(x => x.Id == shift.Id);
            shift.Close(125000m, actor, DateTime.UtcNow);
            await db.SaveChangesAsync();
            Assert.That(shift.DifferenceAmount, Is.EqualTo(-5000m));
            var movements = await new CashShiftMovementQueryService(db).GetAsync(shift.Id, 1, CancellationToken.None);
            Assert.That(movements.TotalCount, Is.EqualTo(2));
            // Cash accounting without an active shift remains permitted.
            var later = MakeJournal("SHIFT-LATER", 100m, shift.ClosedAtUtc!.Value.AddSeconds(1));
            db.Add(later);
            await db.SaveChangesAsync();
            Assert.That(later.Lines.All(x => !x.CashShiftId.HasValue), Is.True);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
