using NUnit.Framework;
using OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;
using OAS.Application.Accounting.Journals.Commands.ReverseJournalEntry;
using OAS.Application.Accounting.Journals.Commands.SetJournalEntryStatus;
using OAS.Application.Accounting.Journals.Queries.GetJournalEntryById;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Journals;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainAccountClass = OAS.Domain.Accounting.Enums.AccountClass;
using DomainAccountType = OAS.Domain.Accounting.Enums.AccountType;
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
    public async Task CreateJournalEntryCommandHandler_CreatesDraftJournalWithLines()
    {
        var handler = new CreateJournalEntryCommandHandler(
            _journalRepository,
            new FakeRepository<Customer, Guid>(),
            new FakeRepository<Supplier, Guid>(),
            _sequenceGenerator);

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
                    DebitAmount: 2000m,
                    CreditAmount: 0m,
                    Description: "مدين",
                    CustomerId: null,
                    SupplierId: null,
                    CostCenterId: null,
                    ProductVariantId: null,
                    WarehouseId: null),

                new CreateJournalEntryLineRequest(
                    AccountId: _accountCreditId,
                    DebitAmount: 0m,
                    CreditAmount: 2000m,
                    Description: "دائن",
                    CustomerId: null,
                    SupplierId: null,
                    CostCenterId: null,
                    ProductVariantId: null,
                    WarehouseId: null)
            ]);

        var command = new CreateJournalEntryCommand(request);

        var journalId = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.That(journalId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(_journalRepository.Items.Count, Is.EqualTo(1));

        var saved = _journalRepository.Items[0];

        Assert.That(saved.Lines.Count, Is.EqualTo(2));
        Assert.That(saved.JournalNumber, Does.StartWith("JV-2026-"));
        Assert.That(saved.Status, Is.EqualTo(DomainJournalStatus.Draft));
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
            _sequenceGenerator);

        var command = new ReverseJournalEntryCommand(journal.Id);

        var reversalId = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.That(
            reversalId,
            Is.Not.EqualTo(Guid.Empty));

        Assert.That(
            journal.Status,
            Is.EqualTo(DomainJournalStatus.Reversed));

        Assert.That(
            _journalRepository.Items.Count,
            Is.EqualTo(2));

        var reversal = _journalRepository.Items
            .Single(x => x.Id == reversalId);

        Assert.That(
            reversal.JournalType,
            Is.EqualTo(DomainJournalType.Reversal));

        Assert.That(
            reversal.Lines.Count,
            Is.EqualTo(2));

        Assert.That(
            reversal.Lines.Sum(x => x.DebitAmount),
            Is.EqualTo(1200m));

        Assert.That(
            reversal.Lines.Sum(x => x.CreditAmount),
            Is.EqualTo(1200m));
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
}
