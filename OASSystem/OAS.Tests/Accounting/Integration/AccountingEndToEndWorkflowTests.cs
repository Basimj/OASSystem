using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Integration;

[TestFixture]
public class AccountingEndToEndWorkflowTests
{
    [Test]
    public void FullAccountingLifecycleWorkflow_ExecutesSuccessfully()
    {
        var adminUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // 1. Setup Chart of Accounts
        var cashAccount = Account.Create(
            Guid.NewGuid(), "1101", "الصندوق الرئيسي", "Main Cash", null, 1,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: true,
            isSystemAccount: false, isActive: true, effectiveDate: today);

        var bankAccount = Account.Create(
            Guid.NewGuid(), "1102", "بنك التضامن", "Tadhamon Bank", null, 1,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: true,
            isSystemAccount: false, isActive: true, effectiveDate: today);

        var salesRevenueAccount = Account.Create(
            Guid.NewGuid(), "4101", "إيراد المبيعات", "Sales Revenue", null, 1,
            AccountClass.Revenue, AccountType.Posting, NormalBalance.Credit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: true,
            isSystemAccount: false, isActive: true, effectiveDate: today);

        var maintenanceExpenseAccount = Account.Create(
            Guid.NewGuid(), "5101", "مصروف الصيانة", "Maintenance Expense", null, 1,
            AccountClass.Expense, AccountType.Posting, NormalBalance.Debit,
            isPostingAccount: true, isControlAccount: false, allowManualPosting: true,
            isSystemAccount: false, isActive: true, effectiveDate: today);

        Assert.That(cashAccount.CanReceivePosting(), Is.True);
        Assert.That(bankAccount.CanReceivePosting(), Is.True);

        // 2. Open Fiscal Year and Fiscal Period
        var fiscalYear = FiscalYear.Create(
            Guid.NewGuid(), "FY2026", "2026",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            FiscalYearStatus.Open);

        var fiscalPeriod = FiscalPeriod.Create(
            Guid.NewGuid(), fiscalYear.Id, 1, "Period 01",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            FiscalPeriodStatus.Open,
            salesLocked: false, inventoryLocked: false, accountingLocked: false);

        Assert.That(fiscalYear.Status, Is.EqualTo(FiscalYearStatus.Open));
        Assert.That(fiscalPeriod.Status, Is.EqualTo(FiscalPeriodStatus.Open));

        // 3. Open Cash Shift
        var shift = CashShift.Create(
            Guid.NewGuid(), "CS-2026-000001", cashAccount.Id,
            adminUserId, now, openingBalance: 1000m,
            CashShiftStatus.Open);

        Assert.That(shift.Status, Is.EqualTo(CashShiftStatus.Open));
        Assert.That(shift.OpeningBalance, Is.EqualTo(1000m));

        // 4. Create & Post a Journal Entry (JV)
        var journal = JournalEntry.Create(
            Guid.NewGuid(), "JV-2026-000001", JournalType.Manual,
            today, today, fiscalPeriod.Id, "قيد تسوية إيراد",
            null, null, null,
            JournalEntryStatus.Draft, adminUserId, now);

        var debitLine = JournalEntryLine.Create(
            Guid.NewGuid(), journal.Id, 1, cashAccount.Id,
            500m, 0m, "قبض نقدي", null, null, null, null, null);

        var creditLine = JournalEntryLine.Create(
            Guid.NewGuid(), journal.Id, 2, salesRevenueAccount.Id,
            0m, 500m, "إيراد مبيعات", null, null, null, null, null);

        journal.AddLine(debitLine);
        journal.AddLine(creditLine);

        journal.SetPendingApproval();
        journal.Approve(adminUserId, now);
        journal.Post(adminUserId, now);
        Assert.That(journal.Status, Is.EqualTo(JournalEntryStatus.Posted));
        Assert.That(journal.Lines.Sum(x => x.DebitAmount), Is.EqualTo(journal.Lines.Sum(x => x.CreditAmount)));

        // 5. Create & Post a Receipt Voucher (RV)
        var rv = ReceiptVoucher.Create(
            Guid.NewGuid(), "RV-2026-000001", today,
            ReceiptPartyType.Customer, Guid.NewGuid(), "عميل نقدي",
            PaymentMethod.Cash, cashAccount.Id, null,
            1500m, ReceiptVoucherStatus.Draft, "سند قبض نقدي",
            null, adminUserId, now);

        var rvLine = ReceiptVoucherLine.Create(
            Guid.NewGuid(), rv.Id, 1, salesRevenueAccount.Id,
            1500m, null, null, "دفعة مبيعات");
        rv.AddLine(rvLine);

        rv.SetJournalEntry(journal.Id);
        rv.Approve();
        rv.Post(adminUserId, now);
        Assert.That(rv.Status, Is.EqualTo(ReceiptVoucherStatus.Posted));
        Assert.That(rv.JournalEntryId, Is.EqualTo(journal.Id));

        // 6. Create & Post a Payment Voucher (PV)
        var pv = PaymentVoucher.Create(
            Guid.NewGuid(), "PV-2026-000001", today,
            PaymentPartyType.Supplier, Guid.NewGuid(), "مؤسسة النور",
            PaymentMethod.BankTransfer, null, bankAccount.Id,
            800m, PaymentVoucherStatus.Draft, "سند صرف بنكي",
            null, adminUserId, now);

        var pvLine = PaymentVoucherLine.Create(
            Guid.NewGuid(), pv.Id, 1, maintenanceExpenseAccount.Id,
            800m, null, null, "مصروف صيانة معدات");
        pv.AddLine(pvLine);

        pv.SetJournalEntry(journal.Id);
        pv.Approve();
        pv.Post(adminUserId, now);
        Assert.That(pv.Status, Is.EqualTo(PaymentVoucherStatus.Posted));

        // 7. Create, Approve & Post an Expense
        var expense = Expense.Create(
            Guid.NewGuid(), "EXP-2026-000001", today,
            Guid.NewGuid(), maintenanceExpenseAccount.Id, "فني الصيانة",
            200m, PaymentMethod.Cash, cashAccount.Id, null,
            "إصلاح مكيفات", ExpenseStatus.Draft, null,
            adminUserId, now);

        expense.Approve();
        Assert.That(expense.Status, Is.EqualTo(ExpenseStatus.Approved));

        expense.SetJournalEntry(journal.Id);
        expense.Post(now);
        Assert.That(expense.Status, Is.EqualTo(ExpenseStatus.Posted));

        // 8. Close Cash Shift
        decimal expectedCash = 1000m + 1500m + 500m - 200m;
        shift.StartClosing(expectedClosingBalance: expectedCash);
        Assert.That(shift.Status, Is.EqualTo(CashShiftStatus.Closing));

        shift.Close(actualClosingBalance: expectedCash, closedBy: adminUserId, closedAtUtc: now);
        Assert.That(shift.Status, Is.EqualTo(CashShiftStatus.Closed));
        Assert.That(shift.DifferenceAmount, Is.EqualTo(0m));

        // 9. Close Fiscal Period and Lock Accounting
        fiscalPeriod.SetLocks(salesLocked: true, inventoryLocked: true, accountingLocked: true);
        Assert.That(fiscalPeriod.AccountingLocked, Is.True);

        fiscalPeriod.Close(adminUserId, now);
        Assert.That(fiscalPeriod.Status, Is.EqualTo(FiscalPeriodStatus.Closed));

        // 10. Reverse Journal Entry
        var reversalJournalId = Guid.NewGuid();
        journal.MarkReversed(reversalJournalId);
        Assert.That(journal.Status, Is.EqualTo(JournalEntryStatus.Reversed));
        Assert.That(journal.ReversedJournalId, Is.EqualTo(reversalJournalId));
    }
}
