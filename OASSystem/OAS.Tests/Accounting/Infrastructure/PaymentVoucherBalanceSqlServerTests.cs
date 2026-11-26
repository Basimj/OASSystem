using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Accounting.Persistence.Queries;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Accounting.Infrastructure;

[TestFixture, NonParallelizable, Category("VoucherBalanceSqlServer")]
public sealed class PaymentVoucherBalanceSqlServerTests
{
    [Test]
    public async Task PaymentChecksCombinedOutflowBeforePostingAndDoesNotChangeLedgerOnFailure()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_SALES_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured)) { Assert.Ignore("Set OAS_SALES_TEST_SQLSERVER for this isolated SQL Server test."); return; }
        var connection = new SqlConnectionStringBuilder(configured) { InitialCatalog = $"OAS_Voucher_{Guid.NewGuid():N}" }.ConnectionString;
        var options = new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(connection).Options;
        await using var db = new OasDbContext(options);
        try
        {
            await db.Database.MigrateAsync();
            var date = DateOnly.FromDateTime(DateTime.UtcNow);
            var currency = Currency.Create(Guid.NewGuid(), "VBT", "Voucher test", null, null, 2);
            var year = FiscalYear.Create(Guid.NewGuid(), "VBT-YEAR", "Test year", date.AddDays(-1), date.AddDays(2), FiscalYearStatus.Open);
            var period = FiscalPeriod.Create(Guid.NewGuid(), year.Id, 1, "Test period", date.AddDays(-1), date.AddDays(2), FiscalPeriodStatus.Open, false, false, false);
            var ledger = Account.Create(Guid.NewGuid(), "VBT-CASH", "Cash", null, null, 1, AccountClass.Asset, AccountType.Posting, NormalBalance.Debit, true, false, false, false, true, null);
            var counterpart = Account.Create(Guid.NewGuid(), "VBT-PARTY", "Party", null, null, 1, AccountClass.Expense, AccountType.Posting, NormalBalance.Debit, true, false, true, false, true, null);
            var box = CashAccount.Create(Guid.NewGuid(), "VBT-BOX", "Test box", ledger.Id, currency.Id, false, true);
            db.AddRange(currency, year, period, ledger, counterpart, box, AccountingSettings.Create(currency.Id));
            await db.SaveChangesAsync();
            var actor = Guid.NewGuid();
            JournalEntry MakeJournal(string number, bool outgoing, params decimal[] amounts)
            {
                var journal = JournalEntry.Create(Guid.NewGuid(), number, JournalType.Automatic, date, date, period.Id,
                    "Voucher balance test", "Accounting", outgoing ? "PaymentVoucher" : "ReceiptVoucher", Guid.NewGuid(), JournalEntryStatus.Draft);
                journal.SetBaseCurrencySnapshot(currency.Id, currency.Code, currency.DecimalPlaces);
                foreach (var amount in amounts)
                {
                    journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, journal.Lines.Count + 1, ledger.Id,
                        outgoing ? 0m : amount, outgoing ? amount : 0m, null, null, null, null, null, null));
                    journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, journal.Lines.Count + 1, counterpart.Id,
                        outgoing ? amount : 0m, outgoing ? 0m : amount, null, null, null, null, null, null));
                }
                var now = DateTime.UtcNow;
                journal.SetPendingApproval(); journal.Approve(actor, now); journal.Post(actor, now);
                return journal;
            }
            db.Add(MakeJournal("VBT-IN", false, 100m));
            await db.SaveChangesAsync();
            var balances = new PaymentVoucherBalanceQueryService(db);
            Assert.That((await balances.GetAsync(box.Id, false, date, default)).AvailableAmount, Is.EqualTo(100m));
            db.Add(MakeJournal("VBT-BLOCKED", true, 60m, 60m));
            Assert.ThrowsAsync<ConflictException>(async () => await db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            Assert.That(await db.Set<JournalEntry>().AnyAsync(x => x.JournalNumber == "VBT-BLOCKED"), Is.False);
            Assert.That((await balances.GetAsync(box.Id, false, date, default)).AvailableAmount, Is.EqualTo(100m));
            db.Add(MakeJournal("VBT-OUT", true, 40m, 60m));
            await db.SaveChangesAsync();
            Assert.That((await balances.GetAsync(box.Id, false, date, default)).AvailableAmount, Is.EqualTo(0m));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
