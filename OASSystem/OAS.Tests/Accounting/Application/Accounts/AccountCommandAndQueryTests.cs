using NUnit.Framework;
using OAS.Application.Accounting.Accounts.Commands.CreateAccount;
using OAS.Application.Accounting.Accounts.Commands.SetAccountStatus;
using OAS.Application.Accounting.Accounts.Commands.UpdateAccount;
using OAS.Application.Accounting.Accounts.Mapping;
using OAS.Application.Accounting.Accounts.Queries.GetAccountById;
using OAS.Application.Accounting.Accounts.Queries.GetAccounts;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainAccountClass = OAS.Domain.Accounting.Enums.AccountClass;
using DomainAccountType = OAS.Domain.Accounting.Enums.AccountType;
using DomainNormalBalance = OAS.Domain.Accounting.Enums.NormalBalance;

namespace OAS.Tests.Accounting.Application.Accounts;

[TestFixture]
public class AccountCommandAndQueryTests
{
    private FakeRepository<Account, Guid> _repository = null!;
    private AccountMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        _repository = new FakeRepository<Account, Guid>();
        _mapper = new AccountMapper();
    }

    [Test]
    public async Task CreateAccountCommandHandler_AddsAccountToRepository()
    {
        var handler = new CreateAccountCommandHandler(_repository, _mapper);
        var request = new CreateAccountRequest(
            Code: "1101",
            NameAr: "الصندوق الرئيسي",
            NameEn: "Main Cash",
            ParentAccountId: null,
            Level: 1,
            AccountClass: AccountClass.Asset,
            AccountType: AccountType.Posting,
            NormalBalance: NormalBalance.Debit,
            IsPostingAccount: true,
            IsControlAccount: false,
            AllowManualPosting: true,
            IsSystemAccount: false,
            IsActive: true,
            EffectiveDate: null);

        var result = await handler.Handle(
            new CreateAccountCommand(request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Code, Is.EqualTo("1101"));
            Assert.That(result.NameAr, Is.EqualTo("الصندوق الرئيسي"));
            Assert.That(_repository.Items.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task UpdateAccountCommandHandler_UpdatesExistingAccountWithoutChangingCode()
    {
        var existing = CreateAccountEntity("1101", "الصندوق");
        await _repository.AddAsync(existing);

        var handler = new UpdateAccountCommandHandler(_repository, _mapper);
        var request = new UpdateAccountRequest(
            Code: "1101",
            NameAr: "الصندوق الرئيسي المعدل",
            NameEn: "Updated Main Cash",
            ParentAccountId: null,
            Level: 1,
            AccountClass: AccountClass.Asset,
            AccountType: AccountType.Posting,
            NormalBalance: NormalBalance.Debit,
            IsPostingAccount: true,
            IsControlAccount: false,
            AllowManualPosting: true,
            IsSystemAccount: false,
            IsActive: true,
            EffectiveDate: null,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var result = await handler.Handle(
            new UpdateAccountCommand(existing.Id, request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("1101"));
            Assert.That(result.NameAr, Is.EqualTo("الصندوق الرئيسي المعدل"));
            Assert.That(result.NameEn, Is.EqualTo("Updated Main Cash"));
            Assert.That(_repository.Items.Count, Is.EqualTo(1));
            Assert.That(_repository.LastUpdated, Is.SameAs(existing));
        });
    }

    [Test]
    public void UpdateAccountCommandHandler_ChangingCode_ThrowsConflictException()
    {
        var existing = CreateAccountEntity("1101", "الصندوق");
        _repository.AddAsync(existing).GetAwaiter().GetResult();

        var handler = new UpdateAccountCommandHandler(_repository, _mapper);
        var request = new UpdateAccountRequest(
            Code: "9999",
            NameAr: "الصندوق",
            NameEn: null,
            ParentAccountId: null,
            Level: 1,
            AccountClass: AccountClass.Asset,
            AccountType: AccountType.Posting,
            NormalBalance: NormalBalance.Debit,
            IsPostingAccount: true,
            IsControlAccount: false,
            AllowManualPosting: true,
            IsSystemAccount: false,
            IsActive: true,
            EffectiveDate: null,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new UpdateAccountCommand(existing.Id, request),
                CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("accounting_account_code_immutable"));
        Assert.That(existing.Code, Is.EqualTo("1101"));
    }

    [Test]
    public async Task SetAccountStatusCommandHandler_TogglesActiveState()
    {
        var existing = CreateAccountEntity("1101", "الصندوق");
        await _repository.AddAsync(existing);

        var handler = new SetAccountStatusCommandHandler(_repository);
        var request = new SetAccountStatusRequest(
            IsActive: false,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        await handler.Handle(
            new SetAccountStatusCommand(existing.Id, request),
            CancellationToken.None);

        Assert.That(existing.IsActive, Is.False);
    }

    [Test]
    public async Task GetAccountByIdQueryHandler_ReturnsMappedDto()
    {
        var existing = Account.Create(
            Guid.NewGuid(), "1101", "الصندوق الرئيسي", "Main Cash", null, 1,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            true, false, true, false, true, null);
        await _repository.AddAsync(existing);

        var handler = new GetAccountByIdQueryHandler(_repository, _mapper);
        var dto = await handler.Handle(
            new GetAccountByIdQuery(existing.Id),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto.Id, Is.EqualTo(existing.Id));
            Assert.That(dto.Code, Is.EqualTo("1101"));
            Assert.That(dto.NameAr, Is.EqualTo("الصندوق الرئيسي"));
        });
    }

    [Test]
    public async Task GetAccountsQueryHandler_ReturnsPagedResult()
    {
        var account1 = CreateAccountEntity("1101", "الصندوق الرئيسي");
        var account2 = CreateAccountEntity("1102", "الصندوق الفرعي");
        await _repository.AddRangeAsync([account1, account2]);

        var handler = new GetAccountsQueryHandler(_repository, _mapper);
        var pagedResult = await handler.Handle(
            new GetAccountsQuery(new PageRequest { PageNumber = 1, PageSize = 10 }),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(pagedResult, Is.Not.Null);
            Assert.That(pagedResult.TotalCount, Is.EqualTo(2));
            Assert.That(pagedResult.Items.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetAccountsQueryHandler_SearchesByCodeOrName()
    {
        await _repository.AddRangeAsync([
            CreateAccountEntity("1101", "الصندوق الرئيسي"),
            CreateAccountEntity("4100", "إيرادات المبيعات")
        ]);

        var handler = new GetAccountsQueryHandler(_repository, _mapper);
        var result = await handler.Handle(
            new GetAccountsQuery(new PageRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Search = "4100"
            }),
            CancellationToken.None);

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].Code, Is.EqualTo("4100"));
    }

    private static Account CreateAccountEntity(string code, string name) =>
        Account.Create(
            Guid.NewGuid(), code, name, null, null, 1,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            true, false, true, false, true, null);
}
