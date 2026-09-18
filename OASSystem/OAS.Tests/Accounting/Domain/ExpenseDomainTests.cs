using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public class ExpenseDomainTests
{
    private Guid _expenseId;
    private Guid _expenseTypeId;
    private Guid _expenseAccountId;
    private Guid _cashAccountId;
    private Guid _userId;

    [SetUp]
    public void Setup()
    {
        _expenseId = Guid.NewGuid();
        _expenseTypeId = Guid.NewGuid();
        _expenseAccountId = Guid.NewGuid();
        _cashAccountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    [Test]
    public void Create_ValidExpense_InitializesDraftExpense()
    {
        var expense = Expense.Create(
            _expenseId,
            "EXP-2026-0001",
            new DateOnly(2026, 1, 20),
            _expenseTypeId,
            _expenseAccountId,
            "فواتير كهرباء وإنترنت",
            450m,
            PaymentMethod.Cash,
            _cashAccountId,
            null,
            "مصاريف مرافق شهر يناير",
            ExpenseStatus.Draft,
            null,
            _userId,
            DateTime.UtcNow);

        Assert.That(expense.Id, Is.EqualTo(_expenseId));
        Assert.That(expense.ExpenseNumber, Is.EqualTo("EXP-2026-0001"));
        Assert.That(expense.Amount, Is.EqualTo(450m));
        Assert.That(expense.Status, Is.EqualTo(ExpenseStatus.Draft));
        Assert.That(expense.Beneficiary, Is.EqualTo("فواتير كهرباء وإنترنت"));
    }

    [Test]
    public void Approve_DraftExpense_SetsStatusToApproved()
    {
        var expense = Expense.Create(
            _expenseId, "EXP-2026-0001", new DateOnly(2026, 1, 20),
            _expenseTypeId, _expenseAccountId, "المورد", 300m,
            PaymentMethod.Cash, _cashAccountId, null, "مصروف",
            ExpenseStatus.Draft, null, _userId, DateTime.UtcNow);

        expense.Approve();

        Assert.That(expense.Status, Is.EqualTo(ExpenseStatus.Approved));
    }

    [Test]
    public void Post_ApprovedExpense_SetsStatusToPosted()
    {
        var expense = Expense.Create(
            _expenseId, "EXP-2026-0001", new DateOnly(2026, 1, 20),
            _expenseTypeId, _expenseAccountId, "المورد", 300m,
            PaymentMethod.Cash, _cashAccountId, null, "مصروف",
            ExpenseStatus.Draft, null, _userId, DateTime.UtcNow);

        expense.Approve();

        var journalId = Guid.NewGuid();
        var postedAt = DateTime.UtcNow;
        expense.SetJournalEntry(journalId);
        expense.Post(postedAt);

        Assert.That(expense.Status, Is.EqualTo(ExpenseStatus.Posted));
        Assert.That(expense.JournalEntryId, Is.EqualTo(journalId));
        Assert.That(expense.PostedAtUtc, Is.EqualTo(postedAt));
    }
}
