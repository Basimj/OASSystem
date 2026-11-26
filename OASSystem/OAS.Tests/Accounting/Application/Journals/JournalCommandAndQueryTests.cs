using NUnit.Framework;
using OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.CoreHardening.Services;
using OAS.Application.Accounting.Journals.Commands.ReverseJournalEntry;
using OAS.Application.Accounting.Journals.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Application.Accounting.Journals.Commands.SetJournalEntryStatus;
using OAS.Application.Accounting.Journals.Queries.GetJournalEntryById;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Journals;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainAccountClass = OAS.Domain.Accounting.Enums.AccountClass;
using DomainAccountType = OAS.Domain.Accounting.Enums.AccountType;
using DomainExchangeRateSource = OAS.Domain.Accounting.Enums.ExchangeRateSource;
using DomainExchangeRateType = OAS.Domain.Accounting.Enums.ExchangeRateType;
using DomainFiscalPeriodStatus = OAS.Domain.Accounting.Enums.FiscalPeriodStatus;
using DomainJournalStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;
using DomainNormalBalance = OAS.Domain.Accounting.Enums.NormalBalance;

namespace OAS.Tests.Accounting.Application.Journals;

[TestFixture]
public class JournalCommandAndQueryTests
{
    private FakeRepository<JournalEntry, Guid> _journalRepository = null!;
    private FakeRepository<JournalEntryLine, Guid> _lineRepository = null!;
    private FakeRepository<FiscalPeriod, Guid> _periodRepository = null!;
    private FakeRepository<Account, Guid> _accountRepository = null!;

    private FakeCurrentUser _currentUser = null!;
    private FakeSequenceNumberGenerator _sequenceGenerator = null!;

    private Guid _fiscalPeriodId;
    private Guid _accountDebitId;
    private Guid _accountCreditId;

    [SetUp]
    public void Setup()
    {
        _journalRepository = new FakeRepository<JournalEntry, Guid>();
        _lineRepository = new FakeRepository<JournalEntryLine, Guid>();
        _periodRepository = new FakeRepository<FiscalPeriod, Guid>();
        _accountRepository = new FakeRepository<Account, Guid>();

        _currentUser = new FakeCurrentUser();
        _sequenceGenerator = new FakeSequenceNumberGenerator();

        _fiscalPeriodId = Guid.NewGuid();
        _accountDebitId = Guid.NewGuid();
        _accountCreditId = Guid.NewGuid();
    }

    [Test]
    public async Task CreateJournalEntryCommandHandler_CreatesDraftMultiCurrencyJournalWithLines()
    {
        var currencyId = Guid.NewGuid();
        var currency = Currency.Create(currencyId, "YER", "الريال اليمني", null, "﷼", 2);
        var settings = AccountingSettings.Create(currencyId);

        var debitAccount = Account.Create(
            _accountDebitId, "1101", "الصندوق", null, null, 1,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            true, false, true, false, true, null);
        var creditAccount = Account.Create(
            _accountCreditId, "4101", "الإيرادات", null, null, 1,
            DomainAccountClass.Revenue, DomainAccountType.Posting, DomainNormalBalance.Credit,
            true, false, true, false, true, null);

        var accountRepository = new FakeRepository<Account, Guid>([debitAccount, creditAccount]);
        var currencyRepository = new FakeRepository<Currency, Guid>([currency]);
        var settingsRepository = new FakeRepository<AccountingSettings, Guid>([settings]);

        var subledgerIntegrity = new JournalSubledgerIntegrityService(
            new FakeRepository<Customer, Guid>(),
            new FakeRepository<Supplier, Guid>(),
            new FakeRepository<OAS.Domain.Features.Employees.Entities.Employee, Guid>(),
            new FakeRepository<EmployeeAccount, Guid>());

        var handler = new CreateJournalEntryCommandHandler(
            _journalRepository,
            accountRepository,
            settingsRepository,
            currencyRepository,
            new FixedExchangeRateResolver(currency),
            new TestCurrencyRoundingService(),
            new FakePermissionChecker(),
            _sequenceGenerator,
            subledgerIntegrity,
            new JournalSourceIntegrityService(_journalRepository));

        var request = new CreateJournalEntryRequest(
            JournalType: JournalType.Manual,
            PostingDate: new DateOnly(2026, 1, 15),
            DocumentDate: new DateOnly(2026, 1, 15),
            FiscalPeriodId: _fiscalPeriodId,
            Description: "قيد تسوية عام",
            SourceModule: null,
            SourceDocumentType: null,
            SourceDocumentId: null,
            Lines:
            [
                new CreateJournalEntryLineRequest(
                    AccountId: _accountDebitId,
                    TransactionDebitAmount: 2000m,
                    TransactionCreditAmount: 0m,
                    TransactionCurrencyId: currencyId,
                    Description: "مدين"),

                new CreateJournalEntryLineRequest(
                    AccountId: _accountCreditId,
                    TransactionDebitAmount: 0m,
                    TransactionCreditAmount: 2000m,
                    TransactionCurrencyId: currencyId,
                    Description: "دائن")
            ]);

        foreach (var type in new[] { JournalType.Automatic, JournalType.Opening, JournalType.Closing, JournalType.Reversal })
            Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(new CreateJournalEntryCommand(request with { JournalType = type }), CancellationToken.None));
        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(new CreateJournalEntryCommand(request with { SourceDocumentType = "SalesInvoice", SourceDocumentId = Guid.NewGuid() }), CancellationToken.None));
        var journalId = await handler.Handle(
            new CreateJournalEntryCommand(request),
            CancellationToken.None);

        Assert.That(journalId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(_journalRepository.Items.Count, Is.EqualTo(1));

        var saved = _journalRepository.Items[0];
        Assert.That(saved.Lines.Count, Is.EqualTo(2));
        Assert.That(saved.JournalNumber, Does.StartWith("JV-2026-"));
        Assert.That(saved.Status, Is.EqualTo(DomainJournalStatus.Draft));
        Assert.That(saved.BaseCurrencyId, Is.EqualTo(currencyId));
        Assert.That(saved.Lines.Sum(x => x.DebitAmount), Is.EqualTo(2000m));
        Assert.That(saved.Lines.Sum(x => x.CreditAmount), Is.EqualTo(2000m));
        Assert.That(saved.Lines.All(x => x.TransactionCurrencyId == currencyId), Is.True);
    }

    [Test]
    public async Task SetJournalEntryStatusCommandHandler_PostsJournalSuccessfully()
    {
        var period = FiscalPeriod.Create(
            _fiscalPeriodId,
            Guid.NewGuid(),
            1,
            "Period 01",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            DomainFiscalPeriodStatus.Open,
            false,
            false,
            false);

        await _periodRepository.AddAsync(period);

        var debitAccount = Account.Create(
            _accountDebitId,
            "1101",
            "الصندوق",
            null,
            null,
            1,
            DomainAccountClass.Asset,
            DomainAccountType.Posting,
            DomainNormalBalance.Debit,
            true,
            false,
            true,
            false,
            true,
            null);

        var creditAccount = Account.Create(
            _accountCreditId,
            "4101",
            "المبيعات",
            null,
            null,
            1,
            DomainAccountClass.Revenue,
            DomainAccountType.Posting,
            DomainNormalBalance.Credit,
            true,
            false,
            true,
            false,
            true,
            null);

        await _accountRepository.AddRangeAsync(
            [debitAccount, creditAccount]);

        var journal = JournalEntry.Create(
            Guid.NewGuid(),
            "JV-2026-000001",
            DomainJournalType.Manual,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15),
            _fiscalPeriodId,
            "قيد تجريبي",
            null,
            null,
            null,
            DomainJournalStatus.Draft);

        var debitLine = JournalEntryLine.Create(
            Guid.NewGuid(),
            journal.Id,
            1,
            _accountDebitId,
            500m,
            0m,
            null,
            null,
            null,
            null,
            null,
            null);

        var creditLine = JournalEntryLine.Create(
            Guid.NewGuid(),
            journal.Id,
            2,
            _accountCreditId,
            0m,
            500m,
            null,
            null,
            null,
            null,
            null,
            null);

        journal.AddLine(debitLine);
        journal.AddLine(creditLine);

        journal.SetPendingApproval();
        journal.Approve(
            Guid.Parse(_currentUser.UserId!),
            DateTime.UtcNow);

        await _journalRepository.AddAsync(journal);

        /*
         * لا نضيف الأسطر إلى _lineRepository هنا لأن FakeRepository
         * يعيد نفس الـAggregate وبداخله Lines بالفعل.
         *
         * في قاعدة البيانات الحقيقية يقوم الـHandler بتحميل Lines
         * من lineRepository لأن Repository الخاص بالرأس لا يعمل Eager Load.
         */

        var handler = new SetJournalEntryStatusCommandHandler(
            _journalRepository,
            _lineRepository,
            _periodRepository,
            _accountRepository,
            new JournalSubledgerIntegrityService(
                new FakeRepository<Customer, Guid>(),
                new FakeRepository<Supplier, Guid>(),
                new FakeRepository<OAS.Domain.Features.Employees.Entities.Employee, Guid>(),
                new FakeRepository<EmployeeAccount, Guid>()),
            new JournalSourceIntegrityService(_journalRepository),
            _currentUser,
            TimeProvider.System);

        var request = new SetJournalEntryStatusRequest(
            JournalEntryStatus.Posted,
            Convert.ToBase64String(journal.RowVersion));

        var command = new SetJournalEntryStatusCommand(
            journal.Id,
            request);

        await handler.Handle(
            command,
            CancellationToken.None);

        Assert.That(
            journal.Status,
            Is.EqualTo(DomainJournalStatus.Posted));

        Assert.That(
            journal.PostedBy,
            Is.EqualTo(Guid.Parse(_currentUser.UserId!)));

        Assert.That(
            journal.PostedAtUtc,
            Is.Not.Null);
    }

    [Test]
    public async Task ReverseJournalEntryCommandHandler_CreatesOppositeJournalAndSetsReversed()
    {
        var journal = JournalEntry.Create(
            Guid.NewGuid(),
            "JV-2026-000001",
            DomainJournalType.Manual,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15),
            _fiscalPeriodId,
            "قيد تجريبي",
            null,
            null,
            null,
            DomainJournalStatus.Draft);

        var debitLine = JournalEntryLine.Create(
            Guid.NewGuid(),
            journal.Id,
            1,
            _accountDebitId,
            1200m,
            0m,
            "مدين",
            null,
            null,
            null,
            null,
            null);

        var creditLine = JournalEntryLine.Create(
            Guid.NewGuid(),
            journal.Id,
            2,
            _accountCreditId,
            0m,
            1200m,
            "دائن",
            null,
            null,
            null,
            null,
            null);

        journal.AddLine(debitLine);
        journal.AddLine(creditLine);

        journal.SetPendingApproval();

        journal.Approve(
            Guid.Parse(_currentUser.UserId!),
            DateTime.UtcNow);

        journal.Post(
            Guid.Parse(_currentUser.UserId!),
            DateTime.UtcNow);

        await _journalRepository.AddAsync(journal);

        /*
         * ReverseJournalEntryCommandHandler أصبح يعتمد صراحة على
         * JournalEntryLine repository، لذلك يجب أن يحتوي Fake repository
         * على الأسطر التي سيعكسها.
         */
        await _lineRepository.AddRangeAsync(
            [debitLine, creditLine]);

        var handler = new ReverseJournalEntryCommandHandler(
            _journalRepository,
            _lineRepository,
            _sequenceGenerator,
            _periodRepository);

        await _periodRepository.AddAsync(FiscalPeriod.Create(_fiscalPeriodId, Guid.NewGuid(), 1, "Period 01", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), DomainFiscalPeriodStatus.Open, false, false, false));
        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(new ReverseJournalEntryCommand(journal.Id, new DateOnly(2026, 1, 14), "سبب"), CancellationToken.None));
        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(new ReverseJournalEntryCommand(journal.Id, new DateOnly(2026, 1, 20), " "), CancellationToken.None));
        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(new ReverseJournalEntryCommand(journal.Id, new DateOnly(2026, 2, 1), "سبب"), CancellationToken.None));
        var command = new ReverseJournalEntryCommand(journal.Id, new DateOnly(2026, 1, 20), "تصحيح القيد");

        var reversalId = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.That(
            reversalId,
            Is.Not.EqualTo(Guid.Empty));

        Assert.That(
            journal.Status,
            Is.EqualTo(DomainJournalStatus.Posted));

        Assert.That(
            _journalRepository.Items.Count,
            Is.EqualTo(2));

        var reversal = _journalRepository.Items
            .Single(x => x.Id == reversalId);

        Assert.That(
            reversal.JournalType,
            Is.EqualTo(DomainJournalType.Reversal));

        Assert.That(reversal.PostingDate, Is.EqualTo(new DateOnly(2026, 1, 20)));
        Assert.That(reversal.Description, Does.Contain("تصحيح القيد"));
        Assert.That(journal.ReversedJournalId, Is.EqualTo(reversal.Id));
        Assert.That(reversal.Lines.Count, Is.EqualTo(2));
        Assert.That(reversal.Lines.OrderBy(x => x.LineNumber).Zip(journal.Lines.OrderBy(x => x.LineNumber)).All(p => p.First.IsExactOppositeOf(p.Second)), Is.True);
        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(command, CancellationToken.None));
        var pending = await JournalReversalInfoResolver.ResolveAsync(_journalRepository, new[] { journal, reversal }, CancellationToken.None);
        Assert.That(pending[journal.Id].ReversalJournalStatus, Is.EqualTo(JournalEntryStatus.Draft));
        Assert.That(pending[reversal.Id].OriginalJournalNumber, Is.EqualTo(journal.JournalNumber));
        reversal.SetPendingApproval();
        reversal.Approve(Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);
        reversal.Post(Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);
        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(new ReverseJournalEntryCommand(reversal.Id, new DateOnly(2026, 1, 21), "عكس مرة أخرى"), CancellationToken.None));
        var completed = await JournalReversalInfoResolver.ResolveAsync(_journalRepository, new[] { journal }, CancellationToken.None);
        Assert.That(completed[journal.Id].ReversalJournalStatus, Is.EqualTo(JournalEntryStatus.Posted));

        Assert.That(
            reversal.Lines.Sum(x => x.DebitAmount),
            Is.EqualTo(1200m));

        Assert.That(
            reversal.Lines.Sum(x => x.CreditAmount),
            Is.EqualTo(1200m));
    }

    [TestCase(DomainJournalType.Automatic, null)]
    [TestCase(DomainJournalType.Opening, null)]
    [TestCase(DomainJournalType.Closing, null)]
    [TestCase(DomainJournalType.Reversal, null)]
    [TestCase(DomainJournalType.Manual, "SalesInvoice")]
    public async Task ReverseJournalEntry_RejectsSystemOrSourceBoundJournal(DomainJournalType type, string? sourceType)
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), "JV-BLOCK", type, new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15), _fiscalPeriodId, "test", sourceType is null ? null : "Sales", sourceType, sourceType is null ? null : Guid.NewGuid(), DomainJournalStatus.Draft);
        entry.AddLine(JournalEntryLine.Create(Guid.NewGuid(), entry.Id, 1, _accountDebitId, 100m, 0m, null, null, null, null, null, null));
        entry.AddLine(JournalEntryLine.Create(Guid.NewGuid(), entry.Id, 2, _accountCreditId, 0m, 100m, null, null, null, null, null, null));
        entry.SetPendingApproval();
        entry.Approve(Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);
        entry.Post(Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);
        await _journalRepository.AddAsync(entry);
        var handler = new ReverseJournalEntryCommandHandler(_journalRepository, _lineRepository, _sequenceGenerator, _periodRepository);
        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(new ReverseJournalEntryCommand(entry.Id, new DateOnly(2026, 1, 20), "سبب"), CancellationToken.None));
        Assert.That(entry.ReversedJournalId, Is.Null);
    }

    [Test]
    public async Task GetJournalEntryByIdQueryHandler_ReturnsDetailedDto()
    {
        var journal = JournalEntry.Create(
            Guid.NewGuid(),
            "JV-2026-000001",
            DomainJournalType.Manual,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15),
            _fiscalPeriodId,
            "قيد",
            null,
            null,
            null,
            DomainJournalStatus.Draft);

        var debitLine = JournalEntryLine.Create(
            Guid.NewGuid(),
            journal.Id,
            1,
            _accountDebitId,
            100m,
            0m,
            "Line 1",
            null,
            null,
            null,
            null,
            null);

        var creditLine = JournalEntryLine.Create(
            Guid.NewGuid(),
            journal.Id,
            2,
            _accountCreditId,
            0m,
            100m,
            "Line 2",
            null,
            null,
            null,
            null,
            null);

        await _journalRepository.AddAsync(journal);

        /*
         * GetById الآن يقرأ Lines من repository مستقل.
         */
        await _lineRepository.AddRangeAsync(
            [debitLine, creditLine]);

        var handler = new GetJournalEntryByIdQueryHandler(
            _journalRepository,
            _lineRepository);

        var query = new GetJournalEntryByIdQuery(journal.Id);

        var dto = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.That(dto, Is.Not.Null);

        Assert.That(
            dto.JournalNumber,
            Is.EqualTo("JV-2026-000001"));

        Assert.That(
            dto.Lines.Count,
            Is.EqualTo(2));

        Assert.That(
            dto.Lines.Sum(x => x.DebitAmount),
            Is.EqualTo(100m));

        Assert.That(
            dto.Lines.Sum(x => x.CreditAmount),
            Is.EqualTo(100m));
    }

    private sealed class FixedExchangeRateResolver(Currency currency) : IExchangeRateResolver
    {
        public Task<ExchangeRateResolution> ResolveAsync(
            Guid currencyId,
            DateOnly documentDate,
            DomainExchangeRateType rateType = DomainExchangeRateType.Accounting,
            decimal? manualRate = null,
            bool manualOverrideAllowed = false,
            CancellationToken cancellationToken = default)
        {
            if (currencyId != currency.Id)
                throw new InvalidOperationException("Unexpected test currency.");

            return Task.FromResult(new ExchangeRateResolution(
                currency.Id, currency.Code, currency.Symbol, currency.DecimalPlaces,
                1m, documentDate, rateType, DomainExchangeRateSource.System, true));
        }
    }

    private sealed class TestCurrencyRoundingService : ICurrencyRoundingService
    {
        public decimal Round(decimal amount, byte decimalPlaces)
            => Math.Round(amount, decimalPlaces, MidpointRounding.AwayFromZero);

        public decimal CalculateBaseAmount(
            decimal transactionAmount,
            decimal exchangeRate,
            byte transactionDecimalPlaces,
            byte baseDecimalPlaces)
            => Round(Round(transactionAmount, transactionDecimalPlaces) * exchangeRate, baseDecimalPlaces);
    }

}
