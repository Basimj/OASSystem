using NUnit.Framework;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Accounts.Queries.GetAccountSaveCheck;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.CoreHardening.Services;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.Enums;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainAccountClass = OAS.Domain.Accounting.Enums.AccountClass;
using DomainAccountType = OAS.Domain.Accounting.Enums.AccountType;
using DomainNormalBalance = OAS.Domain.Accounting.Enums.NormalBalance;

namespace OAS.Tests.Accounting.Application.Accounts;

[TestFixture]
public sealed class AccountSaveCheckQueryTests
{
    private static Account MakeAccount(string code = "1101", bool system = false, Guid? parentId = null, byte level = 1)
    {
        var account = Account.Create(
            Guid.NewGuid(), code, "حساب تجريبي", null, parentId, level,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            true, false, true, system, true, null);
        typeof(Account).GetProperty(nameof(Account.RowVersion))!.SetValue(account, new byte[] { 1, 2, 3 });
        return account;
    }

    private static UpdateAccountRequest MakeRequest(Account account) => new(
        account.Code, account.NameAr, account.NameEn, account.ParentAccountId, account.Level,
        (AccountClass)account.AccountClass, (AccountType)account.AccountType, (NormalBalance)account.NormalBalance,
        account.IsPostingAccount, account.IsControlAccount, account.AllowManualPosting,
        account.IsSystemAccount, account.IsActive, account.EffectiveDate,
        Convert.ToBase64String(account.RowVersion));

    private static (GetAccountSaveCheckQueryHandler Handler, AccountIntegrityService Integrity)
        CreateHandler(Account account, bool managed = false,
            IEnumerable<Account>? extraAccounts = null,
            IEnumerable<JournalEntryLine>? lines = null,
            IEnumerable<PostingProfileLine>? mappings = null,
            IEnumerable<AccountingSettings>? settings = null)
    {
        var accounts = new FakeRepository<Account, Guid>(new[] { account }.Concat(extraAccounts ?? []));
        var integrity = new AccountIntegrityService(
            accounts,
            new FakeRepository<JournalEntryLine, Guid>(lines),
            new FakeRepository<PostingProfileLine, Guid>(mappings),
            new FakeRepository<Customer, Guid>(),
            new FakeRepository<Supplier, Guid>(),
            new FakeRepository<EmployeeAccount, Guid>(),
            new FakeRepository<CashAccount, Guid>(),
            new FakeRepository<BankAccount, Guid>(),
            new FakeRepository<ExpenseType, Guid>(),
            new FakeRepository<AccountingSettings, Guid>(settings));
        return (new GetAccountSaveCheckQueryHandler(accounts, new StubManagedAccountGuard(managed), integrity), integrity);
    }

    [Test]
    public async Task ValidUnmanagedAccountUpdatePasses()
    {
        var account = MakeAccount();
        var (handler, _) = CreateHandler(account);
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id, MakeRequest(account) with { NameAr = "اسم جديد" }), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.CanSave, Is.True);
            Assert.That(result.ReasonCode, Is.Null);
        });
    }

    [Test]
    public async Task ManagedAccountIsRejectedWithoutThrowing()
    {
        var account = MakeAccount();
        var (handler, _) = CreateHandler(account, managed: true);
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id, MakeRequest(account)), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.CanSave, Is.False);
            Assert.That(result.ReasonCode, Is.EqualTo("accounting_managed_account_edit_forbidden"));
        });
    }

    [Test]
    public async Task SystemAccountDeactivationIsRejectedWithoutThrowing()
    {
        var account = MakeAccount(system: true);
        var (handler, integrity) = CreateHandler(account);
        var request = MakeRequest(account) with { IsActive = false };
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id, request), CancellationToken.None);
        Assert.That(result.ReasonCode, Is.EqualTo("accounting_system_account_deactivation_forbidden"));
        // The server-side command validation deliberately STILL throws on an unsafe write.
        Assert.ThrowsAsync<ConflictException>(async () => await integrity.ValidateUpdateAsync(
            account, request.ParentAccountId, request.Level,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            request.IsPostingAccount, request.IsControlAccount, request.IsSystemAccount,
            request.IsActive));
    }

    [Test]
    public async Task ActiveChildBlocksParentDeactivationWithoutThrowing()
    {
        var parent = MakeAccount();
        var child = MakeAccount("11011", parentId: parent.Id, level: 2);
        var (handler, _) = CreateHandler(parent, extraAccounts: [child]);
        var result = await handler.Handle(new GetAccountSaveCheckQuery(parent.Id, MakeRequest(parent) with { IsActive = false }), CancellationToken.None);
        Assert.That(result.ReasonCode, Is.EqualTo("accounting_account_active_children_exist"));
    }

    [Test]
    public async Task PostingProfileMappingBlocksDeactivationAndPostingChange()
    {
        var account = MakeAccount();
        var mapping = PostingProfileLine.Create(Guid.NewGuid(), Guid.NewGuid(), "Receivables", account.Id, true);
        var (handler, _) = CreateHandler(account, mappings: [mapping]);
        var deactivate = await handler.Handle(new GetAccountSaveCheckQuery(account.Id, MakeRequest(account) with { IsActive = false }), CancellationToken.None);
        var structural = await handler.Handle(new GetAccountSaveCheckQuery(account.Id, MakeRequest(account) with { IsPostingAccount = false, AllowManualPosting = false }), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(deactivate.ReasonCode, Is.EqualTo("accounting_mapped_account_deactivation_forbidden"));
            Assert.That(structural.ReasonCode, Is.EqualTo("accounting_mapped_account_structure_immutable"));
        });
    }

    [Test]
    public async Task AccountingSettingsMappingBlocksDeactivation()
    {
        var account = MakeAccount();
        var settings = AccountingSettings.Create(Guid.NewGuid(), cashParentAccountId: account.Id);
        var (handler, _) = CreateHandler(account, settings: [settings]);
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id,
            MakeRequest(account) with { IsActive = false }), CancellationToken.None);
        Assert.That(result.ReasonCode, Is.EqualTo("accounting_mapped_account_deactivation_forbidden"));
    }

    [Test]
    public async Task SystemAccountStructureChangeIsRejected()
    {
        var account = MakeAccount(system: true);
        var (handler, _) = CreateHandler(account);
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id,
            MakeRequest(account) with { IsPostingAccount = false, AllowManualPosting = false }), CancellationToken.None);
        Assert.That(result.ReasonCode, Is.EqualTo("accounting_mapped_account_structure_immutable"));
    }

    [Test]
    public async Task MappedAccountStructuralChangeChecksTheProposedPostingAndControlFlags()
    {
        var account = MakeAccount();
        var mapping = PostingProfileLine.Create(Guid.NewGuid(), Guid.NewGuid(), "Receivables", account.Id, true);
        var (handler, integrity) = CreateHandler(account, mappings: [mapping]);
        var proposed = MakeRequest(account) with { IsControlAccount = true };
        var inspected = await handler.Handle(new GetAccountSaveCheckQuery(account.Id, proposed), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(inspected.CanSave, Is.False);
            Assert.That(inspected.ReasonCode, Is.EqualTo("accounting_mapped_account_structure_immutable"));
        });
        // It is still a write-time ConflictException (no weakening of the guard).
        Assert.ThrowsAsync<ConflictException>(async () => await integrity.ValidateUpdateAsync(
            account, proposed.ParentAccountId, proposed.Level,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            proposed.IsPostingAccount, proposed.IsControlAccount, proposed.IsSystemAccount,
            proposed.IsActive));
    }

    [Test]
    public async Task MappedAccountNameOnlyChangePassesInspection()
    {
        var account = MakeAccount();
        var mapping = PostingProfileLine.Create(Guid.NewGuid(), Guid.NewGuid(), "Receivables", account.Id, true);
        var (handler, _) = CreateHandler(account, mappings: [mapping]);
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id,
            MakeRequest(account) with { NameAr = "تسمية محدثة" }), CancellationToken.None);

        Assert.That(result.CanSave, Is.True);
    }

    [Test]
    public async Task SystemAccountTypeChangeIsRejectedWithoutThrowing()
    {
        var account = MakeAccount(system: true);
        var (handler, _) = CreateHandler(account);
        var proposed = MakeRequest(account) with { AccountType = AccountType.Control, IsControlAccount = true };
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id, proposed), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.CanSave, Is.False);
            Assert.That(result.ReasonCode, Is.EqualTo("accounting_mapped_account_structure_immutable"));
        });
    }

    [Test]
    public async Task JournalHistoryBlocksTurningOffPosting()
    {
        var account = MakeAccount();
        var line = JournalEntryLine.Create(Guid.NewGuid(), Guid.NewGuid(), 1, account.Id,
            100m, 0m, null, null, null, null, null, null);
        var (handler, _) = CreateHandler(account, lines: [line]);
        var request = MakeRequest(account) with { IsPostingAccount = false, AllowManualPosting = false };
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id, request), CancellationToken.None);
        Assert.That(result.ReasonCode, Is.EqualTo("accounting_account_history_structure_immutable"));
    }

    [Test]
    public async Task ManualPostingOffOnlyIsAllowedWhenOtherSettingsUnchanged()
    {
        var account = MakeAccount();
        var (handler, _) = CreateHandler(account);
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id,
            MakeRequest(account) with { AllowManualPosting = false }), CancellationToken.None);
        Assert.That(result.CanSave, Is.True);
    }

    [Test]
    public async Task InconsistentManualPostingIsRejected()
    {
        var account = MakeAccount();
        var (handler, _) = CreateHandler(account);
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id,
            MakeRequest(account) with { IsPostingAccount = false, AllowManualPosting = true }), CancellationToken.None);
        Assert.That(result.ReasonCode, Is.EqualTo("accounting_account_manual_without_posting_invalid"));
    }

    [Test]
    public async Task InvalidParentAndChangedAccountCodeAreRejected()
    {
        var account = MakeAccount();
        var (handler, _) = CreateHandler(account);
        var code = await handler.Handle(new GetAccountSaveCheckQuery(account.Id,
            MakeRequest(account) with { Code = "9999" }), CancellationToken.None);
        var parent = await handler.Handle(new GetAccountSaveCheckQuery(account.Id,
            MakeRequest(account) with { ParentAccountId = account.Id, Level = 2 }), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(code.ReasonCode, Is.EqualTo("accounting_account_code_immutable"));
            Assert.That(parent.ReasonCode, Is.EqualTo("accounting_account_self_parent"));
        });
    }

    [Test]
    public async Task StaleVersionIsRejectedBeforeUpdate()
    {
        var account = MakeAccount();
        var (handler, _) = CreateHandler(account);
        var result = await handler.Handle(new GetAccountSaveCheckQuery(account.Id,
            MakeRequest(account) with { RowVersion = Convert.ToBase64String(new byte[] { 7 }) }), CancellationToken.None);
        Assert.That(result.ReasonCode, Is.EqualTo("accounting_account_stale_row_version"));
    }

    [Test]
    public void CheckRequiresAccountsEditPermission()
    {
        var account = MakeAccount();
        var query = new GetAccountSaveCheckQuery(account.Id, MakeRequest(account));
        Assert.That(query.RequiredPermissions, Does.Contain(AccountingPermissions.Accounts.Edit));
    }

    private sealed class StubManagedAccountGuard(bool managed) : IManagedAccountGuard
    {
        public Task<bool> IsManagedAsync(Guid accountId, CancellationToken cancellationToken = default)
            => Task.FromResult(managed);
    }
}
