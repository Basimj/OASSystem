using NUnit.Framework;
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
        var expenseId = await handler.Handle(command, CancellationToken.None);

        Assert.That(expenseId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(_repository.Items.Count, Is.EqualTo(1));
        var saved = _repository.Items[0];
        Assert.That(saved.ExpenseNumber, Does.StartWith("EXP-2026-"));
        Assert.That(saved.Amount, Is.EqualTo(650m));
        Assert.That(saved.Status, Is.EqualTo(DomainExpenseStatus.Draft));
    }

    [Test]
    public async Task SetExpenseStatusCommandHandler_ApprovesExpense()
    {
        var expense = Expense.Create(
            Guid.NewGuid(), "EXP-2026-000001", new DateOnly(2026, 1, 20),
            _expenseTypeId, _expenseAccountId, "المستفيد", 500m,
            DomainPaymentMethod.Cash, _cashAccountId, null, "وصف",
            DomainExpenseStatus.Draft, null, Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);

        await _repository.AddAsync(expense);

        var handler = new SetExpenseStatusCommandHandler(_repository, _permissionChecker, TimeProvider.System);
        var setStatusRequest = new SetExpenseStatusRequest(ExpenseStatus.Approved, Convert.ToBase64String(expense.RowVersion));
        var command = new SetExpenseStatusCommand(expense.Id, setStatusRequest);
        await handler.Handle(command, CancellationToken.None);

        Assert.That(expense.Status, Is.EqualTo(DomainExpenseStatus.Approved));
    }

    [Test]
    public async Task GetExpenseByIdQueryHandler_ReturnsMappedDto()
    {
        var expense = Expense.Create(
            Guid.NewGuid(), "EXP-2026-000001", new DateOnly(2026, 1, 20),
            _expenseTypeId, _expenseAccountId, "المستفيد", 500m,
            DomainPaymentMethod.Cash, _cashAccountId, null, "وصف",
            DomainExpenseStatus.Draft, null, Guid.Parse(_currentUser.UserId!), DateTime.UtcNow);

        await _repository.AddAsync(expense);

        var handler = new GetExpenseByIdQueryHandler(_repository, _mapper);
        var query = new GetExpenseByIdQuery(expense.Id);
        var dto = await handler.Handle(query, CancellationToken.None);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.ExpenseNumber, Is.EqualTo("EXP-2026-000001"));
        Assert.That(dto.Amount, Is.EqualTo(500m));
    }
}
