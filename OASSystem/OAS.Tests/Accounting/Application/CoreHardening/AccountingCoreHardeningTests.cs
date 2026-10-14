using NUnit.Framework;
using OAS.Application.Accounting.CoreHardening.Services;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.ValueObjects;
using OAS.Domain.Features.Employees.Entities;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.CoreHardening;

[TestFixture]
public class AccountingCoreHardeningTests
{
    [Test]
    public void Account_Create_SelfParent_IsRejectedByDomain()
    {
        var id = Guid.NewGuid();
        Assert.Throws<OAS.Domain.Exceptions.DomainException>(() =>
            Account.Create(
                id, "1000", "حساب", null, id, 2,
                AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
                true, false, true, false, true, null));
    }

    [Test]
    public void Account_Create_HeaderConfiguredForPosting_IsRejectedByDomain()
    {
        Assert.Throws<OAS.Domain.Exceptions.DomainException>(() =>
            Account.Create(
                Guid.NewGuid(), "1000", "رئيسي", null, null, 1,
                AccountClass.Asset, AccountType.Header, NormalBalance.Debit,
                true, false, false, false, true, null));
    }

    [Test]
    public async Task AccountIntegrity_ParentCycle_IsRejected()
    {
        var cId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var aId = Guid.NewGuid();

        var c = CreatePosting(cId, "100", null, 1);
        var b = CreatePosting(bId, "110", cId, 2);
        var a = CreatePosting(aId, "111", bId, 3);
        var accounts = new FakeRepository<Account, Guid>([a, b, c]);
        var service = BuildAccountIntegrity(accounts);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await service.ValidateHierarchyAsync(c.Id, a.Id, 4));

        Assert.That(ex!.Code, Is.EqualTo("accounting_account_parent_cycle"));
    }

    [Test]
    public async Task AccountIntegrity_ChangingNormalBalanceAfterJournalHistory_IsRejected()
    {
        var account = CreatePosting(Guid.NewGuid(), "1101", null, 1);
        var accounts = new FakeRepository<Account, Guid>([account]);
        var journalLines = new FakeRepository<JournalEntryLine, Guid>([
            JournalEntryLine.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1, account.Id, 100m, 0m,
                null, null, null, null, null, null)
        ]);
        var service = BuildAccountIntegrity(accounts, journalLines: journalLines);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await service.ValidateUpdateAsync(
                account,
                account.ParentAccountId,
                account.Level,
                account.AccountClass,
                account.AccountType,
                NormalBalance.Credit,
                account.IsPostingAccount,
                account.IsControlAccount,
                account.IsSystemAccount,
                account.IsActive));

        Assert.That(ex!.Code, Is.EqualTo("accounting_account_history_structure_immutable"));
    }

    [Test]
    public async Task AccountIntegrity_DisablingPostingProfileAccount_IsRejected()
    {
        var account = CreatePosting(Guid.NewGuid(), "1101", null, 1);
        var accounts = new FakeRepository<Account, Guid>([account]);
        var mapped = PostingProfileLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Inventory", account.Id, true);
        var service = BuildAccountIntegrity(
            accounts,
            postingProfileLines: new FakeRepository<PostingProfileLine, Guid>([mapped]));

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await service.ValidateDeactivationAsync(account));

        Assert.That(ex!.Code, Is.EqualTo("accounting_mapped_account_deactivation_forbidden"));
    }

    [Test]
    public async Task JournalSubledger_WrongCustomerAccount_IsRejected()
    {
        var customerAccount = CreateSubledger(Guid.NewGuid(), "11020001", AccountClass.Asset, NormalBalance.Debit);
        var wrongAccount = CreateSubledger(Guid.NewGuid(), "11020002", AccountClass.Asset, NormalBalance.Debit);
        var customer = CreateCustomer(customerAccount.Id);
        var service = new JournalSubledgerIntegrityService(
            new FakeRepository<Customer, Guid>([customer]),
            new FakeRepository<Supplier, Guid>(),
            new FakeRepository<Employee, Guid>(),
            new FakeRepository<EmployeeAccount, Guid>());

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await service.ValidateAndResolvePartyNameAsync(
                wrongAccount, customer.Id, null, null));

        Assert.That(ex!.Code, Is.EqualTo("journal_customer_account_mismatch"));
    }

    [Test]
    public async Task JournalSubledger_WrongSupplierAccount_IsRejected()
    {
        var supplierAccount = CreateSubledger(Guid.NewGuid(), "21020001", AccountClass.Liability, NormalBalance.Credit);
        var wrongAccount = CreateSubledger(Guid.NewGuid(), "21020002", AccountClass.Liability, NormalBalance.Credit);
        var supplier = CreateSupplier(supplierAccount.Id);
        var service = new JournalSubledgerIntegrityService(
            new FakeRepository<Customer, Guid>(),
            new FakeRepository<Supplier, Guid>([supplier]),
            new FakeRepository<Employee, Guid>(),
            new FakeRepository<EmployeeAccount, Guid>());

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await service.ValidateAndResolvePartyNameAsync(
                wrongAccount, null, supplier.Id, null));

        Assert.That(ex!.Code, Is.EqualTo("journal_supplier_account_mismatch"));
    }

    [Test]
    public async Task JournalSource_DuplicateSource_IsRejectedBeforeSave()
    {
        var sourceId = Guid.NewGuid();
        var existing = JournalEntry.Create(
            Guid.NewGuid(), "JV-2026-000001", JournalType.Automatic,
            new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 13), Guid.NewGuid(),
            "Source posting", "Sales", "SalesInvoice", sourceId, JournalEntryStatus.Posted);
        var journals = new FakeRepository<JournalEntry, Guid>([existing]);
        var service = new JournalSourceIntegrityService(journals);

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await service.ValidateUniqueSourceAsync("Sales", "SalesInvoice", sourceId));

        Assert.That(ex!.Code, Is.EqualTo("accounting_source_already_posted"));
    }

    [Test]
    public async Task JournalSource_PartialSourceIdentity_IsRejected()
    {
        var service = new JournalSourceIntegrityService(new FakeRepository<JournalEntry, Guid>());

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await service.ValidateUniqueSourceAsync("Sales", null, Guid.NewGuid()));

        Assert.That(ex!.Code, Is.EqualTo("accounting_journal_source_incomplete"));
    }

    [Test]
    public async Task FiscalPeriod_OutsideFiscalYear_IsRejected()
    {
        var yearId = Guid.NewGuid();
        var year = FiscalYear.Create(
            yearId, "2026", "2026",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            FiscalYearStatus.Open);
        var service = new FiscalPeriodIntegrityService(
            new FakeRepository<FiscalYear, Guid>([year]),
            new FakeRepository<FiscalPeriod, Guid>(),
            new FakeRepository<JournalEntry, Guid>());

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await service.ValidateAsync(
                yearId, 1, new DateOnly(2025, 12, 1), new DateOnly(2026, 1, 31)));

        Assert.That(ex!.Code, Is.EqualTo("fiscal_period_outside_year"));
    }

    [Test]
    public async Task FiscalPeriod_Overlap_IsRejected()
    {
        var yearId = Guid.NewGuid();
        var year = FiscalYear.Create(
            yearId, "2026", "2026",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            FiscalYearStatus.Open);
        var existing = FiscalPeriod.Create(
            Guid.NewGuid(), yearId, 1, "January",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            FiscalPeriodStatus.Open, false, false, false);
        var service = new FiscalPeriodIntegrityService(
            new FakeRepository<FiscalYear, Guid>([year]),
            new FakeRepository<FiscalPeriod, Guid>([existing]),
            new FakeRepository<JournalEntry, Guid>());

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await service.ValidateAsync(
                yearId, 2, new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 15)));

        Assert.That(ex!.Code, Is.EqualTo("fiscal_period_overlap"));
    }

    [Test]
    public void Journal_ReversalPreservesOriginalPostedStatus()
    {
        var journal = JournalEntry.Create(
            Guid.NewGuid(), "JV-2026-000002", JournalType.Manual,
            new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 13), Guid.NewGuid(),
            "Manual journal", null, null, null, JournalEntryStatus.Draft);
        journal.AddLine(JournalEntryLine.Create(
            Guid.NewGuid(), journal.Id, 1, Guid.NewGuid(), 100m, 0m,
            null, null, null, null, null, null));
        journal.AddLine(JournalEntryLine.Create(
            Guid.NewGuid(), journal.Id, 2, Guid.NewGuid(), 0m, 100m,
            null, null, null, null, null, null));
        journal.SetPendingApproval();
        journal.Approve(Guid.NewGuid(), DateTime.UtcNow);
        journal.Post(Guid.NewGuid(), DateTime.UtcNow);

        var reversalId = Guid.NewGuid();
        journal.MarkReversed(reversalId);

        Assert.Multiple(() =>
        {
            Assert.That(journal.Status, Is.EqualTo(JournalEntryStatus.Posted));
            Assert.That(journal.ReversedJournalId, Is.EqualTo(reversalId));
        });
    }

    private static AccountIntegrityService BuildAccountIntegrity(
        FakeRepository<Account, Guid> accounts,
        FakeRepository<JournalEntryLine, Guid>? journalLines = null,
        FakeRepository<PostingProfileLine, Guid>? postingProfileLines = null) =>
        new(
            accounts,
            journalLines ?? new FakeRepository<JournalEntryLine, Guid>(),
            postingProfileLines ?? new FakeRepository<PostingProfileLine, Guid>(),
            new FakeRepository<Customer, Guid>(),
            new FakeRepository<Supplier, Guid>(),
            new FakeRepository<EmployeeAccount, Guid>(),
            new FakeRepository<CashAccount, Guid>(),
            new FakeRepository<BankAccount, Guid>(),
            new FakeRepository<ExpenseType, Guid>(),
            new FakeRepository<AccountingSettings, Guid>());

    private static Account CreatePosting(Guid id, string code, Guid? parentId, byte level) =>
        Account.Create(
            id, code, code, null, parentId, level,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            true, false, true, false, true, null);

    private static Account CreateSubledger(Guid id, string code, AccountClass accountClass, NormalBalance normalBalance) =>
        Account.Create(
            id, code, code, null, null, 1,
            accountClass, AccountType.Subledger, normalBalance,
            true, false, false, false, true, null);

    private static Customer CreateCustomer(Guid accountId) =>
        Customer.Create(
            Guid.NewGuid(), "C0001", accountId, PartyEntityType.Individual, "عميل", null,
            null, null, null, null, null, Gender.Unspecified, PartyContactInfo.Empty,
            true, 10000m, 30, null, true, null);

    private static Supplier CreateSupplier(Guid accountId) =>
        Supplier.Create(
            Guid.NewGuid(), "S0001", accountId, PartyEntityType.Organization, SupplierScope.Local,
            "مورد", null, null, null, null, null, PartyContactInfo.Empty,
            10000m, 30, null, null, true, null);
}
