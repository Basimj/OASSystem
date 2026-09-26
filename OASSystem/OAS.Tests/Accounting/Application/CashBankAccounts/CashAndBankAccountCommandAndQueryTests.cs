using NUnit.Framework;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.BankAccounts.Commands.CreateBankAccount;
using OAS.Application.Accounting.BankAccounts.Commands.ReserveBankAccountCode;
using OAS.Application.Accounting.BankAccounts.Commands.UpdateBankAccount;
using OAS.Application.Accounting.BankAccounts.Mapping;
using OAS.Application.Accounting.BankAccounts.Queries.GetBankAccountById;
using OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;
using OAS.Application.Accounting.CashAccounts.Commands.ReserveCashAccountCode;
using OAS.Application.Accounting.CashAccounts.Commands.UpdateCashAccount;
using OAS.Application.Accounting.CashAccounts.Mapping;
using OAS.Application.Accounting.CashAccounts.Queries.GetCashAccountById;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.CashBankAccounts;

[TestFixture]
public class CashAndBankAccountCommandAndQueryTests
{
    private FakeRepository<CashAccount, Guid> _cashRepository = null!;
    private FakeRepository<BankAccount, Guid> _bankRepository = null!;
    private FakeRepository<Currency, Guid> _currencyRepository = null!;
    private CashAccountMapper _cashMapper = null!;
    private BankAccountMapper _bankMapper = null!;
    private FakeLinkedAccountingAccountProvisioningService _linkedAccounts = null!;
    private ISequenceNumberGenerator _sequences = null!;
    private Guid _currencyId;
    private Guid _glAccountId;

    [SetUp]
    public async Task Setup()
    {
        _cashRepository = new FakeRepository<CashAccount, Guid>();
        _bankRepository = new FakeRepository<BankAccount, Guid>();
        _currencyRepository = new FakeRepository<Currency, Guid>();
        _cashMapper = new CashAccountMapper();
        _bankMapper = new BankAccountMapper();
        _sequences = new FakeSequenceNumberGenerator();
        _currencyId = Guid.NewGuid();
        _glAccountId = Guid.NewGuid();

        await _currencyRepository.AddAsync(
            Currency.Create(
                _currencyId,
                "YER",
                "الريال اليمني",
                "Yemeni Rial",
                "ر.ي",
                2,
                true));

        _linkedAccounts =
            new FakeLinkedAccountingAccountProvisioningService(_glAccountId);
    }

    [Test]
    public async Task ReserveCashAccountCodeCommandHandler_ReturnsAutomaticCode()
    {
        var handler = new ReserveCashAccountCodeCommandHandler(
            _sequences,
            _cashRepository);

        var result = await handler.Handle(
            new ReserveCashAccountCodeCommand(),
            CancellationToken.None);

        Assert.That(result.CashAccountCode, Is.EqualTo("CASH-000001"));
    }

    [Test]
    public async Task CreateCashAccountCommandHandler_GeneratesCodeAndCreatesLinkedAccount()
    {
        var handler = new CreateCashAccountCommandHandler(
            _cashRepository,
            _currencyRepository,
            _linkedAccounts,
            _sequences);

        var request = new CreateCashAccountRequest(
            null,
            "الصندوق الرئيسي",
            _currencyId,
            true,
            true);

        var result = await handler.Handle(
            new CreateCashAccountCommand(request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("CASH-000001"));
            Assert.That(result.CurrencyId, Is.EqualTo(_currencyId));
            Assert.That(result.AccountId, Is.EqualTo(_glAccountId));
            Assert.That(result.IsDefault, Is.True);
            Assert.That(_cashRepository.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task CreateCashAccountCommandHandler_ReplacesOccupiedReservedCode()
    {
        await _cashRepository.AddAsync(
            CashAccount.Create(
                Guid.NewGuid(),
                "CASH-000001",
                "صندوق موجود",
                Guid.NewGuid(),
                _currencyId,
                false,
                true));

        var handler = new CreateCashAccountCommandHandler(
            _cashRepository,
            _currencyRepository,
            _linkedAccounts,
            _sequences);

        var result = await handler.Handle(
            new CreateCashAccountCommand(
                new CreateCashAccountRequest(
                    "CASH-000001",
                    "صندوق جديد",
                    _currencyId,
                    false,
                    true)),
            CancellationToken.None);

        Assert.That(result.Code, Is.EqualTo("CASH-000002"));
    }

    [Test]
    public async Task UpdateCashAccountCommandHandler_UpdatesDetailsAndPreservesAutomaticCode()
    {
        var existing = CashAccount.Create(
            Guid.NewGuid(),
            "CASH-000123",
            "الصندوق",
            _glAccountId,
            _currencyId,
            false,
            true);

        await _cashRepository.AddAsync(existing);

        var handler = new UpdateCashAccountCommandHandler(
            _cashRepository,
            _currencyRepository,
            _linkedAccounts,
            _cashMapper);

        var request = new UpdateCashAccountRequest(
            "الصندوق الرئيسي المعدل",
            _currencyId,
            true,
            true,
            Convert.ToBase64String(existing.RowVersion));

        var result = await handler.Handle(
            new UpdateCashAccountCommand(existing.Id, request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("CASH-000123"));
            Assert.That(result.Name, Is.EqualTo("الصندوق الرئيسي المعدل"));
            Assert.That(result.CurrencyId, Is.EqualTo(_currencyId));
            Assert.That(result.IsDefault, Is.True);
            Assert.That(_linkedAccounts.LastSynchronizedAccountId, Is.EqualTo(_glAccountId));
        });
    }

    [Test]
    public async Task GetCashAccountByIdQueryHandler_ReturnsMappedDto()
    {
        var existing = CashAccount.Create(
            Guid.NewGuid(),
            "CASH-000001",
            "الصندوق الرئيسي",
            _glAccountId,
            _currencyId,
            true,
            true);

        await _cashRepository.AddAsync(existing);

        var handler = new GetCashAccountByIdQueryHandler(
            _cashRepository,
            _cashMapper);

        var dto = await handler.Handle(
            new GetCashAccountByIdQuery(existing.Id),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(existing.Id));
            Assert.That(dto.Code, Is.EqualTo("CASH-000001"));
            Assert.That(dto.AccountId, Is.EqualTo(_glAccountId));
            Assert.That(dto.CurrencyId, Is.EqualTo(_currencyId));
        });
    }

    [Test]
    public async Task ReserveBankAccountCodeCommandHandler_ReturnsAutomaticCode()
    {
        var handler = new ReserveBankAccountCodeCommandHandler(
            _sequences,
            _bankRepository);

        var result = await handler.Handle(
            new ReserveBankAccountCodeCommand(),
            CancellationToken.None);

        Assert.That(result.BankAccountCode, Is.EqualTo("BANK-000001"));
    }

    [Test]
    public async Task CreateBankAccountCommandHandler_GeneratesCodeAndCreatesLinkedAccount()
    {
        var handler = new CreateBankAccountCommandHandler(
            _bankRepository,
            _currencyRepository,
            _linkedAccounts,
            _sequences);

        var request = new CreateBankAccountRequest(
            null,
            "البنك الرئيسي",
            "الحساب الجاري",
            "123456",
            "YE00TEST123456",
            _currencyId,
            true);

        var result = await handler.Handle(
            new CreateBankAccountCommand(request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("BANK-000001"));
            Assert.That(result.AccountNumber, Is.EqualTo("123456"));
            Assert.That(result.AccountId, Is.EqualTo(_glAccountId));
            Assert.That(result.CurrencyId, Is.EqualTo(_currencyId));
            Assert.That(_bankRepository.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task CreateBankAccountCommandHandler_ReplacesOccupiedReservedCode()
    {
        await _bankRepository.AddAsync(
            BankAccount.Create(
                Guid.NewGuid(),
                "BANK-000001",
                "بنك موجود",
                "جاري",
                "111111",
                null,
                Guid.NewGuid(),
                _currencyId,
                true));

        var handler = new CreateBankAccountCommandHandler(
            _bankRepository,
            _currencyRepository,
            _linkedAccounts,
            _sequences);

        var result = await handler.Handle(
            new CreateBankAccountCommand(
                new CreateBankAccountRequest(
                    "BANK-000001",
                    "بنك جديد",
                    "الحساب الجديد",
                    "222222",
                    null,
                    _currencyId,
                    true)),
            CancellationToken.None);

        Assert.That(result.Code, Is.EqualTo("BANK-000002"));
    }

    [Test]
    public async Task UpdateBankAccountCommandHandler_UpdatesDetailsAndPreservesAutomaticCodeAndAccountNumber()
    {
        var existing = BankAccount.Create(
            Guid.NewGuid(),
            "BANK-000123",
            "البنك",
            "الجاري",
            "123456",
            null,
            _glAccountId,
            _currencyId,
            true);

        await _bankRepository.AddAsync(existing);

        var handler = new UpdateBankAccountCommandHandler(
            _bankRepository,
            _currencyRepository,
            _linkedAccounts,
            _bankMapper);

        var request = new UpdateBankAccountRequest(
            "البنك الرئيسي المعدل",
            "الحساب الجاري الرئيسي",
            "123456",
            "YE00NEWIBAN",
            _currencyId,
            true,
            Convert.ToBase64String(existing.RowVersion));

        var result = await handler.Handle(
            new UpdateBankAccountCommand(existing.Id, request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("BANK-000123"));
            Assert.That(result.BankName, Is.EqualTo("البنك الرئيسي المعدل"));
            Assert.That(result.AccountName, Is.EqualTo("الحساب الجاري الرئيسي"));
            Assert.That(result.AccountNumber, Is.EqualTo("123456"));
            Assert.That(result.IBAN, Is.EqualTo("YE00NEWIBAN"));
            Assert.That(result.CurrencyId, Is.EqualTo(_currencyId));
        });
    }

    [Test]
    public void UpdateBankAccountCommandHandler_ChangingAccountNumber_ThrowsConflictException()
    {
        var existing = BankAccount.Create(
            Guid.NewGuid(),
            "BANK-000001",
            "البنك",
            "الجاري",
            "123456",
            null,
            _glAccountId,
            _currencyId,
            true);

        _bankRepository.AddAsync(existing).GetAwaiter().GetResult();

        var handler = new UpdateBankAccountCommandHandler(
            _bankRepository,
            _currencyRepository,
            _linkedAccounts,
            _bankMapper);

        var request = new UpdateBankAccountRequest(
            existing.BankName,
            existing.AccountName,
            "999999",
            existing.IBAN,
            _currencyId,
            true,
            Convert.ToBase64String(existing.RowVersion));

        var ex = Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new UpdateBankAccountCommand(existing.Id, request),
                CancellationToken.None));

        Assert.That(
            ex!.Code,
            Is.EqualTo("accounting_bank_account_number_immutable"));
    }

    private sealed class FakeLinkedAccountingAccountProvisioningService(Guid accountId)
        : ILinkedAccountingAccountProvisioningService
    {
        public Guid? LastSynchronizedAccountId { get; private set; }

        public Task<Account> ProvisionCashAccountAsync(
            string nameAr,
            bool isActive,
            DateOnly? effectiveDate = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateAccount(nameAr, isActive));

        public Task<Account> ProvisionBankAccountAsync(
            string nameAr,
            bool isActive,
            DateOnly? effectiveDate = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateAccount(nameAr, isActive));

        public Task<Account> ProvisionEmployeeAccountAsync(
            string nameAr,
            bool isActive,
            DateOnly? effectiveDate = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateAccount(nameAr, isActive));

        public Task SynchronizeAsync(
            Guid synchronizedAccountId,
            string nameAr,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            LastSynchronizedAccountId = synchronizedAccountId;
            return Task.CompletedTask;
        }

        private Account CreateAccount(string nameAr, bool isActive) =>
            Account.Create(
                accountId,
                "1101000001",
                nameAr,
                null,
                null,
                1,
                AccountClass.Asset,
                AccountType.Subledger,
                NormalBalance.Debit,
                true,
                false,
                false,
                false,
                isActive,
                null);
    }
}
