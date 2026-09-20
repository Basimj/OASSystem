using NUnit.Framework;
using OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;
using OAS.Application.Accounting.Journals.Commands.ReverseJournalEntry;
using OAS.Application.Accounting.Journals.Commands.SetJournalEntryStatus;
using OAS.Application.Accounting.Journals.Queries.GetJournalEntries;
using OAS.Application.Accounting.Journals.Queries.GetJournalEntryById;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainJournalType = OAS.Domain.Accounting.Enums.JournalType;
using DomainJournalStatus = OAS.Domain.Accounting.Enums.JournalEntryStatus;
using DomainAccountClass = OAS.Domain.Accounting.Enums.AccountClass;
using DomainAccountType = OAS.Domain.Accounting.Enums.AccountType;
using DomainNormalBalance = OAS.Domain.Accounting.Enums.NormalBalance;
using DomainFiscalPeriodStatus = OAS.Domain.Accounting.Enums.FiscalPeriodStatus;

namespace OAS.Tests.Accounting.Application.Journals;

[TestFixture]
public class JournalCommandAndQueryTests
{
    private FakeRepository<JournalEntry, Guid> _journalRepository = null!;
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
            _sequenceGenerator,
            _currentUser);

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
        var journalId = await handler.Handle(command, CancellationToken.None);

        Assert.That(journalId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(_journalRepository.Items.Count, Is.EqualTo(1));
        var saved = _journalRepository.Items[0];
        Assert.That(saved.Lines.Count, Is.EqualTo(2));
        Assert.That(saved.JournalNumber, Does.StartWith("JV-2026-"));
    }

    [Test]
    public async Task SetJournalEntryStatusCommandHandler_PostsJournalSuccessfully()
    {
        var period = FiscalPeriod.Create(
            _fiscalPeriodId, Guid.NewGuid(), 1, "Period 01",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            DomainFiscalPeriodStatus.Open, false, false, false);
        await _periodRepository.AddAsync(period);

        var debitAcc = Account.Create(
            _accountDebitId, "1101", "الصندوق", null, null, 1,
            DomainAccountClass.Asset, DomainAccountType.Posting, DomainNormalBalance.Debit,
            true, false, true, false, true, null);
        var creditAcc = Account.Create(
            _accountCreditId, "4101", "المبيعات", null, null, 1,
            DomainAccountClass.Revenue, DomainAccountType.Posting, DomainNormalBalance.Credit,
            true, false, true, false, true, null);
        await _accountRepository.AddRangeAsync([debitAcc, creditAcc]);

        var journal = JournalEntry.Create(
            Guid.NewGuid(), "JV-2026-000001", DomainJournalType.Manual,
            new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15),
            _fiscalPeriodId, "قيد تجريبي", null, null, null,
            DomainJournalStatus.Draft, Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);

        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 1, _accountDebitId, 500m, 0m, null, null, null, null, null, null));
        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 2, _accountCreditId, 0m, 500m, null, null, null, null, null, null));
        journal.SetPendingApproval();
        journal.Approve(Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);
        await _journalRepository.AddAsync(journal);

        var handler = new SetJournalEntryStatusCommandHandler(
            _journalRepository,
            _periodRepository,
            _accountRepository,
            _currentUser,
            TimeProvider.System);

        var setStatusRequest = new SetJournalEntryStatusRequest(JournalEntryStatus.Posted, Convert.ToBase64String(journal.RowVersion));
        var command = new SetJournalEntryStatusCommand(journal.Id, setStatusRequest);
        await handler.Handle(command, CancellationToken.None);

        Assert.That(journal.Status, Is.EqualTo(DomainJournalStatus.Posted));
        Assert.That(journal.PostedBy, Is.EqualTo(Guid.Parse(_currentUser.UserId!)));
    }

    [Test]
    public async Task ReverseJournalEntryCommandHandler_CreatesOppositeJournalAndSetsReversed()
    {
        var journal = JournalEntry.Create(
            Guid.NewGuid(), "JV-2026-000001", DomainJournalType.Manual,
            new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15),
            _fiscalPeriodId, "قيد تجريبي", null, null, null,
            DomainJournalStatus.Draft, Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);

        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 1, _accountDebitId, 1200m, 0m, null, null, null, null, null, null));
        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 2, _accountCreditId, 0m, 1200m, null, null, null, null, null, null));
        journal.SetPendingApproval();
        journal.Approve(Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);
        journal.Post(Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);
        await _journalRepository.AddAsync(journal);

        var handler = new ReverseJournalEntryCommandHandler(
            _journalRepository,
            _sequenceGenerator,
            _currentUser);

        var command = new ReverseJournalEntryCommand(journal.Id);
        var reversalId = await handler.Handle(command, CancellationToken.None);

        Assert.That(reversalId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(journal.Status, Is.EqualTo(DomainJournalStatus.Reversed));
        Assert.That(_journalRepository.Items.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetJournalEntryByIdQueryHandler_ReturnsDetailedDto()
    {
        var journal = JournalEntry.Create(
            Guid.NewGuid(), "JV-2026-000001", DomainJournalType.Manual,
            new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15),
            _fiscalPeriodId, "قيد", null, null, null,
            DomainJournalStatus.Draft, Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);

        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 1, _accountDebitId, 100m, 0m, "Line 1", null, null, null, null, null));
        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 2, _accountCreditId, 0m, 100m, "Line 2", null, null, null, null, null));
        await _journalRepository.AddAsync(journal);

        var handler = new GetJournalEntryByIdQueryHandler(_journalRepository);
        var query = new GetJournalEntryByIdQuery(journal.Id);
        var dto = await handler.Handle(query, CancellationToken.None);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.JournalNumber, Is.EqualTo("JV-2026-000001"));
        Assert.That(dto.Lines.Count, Is.EqualTo(2));
        Assert.That(dto.Lines.Sum(x => x.DebitAmount), Is.EqualTo(100m));
        Assert.That(dto.Lines.Sum(x => x.CreditAmount), Is.EqualTo(100m));
    }
}
