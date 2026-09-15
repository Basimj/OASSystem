using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class ReceiptVoucher : Entity<Guid>
{
    private ReceiptVoucher()
    {
    }

    private ReceiptVoucher(
        Guid id,
        string voucherNumber,
        DateOnly voucherDate,
        ReceiptPartyType partyType,
        Guid? customerId,
        string? receivedFrom,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        decimal totalAmount,
        ReceiptVoucherStatus status,
        string? description,
        Guid? journalEntryId,
        Guid createdBy,
        DateTime createdAtUtc)
    {
        Id = id;
        VoucherNumber = voucherNumber;
        VoucherDate = voucherDate;
        PartyType = partyType;
        CustomerId = customerId;
        ReceivedFrom = receivedFrom;
        PaymentMethod = paymentMethod;
        CashAccountId = cashAccountId;
        BankAccountId = bankAccountId;
        TotalAmount = totalAmount;
        Status = status;
        Description = description;
        JournalEntryId = journalEntryId;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public string VoucherNumber { get; private set; } = null!;

    public DateOnly VoucherDate { get; private set; }

    public ReceiptPartyType PartyType { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string? ReceivedFrom { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public Guid? CashAccountId { get; private set; }

    public Guid? BankAccountId { get; private set; }

    public decimal TotalAmount { get; private set; }

    public ReceiptVoucherStatus Status { get; private set; }

    public string? Description { get; private set; }

    public Guid? JournalEntryId { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? PostedBy { get; private set; }

    public DateTime? PostedAtUtc { get; private set; }

    public static ReceiptVoucher Create(
        Guid id,
        string voucherNumber,
        DateOnly voucherDate,
        ReceiptPartyType partyType,
        Guid? customerId,
        string? receivedFrom,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        decimal totalAmount,
        ReceiptVoucherStatus status,
        string? description,
        Guid? journalEntryId,
        Guid createdBy,
        DateTime createdAtUtc)
    {
        ValidateAmount(totalAmount);

        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(voucherNumber))
            throw new ArgumentException(
                "Voucher number is required.",
                nameof(voucherNumber));

        if (createdBy == Guid.Empty)
            throw new ArgumentException("Created by is required.", nameof(createdBy));

        return new ReceiptVoucher(
            id,
            voucherNumber.Trim(),
            voucherDate,
            partyType,
            customerId,
            Normalize(receivedFrom),
            paymentMethod,
            cashAccountId,
            bankAccountId,
            totalAmount,
            status,
            Normalize(description),
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
        if (Status != ReceiptVoucherStatus.Draft)
            throw new InvalidOperationException(
                "Only draft receipt vouchers can be approved.");

        Status = ReceiptVoucherStatus.Approved;
    }

    public void Post(Guid postedBy, DateTime postedAtUtc)
    {
        if (postedBy == Guid.Empty)
            throw new ArgumentException("Posted by is required.", nameof(postedBy));

        if (Status != ReceiptVoucherStatus.Approved)
            throw new InvalidOperationException(
                "Only approved receipt vouchers can be posted.");

        Status = ReceiptVoucherStatus.Posted;
        PostedBy = postedBy;
        PostedAtUtc = postedAtUtc;
    }

    public void Cancel()
    {
        if (Status == ReceiptVoucherStatus.Posted)
            throw new InvalidOperationException(
                "Posted receipt vouchers cannot be cancelled directly.");

        Status = ReceiptVoucherStatus.Cancelled;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Total amount must be greater than zero.");
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}