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

            // A generic repository update marks every scalar property modified.
            // Ordinary settings must still be editable with an open shift.
            box.UpdateDetails(box.Code, "Renamed box", box.AccountId, box.CurrencyId, true);
            db.Set<CashAccount>().Update(box);
            await db.SaveChangesAsync();
            Assert.That(box.Name, Is.EqualTo("Renamed box"));
            Assert.That(box.IsDefault, Is.True);

            var boxId = box.Id;
            var shiftId = shift.Id;
            box.SetMovementType(CashAccountMovementType.ReceiptOnly);
            db.Set<CashAccount>().Update(box);
            var configurationError = Assert.ThrowsAsync<ConflictException>(async () => await db.SaveChangesAsync());
            Assert.That(configurationError!.Code, Is.EqualTo("cash_account_active_shift"));
            db.ChangeTracker.Clear();
            box = await db.Set<CashAccount>().SingleAsync(x => x.Id == boxId);
            shift = await db.Set<CashShift>().SingleAsync(x => x.Id == shiftId);
            Assert.That(box.MovementType, Is.EqualTo(CashAccountMovementType.ReceiptAndPayment));

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
            box = await db.Set<CashAccount>().SingleAsync(x => x.Id == box.Id);
            Assert.That(box.MovementType, Is.EqualTo(CashAccountMovementType.ReceiptAndPayment));
            box.SetMovementType(CashAccountMovementType.ReceiptOnly);
            await db.SaveChangesAsync();
            db.Add(MakeJournal("SHIFT-WRONG-DIRECTION", -1m, DateTime.UtcNow));
            Assert.ThrowsAsync<ConflictException>(async () => await db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            db.Add(MakeJournal("SHIFT-ALLOWED-DIRECTION", 1m, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [Test]
    public async Task GroupedShiftReconciliationSeparatesCurrenciesAndIncludesReversal()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_SALES_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured)) { Assert.Ignore("Set OAS_SALES_TEST_SQLSERVER to run this isolated SQL Server test."); return; }
        var connection = new SqlConnectionStringBuilder(configured) { InitialCatalog = $"OAS_Shift_{Guid.NewGuid():N}" }.ConnectionString;
        await using var db = new OasDbContext(new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(connection).Options);
        try
        {
            await db.Database.MigrateAsync();
            var now = DateTime.UtcNow.AddMinutes(-2);
            var date = DateOnly.FromDateTime(now);
            var actor = Guid.NewGuid();
            var yer = Currency.Create(Guid.NewGuid(), "QYR", "يمني", null, null, 0);
            var sar = Currency.Create(Guid.NewGuid(), "QSR", "سعودي", null, null, 2);
            var usd = Currency.Create(Guid.NewGuid(), "QUS", "دولار", null, null, 2);
            var year = FiscalYear.Create(Guid.NewGuid(), "GROUP-TEST", "Test year", date.AddDays(-1), date.AddDays(2), FiscalYearStatus.Open);
            var period = FiscalPeriod.Create(Guid.NewGuid(), year.Id, 1, "Test period", date.AddDays(-1), date.AddDays(2), FiscalPeriodStatus.Open, false, false, false);
            Account Ledger(string code, AccountClass cls, NormalBalance normal) => Account.Create(Guid.NewGuid(), code, code, null,
                null, 1, cls, AccountType.Posting, normal, true, false, false, false, true, null);
            var currencies = new[] { yer, sar, usd };
            var ledgers = currencies.Select(c => Ledger("GROUP-" + c.Code, AccountClass.Asset, NormalBalance.Debit)).ToArray();
            var counterpart = Ledger("GROUP-OFFSET", AccountClass.Revenue, NormalBalance.Credit);
            db.AddRange(yer, sar, usd, year, period, counterpart);
            db.AddRange(ledgers);
            await db.SaveChangesAsync();
            var boxes = currencies.Select((c, i) => CashAccount.Create(Guid.NewGuid(), "BOX-" + c.Code, c.NameAr, ledgers[i].Id, c.Id, false, true)).ToArray();
            db.Add(boxes[0]); await db.SaveChangesAsync();
            foreach (var box in boxes.Skip(1)) { box.JoinCashBox(boxes[0].CashBoxId); db.Add(box); }
            await db.SaveChangesAsync();
            var group = Guid.NewGuid();
            var opening = new[] { 100000m, 1000m, 100m };
            var shifts = boxes.Select((b, i) => CashShift.Create(Guid.NewGuid(), $"GROUP-SHIFT-{i}", b.Id, actor, now, opening[i], CashShiftStatus.Open)).ToArray();
            foreach (var shift in shifts) { shift.JoinCashBoxShift(group); shift.SetOpeningDifferenceReason("Opening count for test"); db.Add(shift); }
            await db.SaveChangesAsync();
            JournalEntry Movement(int i, decimal amount, decimal rate, string suffix, JournalType type = JournalType.Manual)
            {
                var currency = currencies[i];
                var journal = JournalEntry.Create(Guid.NewGuid(), $"GROUP-{i}-{suffix}", type, date, date, period.Id,
                    "Test cash movement", null, null, null, JournalEntryStatus.Draft);
                journal.SetBaseCurrencySnapshot(yer.Id, yer.Code, yer.DecimalPlaces);
                JournalEntryLine Line(int number, Guid accountId, decimal signed) => JournalEntryLine.CreateMultiCurrency(
                    Guid.NewGuid(), journal.Id, number, accountId, Math.Max(signed * rate, 0m), Math.Max(-signed * rate, 0m),
                    currency.Id, currency.Code, currency.DecimalPlaces, Math.Max(signed, 0m), Math.Max(-signed, 0m),
                    rate, date, ExchangeRateType.Accounting, ExchangeRateSource.Manual,
                    null, null, null, null, null, null, null, null, null);
                journal.AddLine(Line(1, ledgers[i].Id, amount)); journal.AddLine(Line(2, counterpart.Id, -amount));
                journal.SetPendingApproval(); journal.Approve(actor, now.AddSeconds(1)); journal.Post(actor, now.AddSeconds(2));
                return journal;
            }
            var receipts = new[] { Movement(0, 50000m, 1m, "IN"), Movement(1, 500m, 100m, "IN"), Movement(2, 50m, 500m, "IN") };
            var payments = new[] { Movement(0, -20000m, 1m, "OUT"), Movement(1, -200m, 110m, "OUT"), Movement(2, -20m, 550m, "OUT") };
            db.AddRange(receipts); db.AddRange(payments); await db.SaveChangesAsync();
            var reversal = JournalEntry.Create(Guid.NewGuid(), "GROUP-2-REV", JournalType.Reversal, date, date, period.Id,
                "Reverse payment", null, null, null, JournalEntryStatus.Draft);
            reversal.SetBaseCurrencySnapshot(yer.Id, yer.Code, yer.DecimalPlaces);
            foreach (var line in payments[2].Lines) reversal.AddLine(line.CreateOpposite(Guid.NewGuid(), reversal.Id));
            reversal.SetPendingApproval(); reversal.Approve(actor, now.AddSeconds(3)); reversal.Post(actor, now.AddSeconds(4));
            payments[2].MarkReversed(reversal.Id); db.Add(reversal); await db.SaveChangesAsync();
            var expected = new[] { 130000m, 1300m, 150m };
            var totalIn = new[] { 50000m, 500m, 70m };
            var totalOut = new[] { 20000m, 200m, 20m };
            var movements = new CashShiftMovementQueryService(db);
            var balance = new CashShiftBalanceService(db);
            for (var i = 0; i < shifts.Length; i++)
            {
                var summary = await movements.GetAsync(shifts[i].Id, 1, CancellationToken.None);
                Assert.That(summary.Reconciliation, Is.Not.Null);
                Assert.That(summary.Reconciliation!.CurrencyId, Is.EqualTo(currencies[i].Id));
                Assert.That(summary.Reconciliation.CurrencyName, Is.EqualTo(currencies[i].NameAr));
                Assert.That(summary.Reconciliation.DecimalPlaces, Is.EqualTo(currencies[i].DecimalPlaces));
                Assert.That(summary.Reconciliation.Received, Is.EqualTo(totalIn[i]));
                Assert.That(summary.Reconciliation.Paid, Is.EqualTo(totalOut[i]));
                Assert.That(summary.Reconciliation.Expected, Is.EqualTo(expected[i]));
                Assert.That(await balance.CalculateExpectedAsync(shifts[i], DateTime.UtcNow), Is.EqualTo(expected[i]));
                Assert.That(summary.TotalCount, Is.EqualTo(i == 2 ? 3 : 2));
                Assert.That(summary.Rows.All(x => x.CurrencyCode == currencies[i].Code), Is.True);
            }
            var actual = new[] { 125000m, 1300m, 155m };
            var closeTime = DateTime.UtcNow;
            for (var i = 0; i < shifts.Length; i++) { shifts[i].StartClosing(expected[i]); shifts[i].Close(actual[i], actor, closeTime); }
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var closed = await db.Set<CashShift>().AsNoTracking().Where(x => x.CashBoxShiftId == group).OrderBy(x => x.ShiftNumber).ToArrayAsync();
            Assert.That(closed.Select(x => x.ExpectedClosingBalance), Is.EqualTo(expected.Select(x => (decimal?)x)));
            Assert.That(closed.Select(x => x.DifferenceAmount), Is.EqualTo(new decimal?[] { -5000m, 0m, 5m }));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

}
