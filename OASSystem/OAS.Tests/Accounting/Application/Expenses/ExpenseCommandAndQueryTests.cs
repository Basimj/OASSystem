using NUnit.Framework;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Expenses.Commands.CreateExpense;
using OAS.Application.Accounting.Expenses.Commands.SetExpenseStatus;
using OAS.Application.Accounting.Expenses.Mapping;
using OAS.Application.Accounting.Expenses.Queries.GetExpenseById;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;
using DomainExpenseStatus = OAS.Domain.Accounting.Enums.ExpenseStatus;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;

namespace OAS.Tests.Accounting.Application.Expenses;

[TestFixture]
public class ExpenseCommandAndQueryTests
{
    private FakeRepository<Expense, Guid> _repository = null!;
    private FakeCurrentUser _currentUser = null!;
    private FakePermissionChecker _permissionChecker = null!;
    private FakeSequenceNumberGenerator _sequenceGenerator = null!;
    private FakeAccountingDocumentPostingService _postingService = null!;
    private ExpenseMapper _mapper = null!;

    private Guid _expenseTypeId;
    private Guid _expenseAccountId;
    private Guid _cashAccountId;

    [SetUp]
    public void Setup()
    {
        _repository = new FakeRepository<Expense, Guid>();
        _currentUser = new FakeCurrentUser();
        _permissionChecker = new FakePermissionChecker();
        _sequenceGenerator = new FakeSequenceNumberGenerator();
        _postingService = new FakeAccountingDocumentPostingService();
        _mapper = new ExpenseMapper();

        _expenseTypeId = Guid.NewGuid();
        _expenseAccountId = Guid.NewGuid();
        _cashAccountId = Guid.NewGuid();
    }

    [Test]
    public async Task CreateExpenseCommandHandler_CreatesExpenseWithGeneratedNumber()
    {
        var handler = new CreateExpenseCommandHandler(
            _repository,
            _sequenceGenerator,
            _currentUser,
            TimeProvider.System);

        var request = new CreateExpenseRequest(
            ExpenseDate: new DateOnly(2026, 1, 20),
            ExpenseTypeId: _expenseTypeId,
            ExpenseAccountId: _expenseAccountId,
            Beneficiary: "شركة الخدمات",
            Amount: 650m,
            PaymentMethod: PaymentMethod.Cash,
            CashAccountId: _cashAccountId,
            BankAccountId: null,
            Description: "مصاريف صيانة");

        var command = new CreateExpenseCommand(request);

        var expenseId = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.That(
            expenseId,
            Is.Not.EqualTo(Guid.Empty));

        Assert.That(
            _repository.Items.Count,
            Is.EqualTo(1));

        var saved = _repository.Items[0];

        Assert.That(
            saved.ExpenseNumber,
            Does.StartWith("EXP-2026-"));

        Assert.That(
            saved.Amount,
            Is.EqualTo(650m));

        Assert.That(
            saved.Status,
            Is.EqualTo(DomainExpenseStatus.Draft));
    }

    [Test]
    public async Task SetExpenseStatusCommandHandler_ApprovesExpense()
    {
        var expense = Expense.Create(
            Guid.NewGuid(),
            "EXP-2026-000001",
            new DateOnly(2026, 1, 20),
            _expenseTypeId,
            _expenseAccountId,
            "المستفيد",
            500m,
            DomainPaymentMethod.Cash,
            _cashAccountId,
            null,
            "وصف",
            DomainExpenseStatus.Draft,
            null,
            Guid.Parse(_currentUser.UserId!),
            DateTime.UtcNow);

        await _repository.AddAsync(expense);

        var handler = new SetExpenseStatusCommandHandler(
            _repository,
            _postingService,
            _permissionChecker,
            _currentUser,
            TimeProvider.System);

        var setStatusRequest = new SetExpenseStatusRequest(
            ExpenseStatus.Approved,
            Convert.ToBase64String(expense.RowVersion));

        var command = new SetExpenseStatusCommand(
            expense.Id,
            setStatusRequest);

        await handler.Handle(
            command,
            CancellationToken.None);

        Assert.That(
            expense.Status,
            Is.EqualTo(DomainExpenseStatus.Approved));

        Assert.That(
            _postingService.PostExpenseCallCount,
            Is.EqualTo(0));
    }

    [Test]
    public async Task SetExpenseStatusCommandHandler_PostsExpenseAndSetsJournalEntry()
    {
        var expense = Expense.Create(
            Guid.NewGuid(),
            "EXP-2026-000002",
            new DateOnly(2026, 1, 20),
            _expenseTypeId,
            _expenseAccountId,
            "المستفيد",
            750m,
            DomainPaymentMethod.Cash,
            _cashAccountId,
            null,
            "مصروف مرحل",
            DomainExpenseStatus.Draft,
            null,
            Guid.Parse(_currentUser.UserId!),
            DateTime.UtcNow);

        await _repository.AddAsync(expense);

        /*
         * الانتقال إلى Posted يجب أن يتم من Approved،
         * لذلك نعتمد المصروف أولاً.
         */
        expense.Approve();

        var expectedJournalId = Guid.NewGuid();

        _postingService.JournalEntryId = expectedJournalId;

        var handler = new SetExpenseStatusCommandHandler(
            _repository,
            _postingService,
            _permissionChecker,
            _currentUser,
            TimeProvider.System);

        var request = new SetExpenseStatusRequest(
            ExpenseStatus.Posted,
            Convert.ToBase64String(expense.RowVersion));

        var command = new SetExpenseStatusCommand(
            expense.Id,
            request);

        await handler.Handle(
            command,
            CancellationToken.None);

        Assert.That(
            expense.Status,
            Is.EqualTo(DomainExpenseStatus.Posted));

        Assert.That(
            expense.JournalEntryId,
            Is.EqualTo(expectedJournalId));

        Assert.That(
            expense.PostedAtUtc,
            Is.Not.Null);

        Assert.That(
            _postingService.PostExpenseCallCount,
            Is.EqualTo(1));

        Assert.That(
            _postingService.LastPostedExpenseId,
            Is.EqualTo(expense.Id));

        Assert.That(
            _postingService.LastPostedBy,
            Is.EqualTo(Guid.Parse(_currentUser.UserId!)));
    }

    [Test]
    public async Task GetExpenseByIdQueryHandler_ReturnsMappedDto()
    {
        var expense = Expense.Create(
            Guid.NewGuid(),
            "EXP-2026-000001",
            new DateOnly(2026, 1, 20),
            _expenseTypeId,
            _expenseAccountId,
            "المستفيد",
            500m,
            DomainPaymentMethod.Cash,
            _cashAccountId,
            null,
            "وصف",
            DomainExpenseStatus.Draft,
            null,
            Guid.Parse(_currentUser.UserId!),
            DateTime.UtcNow);

        await _repository.AddAsync(expense);

        var handler = new GetExpenseByIdQueryHandler(
            _repository,
            _mapper);

        var query = new GetExpenseByIdQuery(expense.Id);

        var dto = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.That(dto, Is.Not.Null);

        Assert.That(
            dto.ExpenseNumber,
            Is.EqualTo("EXP-2026-000001"));

        Assert.That(
            dto.Amount,
            Is.EqualTo(500m));
    }

    private sealed class FakeAccountingDocumentPostingService
        : IAccountingDocumentPostingService
    {
        public Guid JournalEntryId { get; set; } = Guid.NewGuid();

        public int PostReceiptVoucherCallCount { get; private set; }

        public int PostPaymentVoucherCallCount { get; private set; }

        public int PostExpenseCallCount { get; private set; }

        public Guid? LastPostedExpenseId { get; private set; }

        public Guid? LastPostedBy { get; private set; }

        public DateTime? LastPostedAtUtc { get; private set; }

        public Task<Guid> PostReceiptVoucherAsync(
            ReceiptVoucher voucher,
            IReadOnlyCollection<ReceiptVoucherLine> lines,
            Guid postedBy,
            DateTime postedAtUtc,
            CancellationToken cancellationToken = default)
        {
            PostReceiptVoucherCallCount++;

            return Task.FromResult(JournalEntryId);
        }

        public Task<Guid> PostPaymentVoucherAsync(
            PaymentVoucher voucher,
            IReadOnlyCollection<PaymentVoucherLine> lines,
            Guid postedBy,
            DateTime postedAtUtc,
            CancellationToken cancellationToken = default)
        {
            PostPaymentVoucherCallCount++;

            return Task.FromResult(JournalEntryId);
        }

        public Task<Guid> PostExpenseAsync(
            Expense expense,
            Guid postedBy,
            DateTime postedAtUtc,
            CancellationToken cancellationToken = default)
        {
            PostExpenseCallCount++;
            LastPostedExpenseId = expense.Id;
            LastPostedBy = postedBy;
            LastPostedAtUtc = postedAtUtc;

            return Task.FromResult(JournalEntryId);
        }
    }
}