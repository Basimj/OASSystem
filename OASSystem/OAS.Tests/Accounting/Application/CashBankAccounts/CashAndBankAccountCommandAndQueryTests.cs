using NUnit.Framework;
using OAS.Application.Accounting.BankAccounts.Commands.CreateBankAccount;
using OAS.Application.Accounting.BankAccounts.Commands.UpdateBankAccount;
using OAS.Application.Accounting.BankAccounts.Mapping;
using OAS.Application.Accounting.BankAccounts.Queries.GetBankAccountById;
using OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;
using OAS.Application.Accounting.CashAccounts.Commands.UpdateCashAccount;
using OAS.Application.Accounting.CashAccounts.Mapping;
using OAS.Application.Accounting.CashAccounts.Queries.GetCashAccountById;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.CashBankAccounts;

[TestFixture]
public class CashAndBankAccountCommandAndQueryTests
{
    private FakeRepository<CashAccount, Guid> _cashRepository = null!;
    private FakeRepository<BankAccount, Guid> _bankRepository = null!;
    private CashAccountMapper _cashMapper = null!;
    private BankAccountMapper _bankMapper = null!;
    private Guid _glAccountId;

    [SetUp]
    public void Setup()
    {
        _cashRepository = new FakeRepository<CashAccount, Guid>();
        _bankRepository = new FakeRepository<BankAccount, Guid>();
        _cashMapper = new CashAccountMapper();
        _bankMapper = new BankAccountMapper();
        _glAccountId = Guid.NewGuid();
    }

    [Test]
    public async Task CreateCashAccountCommandHandler_CreatesCashAccount()
    {
        var handler = new CreateCashAccountCommandHandler(_cashRepository, _cashMapper);
        var request = new CreateCashAccountRequest(
            Code: "CASH-01",
            Name: "الصندوق الرئيسي",
            AccountId: _glAccountId,
            IsDefault: true,
            IsActive: true);

        var result = await handler.Handle(
            new CreateCashAccountCommand(request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("CASH-01"));
            Assert.That(result.Name, Is.EqualTo("الصندوق الرئيسي"));
            Assert.That(result.IsDefault, Is.True);
            Assert.That(_cashRepository.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task UpdateCashAccountCommandHandler_UpdatesDetailsWithoutChangingCode()
    {
        var existing = CashAccount.Create(
            Guid.NewGuid(), "CASH-01", "الصندوق", _glAccountId, false, true);
        await _cashRepository.AddAsync(existing);

        var handler = new UpdateCashAccountCommandHandler(_cashRepository, _cashMapper);
        var request = new UpdateCashAccountRequest(
            Code: "CASH-01",
            Name: "الصندوق الرئيسي المعدل",
            AccountId: _glAccountId,
            IsDefault: true,
            IsActive: true,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var result = await handler.Handle(
            new UpdateCashAccountCommand(existing.Id, request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("CASH-01"));
            Assert.That(result.Name, Is.EqualTo("الصندوق الرئيسي المعدل"));
            Assert.That(result.IsDefault, Is.True);
            Assert.That(_cashRepository.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void UpdateCashAccountCommandHandler_ChangingCode_ThrowsConflictException()
    {
        var existing = CashAccount.Create(
            Guid.NewGuid(), "CASH-01", "الصندوق", _glAccountId, false, true);
        _cashRepository.AddAsync(existing).GetAwaiter().GetResult();

        var handler = new UpdateCashAccountCommandHandler(_cashRepository, _cashMapper);
        var request = new UpdateCashAccountRequest(
            Code: "CASH-99",
            Name: "الصندوق",
            AccountId: _glAccountId,
            IsDefault: false,
            IsActive: true,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new UpdateCashAccountCommand(existing.Id, request),
                CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("accounting_cash_account_code_immutable"));
        Assert.That(existing.Code, Is.EqualTo("CASH-01"));
    }

    [Test]
    public async Task GetCashAccountByIdQueryHandler_ReturnsMappedDto()
    {
        var existing = CashAccount.Create(
            Guid.NewGuid(), "CASH-01", "الصندوق الرئيسي", _glAccountId, true, true);
        await _cashRepository.AddAsync(existing);

        var handler = new GetCashAccountByIdQueryHandler(_cashRepository, _cashMapper);
        var dto = await handler.Handle(
            new GetCashAccountByIdQuery(existing.Id),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(existing.Id));
            Assert.That(dto.Code, Is.EqualTo("CASH-01"));
            Assert.That(dto.AccountId, Is.EqualTo(_glAccountId));
        });
    }

    [Test]
    public async Task CreateBankAccountCommandHandler_CreatesBankAccount()
    {
        var handler = new CreateBankAccountCommandHandler(_bankRepository, _bankMapper);
        var request = new CreateBankAccountRequest(
            Code: "BANK-01",
            BankName: "البنك الرئيسي",
            AccountName: "الحساب الجاري",
            AccountNumber: "123456",
            IBAN: "YE00TEST123456",
            AccountId: _glAccountId,
            IsActive: true);

        var result = await handler.Handle(
            new CreateBankAccountCommand(request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("BANK-01"));
            Assert.That(result.BankName, Is.EqualTo("البنك الرئيسي"));
            Assert.That(result.AccountNumber, Is.EqualTo("123456"));
            Assert.That(_bankRepository.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task UpdateBankAccountCommandHandler_UpdatesDetailsWithoutChangingCodeOrAccountNumber()
    {
        var existing = BankAccount.Create(
            Guid.NewGuid(), "BANK-01", "البنك", "الجاري", "123456", null, _glAccountId, true);
        await _bankRepository.AddAsync(existing);

        var handler = new UpdateBankAccountCommandHandler(_bankRepository, _bankMapper);
        var request = new UpdateBankAccountRequest(
            Code: "BANK-01",
            BankName: "البنك الرئيسي المعدل",
            AccountName: "الحساب الجاري الرئيسي",
            AccountNumber: "123456",
            IBAN: "YE00NEWIBAN",
            AccountId: _glAccountId,
            IsActive: true,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var result = await handler.Handle(
            new UpdateBankAccountCommand(existing.Id, request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("BANK-01"));
            Assert.That(result.AccountNumber, Is.EqualTo("123456"));
            Assert.That(result.BankName, Is.EqualTo("البنك الرئيسي المعدل"));
            Assert.That(result.AccountName, Is.EqualTo("الحساب الجاري الرئيسي"));
            Assert.That(result.IBAN, Is.EqualTo("YE00NEWIBAN"));
            Assert.That(_bankRepository.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void UpdateBankAccountCommandHandler_ChangingCode_ThrowsConflictException()
    {
        var existing = BankAccount.Create(
            Guid.NewGuid(), "BANK-01", "البنك", "الجاري", "123456", null, _glAccountId, true);
        _bankRepository.AddAsync(existing).GetAwaiter().GetResult();

        var handler = new UpdateBankAccountCommandHandler(_bankRepository, _bankMapper);
        var request = new UpdateBankAccountRequest(
            Code: "BANK-99",
            BankName: existing.BankName,
            AccountName: existing.AccountName,
            AccountNumber: existing.AccountNumber,
            IBAN: existing.IBAN,
            AccountId: existing.AccountId,
            IsActive: existing.IsActive,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new UpdateBankAccountCommand(existing.Id, request),
                CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("accounting_bank_account_code_immutable"));
        Assert.That(existing.Code, Is.EqualTo("BANK-01"));
    }

    [Test]
    public void UpdateBankAccountCommandHandler_ChangingAccountNumber_ThrowsConflictException()
    {
        var existing = BankAccount.Create(
            Guid.NewGuid(), "BANK-01", "البنك", "الجاري", "123456", null, _glAccountId, true);
        _bankRepository.AddAsync(existing).GetAwaiter().GetResult();

        var handler = new UpdateBankAccountCommandHandler(_bankRepository, _bankMapper);
        var request = new UpdateBankAccountRequest(
            Code: existing.Code,
            BankName: existing.BankName,
            AccountName: existing.AccountName,
            AccountNumber: "999999",
            IBAN: existing.IBAN,
            AccountId: existing.AccountId,
            IsActive: existing.IsActive,
            RowVersion: Convert.ToBase64String(existing.RowVersion));

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new UpdateBankAccountCommand(existing.Id, request),
                CancellationToken.None));

        Assert.That(ex!.Code, Is.EqualTo("accounting_bank_account_number_immutable"));
        Assert.That(existing.AccountNumber, Is.EqualTo("123456"));
    }

    [Test]
    public async Task GetBankAccountByIdQueryHandler_ReturnsMappedDto()
    {
        var existing = BankAccount.Create(
            Guid.NewGuid(), "BANK-01", "البنك الرئيسي", "الجاري", "123456", "YE00TEST", _glAccountId, true);
        await _bankRepository.AddAsync(existing);

        var handler = new GetBankAccountByIdQueryHandler(_bankRepository, _bankMapper);
        var dto = await handler.Handle(
            new GetBankAccountByIdQuery(existing.Id),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(existing.Id));
            Assert.That(dto.Code, Is.EqualTo("BANK-01"));
            Assert.That(dto.AccountNumber, Is.EqualTo("123456"));
            Assert.That(dto.AccountId, Is.EqualTo(_glAccountId));
        });
    }
}
