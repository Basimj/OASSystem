using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class Expense : Entity<Guid>
{
    private Expense()
    {
    }

    private Expense(
        Guid id,
        string expenseNumber,
        DateOnly expenseDate,
        Guid expenseTypeId,
        Guid expenseAccountId,
        string? beneficiary,
        decimal amount,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        string? description,
        ExpenseStatus status,
        Guid? journalEntryId,
        Guid createdBy,
        DateTime createdAtUtc)
    {
        Id = id;
        ExpenseNumber = expenseNumber;
        ExpenseDate = expenseDate;
        ExpenseTypeId = expenseTypeId;
        ExpenseAccountId = expenseAccountId;
        Beneficiary = beneficiary;
        Amount = amount;
        PaymentMethod = paymentMethod;
        CashAccountId = cashAccountId;
        BankAccountId = bankAccountId;
        Description = description;
        Status = status;
        JournalEntryId = journalEntryId;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public string ExpenseNumber { get; private set; } = null!;

    public DateOnly ExpenseDate { get; private set; }

    public Guid ExpenseTypeId { get; private set; }

    public Guid ExpenseAccountId { get; private set; }

    public string? Beneficiary { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public Guid? CashAccountId { get; private set; }

    public Guid? BankAccountId { get; private set; }

    public string? Description { get; private set; }

    public ExpenseStatus Status { get; private set; }

    public Guid? JournalEntryId { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? PostedAtUtc { get; private set; }

    public static Expense Create(
        Guid id,
        string expenseNumber,
        DateOnly expenseDate,
        Guid expenseTypeId,
        Guid expenseAccountId,
        string? beneficiary,
        decimal amount,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        string? description,
        ExpenseStatus status,
        Guid? journalEntryId,
        Guid createdBy,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(expenseNumber))
            throw new ArgumentException(
                "Expense number is required.",
                nameof(expenseNumber));

        if (expenseTypeId == Guid.Empty)
            throw new ArgumentException(
                "Expense type id is required.",
                nameof(expenseTypeId));

        if (expenseAccountId == Guid.Empty)
            throw new ArgumentException(
                "Expense account id is required.",
                nameof(expenseAccountId));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Amount must be greater than zero.");

        if (createdBy == Guid.Empty)
            throw new ArgumentException(
                "Created by is required.",
                nameof(createdBy));

        return new Expense(
            id,
            expenseNumber.Trim(),
            expenseDate,
            expenseTypeId,
            expenseAccountId,
            Normalize(beneficiary),
            amount,
            paymentMethod,
            cashAccountId,
            bankAccountId,
            Normalize(description),
            status,
            journalEntryId,
            createdBy,
            createdAtUtc);
    }

    public void SetJournalEntry(Guid journalEntryId)
    {
        if (journalEntryId == Guid.Empty)
            throw new ArgumentException(
                "Journal entry id is required.",
                nameof(journalEntryId));

        JournalEntryId = journalEntryId;
    }

    public void Approve()
    {
        if (Status != ExpenseStatus.Draft)
            throw new InvalidOperationException(
                "Only draft expenses can be approved.");

        Status = ExpenseStatus.Approved;
    }

    public void Post(DateTime postedAtUtc)
    {
        if (Status != ExpenseStatus.Approved)
            throw new InvalidOperationException(
                "Only approved expenses can be posted.");

        Status = ExpenseStatus.Posted;
        PostedAtUtc = postedAtUtc;
    }

    public void Cancel()
    {
        if (Status == ExpenseStatus.Posted)
            throw new InvalidOperationException(
                "Posted expenses cannot be cancelled directly.");

        Status = ExpenseStatus.Cancelled;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}