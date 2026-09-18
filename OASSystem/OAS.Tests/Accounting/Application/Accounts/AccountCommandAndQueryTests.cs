using NUnit.Framework;
using OAS.Application.Accounting.Accounts.Commands.CreateAccount;
using OAS.Application.Accounting.Accounts.Commands.SetAccountStatus;
using OAS.Application.Accounting.Accounts.Commands.UpdateAccount;
using OAS.Application.Accounting.Accounts.Mapping;
using OAS.Application.Accounting.Accounts.Queries.GetAccountById;
using OAS.Application.Accounting.Accounts.Queries.GetAccounts;
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

        var command = new CreateAccountCommand(request);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo("1101"));
        Assert.That(_repository.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateAccountCommandHandler_UpdatesExistingAccount()
    {
        var existing = Account.Create(
            Guid.NewGuid(), "1101", "الصندوق", null, null, 1,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            true, false, true, false, true, null);
        await _repository.AddAsync(existing);

        var handler = new UpdateAccountCommandHandler(_repository, _mapper);
        var updateRequest = new UpdateAccountRequest(
            Code: "1101-A",
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

        var command = new UpdateAccountCommand(existing.Id, updateRequest);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result.Code, Is.EqualTo("1101-A"));
        Assert.That(result.NameAr, Is.EqualTo("الصندوق الرئيسي المعدل"));
    }

    [Test]
    public async Task SetAccountStatusCommandHandler_TogglesActiveState()
    {
        var existing = Account.Create(
            Guid.NewGuid(), "1101", "الصندوق", null, null, 1,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            true, false, true, false, true, null);
        await _repository.AddAsync(existing);

        var handler = new SetAccountStatusCommandHandler(_repository);
        var setStatusRequest = new SetAccountStatusRequest(IsActive: false, RowVersion: Convert.ToBase64String(existing.RowVersion));
        var command = new SetAccountStatusCommand(existing.Id, setStatusRequest);
        await handler.Handle(command, CancellationToken.None);

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
        var query = new GetAccountByIdQuery(existing.Id);
        var dto = await handler.Handle(query, CancellationToken.None);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Id, Is.EqualTo(existing.Id));
        Assert.That(dto.Code, Is.EqualTo("1101"));
        Assert.That(dto.NameAr, Is.EqualTo("الصندوق الرئيسي"));
    }

    [Test]
    public async Task GetAccountsQueryHandler_ReturnsPagedResult()
    {
        var account1 = Account.Create(
            Guid.NewGuid(), "1101", "الصندوق الرئيسي", null, null, 1,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            true, false, true, false, true, null);
        var account2 = Account.Create(
            Guid.NewGuid(), "1102", "الصندوق الفرعي", null, null, 1,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            true, false, true, false, true, null);

        await _repository.AddRangeAsync([account1, account2]);

        var handler = new GetAccountsQueryHandler(_repository, _mapper);
        var query = new GetAccountsQuery(new PageRequest { PageNumber = 1, PageSize = 10 });
        var pagedResult = await handler.Handle(query, CancellationToken.None);

        Assert.That(pagedResult, Is.Not.Null);
        Assert.That(pagedResult.TotalCount, Is.EqualTo(2));
        Assert.That(pagedResult.Items.Count, Is.EqualTo(2));
    }
}
