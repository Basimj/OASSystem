using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PaymentVoucher : AuditableEntity<Guid>
{
    private PaymentVoucher()
    {
    }

    private PaymentVoucher(
        Guid id,
        string voucherNumber,
        DateOnly voucherDate,
        PaymentPartyType partyType,
        Guid? supplierId,
        string? beneficiaryName,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        decimal totalAmount,
        PaymentVoucherStatus status,
        string? description,
        Guid? journalEntryId)
    {
        Id = id;
        VoucherNumber = voucherNumber;
        VoucherDate = voucherDate;
        PartyType = partyType;
        SupplierId = supplierId;
        BeneficiaryName = beneficiaryName;
        PaymentMethod = paymentMethod;
        CashAccountId = cashAccountId;
        BankAccountId = bankAccountId;
        TotalAmount = totalAmount;
        Status = status;
        Description = description;
        JournalEntryId = journalEntryId;
    }

    public string VoucherNumber { get; private set; } = null!;

    public DateOnly VoucherDate { get; private set; }

    public PaymentPartyType PartyType { get; private set; }

    public Guid? SupplierId { get; private set; }

    public string? BeneficiaryName { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public Guid? CashAccountId { get; private set; }

    public Guid? BankAccountId { get; private set; }

    public decimal TotalAmount { get; private set; }

    public PaymentVoucherStatus Status { get; private set; }

    public string? Description { get; private set; }

    public Guid? JournalEntryId { get; private set; }


    public Guid? PostedBy { get; private set; }

    public DateTime? PostedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private readonly List<PaymentVoucherLine> _lines = [];
    public IReadOnlyCollection<PaymentVoucherLine> Lines => _lines.AsReadOnly();

    public static PaymentVoucher Create(
        Guid id,
        string voucherNumber,
        DateOnly voucherDate,
        PaymentPartyType partyType,
        Guid? supplierId,
        string? beneficiaryName,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        decimal totalAmount,
        PaymentVoucherStatus status,
        string? description,
        Guid? journalEntryId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(voucherNumber))
            throw new ArgumentException(
                "Voucher number is required.",
                nameof(voucherNumber));

        if (totalAmount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(totalAmount),
                "Total amount must be greater than zero.");

        return new PaymentVoucher(
            id,
            voucherNumber.Trim(),
            voucherDate,
            partyType,
            supplierId,
            Normalize(beneficiaryName),
            paymentMethod,
            cashAccountId,
            bankAccountId,
            totalAmount,
            status,
            Normalize(description),
            journalEntryId);
    }

    public void AddLine(PaymentVoucherLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        EnsureDraft();

        if (line.PaymentVoucherId != Id)
            throw new InvalidOperationException("Line does not belong to this payment voucher.");

        _lines.Add(line);
    }

    public void ClearLines()
    {
        EnsureDraft();
        _lines.Clear();
    }

    public void ReplaceLines(IEnumerable<PaymentVoucherLine> lines)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(lines);
        _lines.Clear();
        foreach (var line in lines)
        {
            if (line.PaymentVoucherId != Id)
                throw new InvalidOperationException("Line does not belong to this payment voucher.");
            _lines.Add(line);
        }
    }

    public void UpdateDetails(
        DateOnly voucherDate,
        PaymentPartyType partyType,
        Guid? supplierId,
        string? beneficiaryName,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        decimal totalAmount,
        string? description)
    {
        EnsureDraft();
        if (totalAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total amount must be greater than zero.");

        VoucherDate = voucherDate;
        PartyType = partyType;
        SupplierId = supplierId;
        BeneficiaryName = Normalize(beneficiaryName);
        PaymentMethod = paymentMethod;
        CashAccountId = cashAccountId;
        BankAccountId = bankAccountId;
        TotalAmount = totalAmount;
        Description = Normalize(description);
    }

    public void SetJournalEntry(Guid journalEntryId)
    {
        if (journalEntryId == Guid.Empty)
            throw new ArgumentException(
                "Journal entry id is required.",
                nameof(journalEntryId));

        JournalEntryId = journalEntryId;
    }

    private void EnsureDraft()
    {
        if (Status != PaymentVoucherStatus.Draft)
            throw new InvalidOperationException("Only draft payment vouchers can be modified.");
    }

    public void Approve()
    {
        if (Status != PaymentVoucherStatus.Draft)
            throw new InvalidOperationException(
                "Only draft payment vouchers can be approved.");

        Status = PaymentVoucherStatus.Approved;
    }

    public void Post(Guid postedBy, DateTime postedAtUtc)
    {
        if (postedBy == Guid.Empty)
            throw new ArgumentException("Posted by is required.", nameof(postedBy));

        if (Status != PaymentVoucherStatus.Approved)
            throw new InvalidOperationException(
                "Only approved payment vouchers can be posted.");

        Status = PaymentVoucherStatus.Posted;
        PostedBy = postedBy;
        PostedAtUtc = postedAtUtc;
    }

    public void Cancel()
    {
        if (Status == PaymentVoucherStatus.Posted)
            throw new InvalidOperationException(
                "Posted payment vouchers cannot be cancelled directly.");

        Status = PaymentVoucherStatus.Cancelled;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
