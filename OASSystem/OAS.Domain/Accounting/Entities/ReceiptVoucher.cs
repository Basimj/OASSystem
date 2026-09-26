using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class ReceiptVoucher : AuditableEntity<Guid>
{
    private readonly List<ReceiptVoucherLine> _lines = [];
    private ReceiptVoucher() { }

    public string VoucherNumber { get; private set; } = null!;
    public DateOnly VoucherDate { get; private set; }

    // Legacy header fields are kept during Expand/Backfill. New multi-currency flows
    // derive settlement information from Lines and use Other/Other as neutral legacy values.
    public ReceiptPartyType PartyType { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string? ReceivedFrom { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public Guid? CashAccountId { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public decimal TotalAmount { get; private set; }

    public Guid? BaseCurrencyId { get; private set; }
    public string? BaseCurrencyCodeSnapshot { get; private set; }
    public byte? BaseCurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal? BaseTotalAmount { get; private set; }

    public ReceiptVoucherStatus Status { get; private set; }
    public string? Description { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public Guid? PostedBy { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<ReceiptVoucherLine> Lines => _lines.AsReadOnly();

    public static ReceiptVoucher CreateSettlementDocument(
        Guid id,
        string voucherNumber,
        DateOnly voucherDate,
        Guid baseCurrencyId,
        string baseCurrencyCodeSnapshot,
        byte baseCurrencyDecimalPlacesSnapshot,
        decimal baseTotalAmount,
        string? description)
    {
        ValidateIdentity(id, voucherNumber);
        ValidateBaseCurrency(baseCurrencyId, baseCurrencyCodeSnapshot, baseCurrencyDecimalPlacesSnapshot, baseTotalAmount);

        return new ReceiptVoucher
        {
            Id = id,
            VoucherNumber = voucherNumber.Trim(),
            VoucherDate = voucherDate,
            PartyType = ReceiptPartyType.Other,
            PaymentMethod = PaymentMethod.Other,
            TotalAmount = baseTotalAmount,
            BaseCurrencyId = baseCurrencyId,
            BaseCurrencyCodeSnapshot = baseCurrencyCodeSnapshot.Trim().ToUpperInvariant(),
            BaseCurrencyDecimalPlacesSnapshot = baseCurrencyDecimalPlacesSnapshot,
            BaseTotalAmount = baseTotalAmount,
            Status = ReceiptVoucherStatus.Draft,
            Description = Normalize(description)
        };
    }

    // Compatibility factory used by legacy data/tests while Expand migration is in place.
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
        Guid? journalEntryId)
    {
        ValidateIdentity(id, voucherNumber);
        ValidateAmount(totalAmount);

        return new ReceiptVoucher
        {
            Id = id,
            VoucherNumber = voucherNumber.Trim(),
            VoucherDate = voucherDate,
            PartyType = partyType,
            CustomerId = Normalize(customerId),
            ReceivedFrom = Normalize(receivedFrom),
            PaymentMethod = paymentMethod,
            CashAccountId = Normalize(cashAccountId),
            BankAccountId = Normalize(bankAccountId),
            TotalAmount = totalAmount,
            Status = status,
            Description = Normalize(description),
            JournalEntryId = Normalize(journalEntryId)
        };
    }

    public void AddLine(ReceiptVoucherLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        EnsureDraft();
        if (line.ReceiptVoucherId != Id) throw new InvalidOperationException("Line does not belong to this receipt voucher.");
        if (_lines.Any(x => x.Id == line.Id)) throw new InvalidOperationException("Receipt voucher line already exists.");
        _lines.Add(line);
        ReNumberLines();
        RecalculateBaseTotalFromSettlementLines();
    }

    public void ClearLines()
    {
        EnsureDraft();
        _lines.Clear();
        BaseTotalAmount = 0m;
        TotalAmount = 0m;
    }

    public void ReplaceLines(IEnumerable<ReceiptVoucherLine> lines)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(lines);
        var newLines = lines.ToList();
        if (newLines.Any(x => x.ReceiptVoucherId != Id)) throw new InvalidOperationException("All lines must belong to this receipt voucher.");
        if (newLines.GroupBy(x => x.Id).Any(g => g.Count() > 1)) throw new InvalidOperationException("Duplicate receipt voucher lines are not allowed.");
        _lines.Clear();
        _lines.AddRange(newLines);
        ReNumberLines();
        RecalculateBaseTotalFromSettlementLines();
    }

    public void UpdateSettlementDocument(
        DateOnly voucherDate,
        Guid baseCurrencyId,
        string baseCurrencyCodeSnapshot,
        byte baseCurrencyDecimalPlacesSnapshot,
        decimal baseTotalAmount,
        string? description)
    {
        EnsureDraft();
        ValidateBaseCurrency(baseCurrencyId, baseCurrencyCodeSnapshot, baseCurrencyDecimalPlacesSnapshot, baseTotalAmount);
        VoucherDate = voucherDate;
        BaseCurrencyId = baseCurrencyId;
        BaseCurrencyCodeSnapshot = baseCurrencyCodeSnapshot.Trim().ToUpperInvariant();
        BaseCurrencyDecimalPlacesSnapshot = baseCurrencyDecimalPlacesSnapshot;
        BaseTotalAmount = baseTotalAmount;
        TotalAmount = baseTotalAmount;
        PartyType = ReceiptPartyType.Other;
        CustomerId = null;
        ReceivedFrom = null;
        PaymentMethod = PaymentMethod.Other;
        CashAccountId = null;
        BankAccountId = null;
        Description = Normalize(description);
    }

    public void UpdateDetails(
        DateOnly voucherDate,
        ReceiptPartyType partyType,
        Guid? customerId,
        string? receivedFrom,
        PaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        decimal totalAmount,
        string? description)
    {
        EnsureDraft();
        ValidateAmount(totalAmount);
        VoucherDate = voucherDate;
        PartyType = partyType;
        CustomerId = Normalize(customerId);
        ReceivedFrom = Normalize(receivedFrom);
        PaymentMethod = paymentMethod;
        CashAccountId = Normalize(cashAccountId);
        BankAccountId = Normalize(bankAccountId);
        TotalAmount = totalAmount;
        Description = Normalize(description);
    }

    public void SetJournalEntry(Guid journalEntryId)
    {
        if (journalEntryId == Guid.Empty) throw new ArgumentException("Journal entry id is required.", nameof(journalEntryId));
        JournalEntryId = journalEntryId;
    }

    public void Approve()
    {
        if (Status != ReceiptVoucherStatus.Draft) throw new InvalidOperationException("Only draft receipt vouchers can be approved.");
        EnsureReadyForPosting();
        Status = ReceiptVoucherStatus.Approved;
    }

    public void Post(Guid postedBy, DateTime postedAtUtc)
    {
        if (postedBy == Guid.Empty) throw new ArgumentException("Posted by is required.", nameof(postedBy));
        if (Status != ReceiptVoucherStatus.Approved) throw new InvalidOperationException("Only approved receipt vouchers can be posted.");
        EnsureReadyForPosting();
        Status = ReceiptVoucherStatus.Posted;
        PostedBy = postedBy;
        PostedAtUtc = postedAtUtc;
    }

    public void Cancel()
    {
        if (Status == ReceiptVoucherStatus.Posted) throw new InvalidOperationException("Posted receipt vouchers cannot be cancelled directly.");
        Status = ReceiptVoucherStatus.Cancelled;
    }

    private void EnsureReadyForPosting()
    {
        if (_lines.Count == 0) throw new InvalidOperationException("A receipt voucher must contain at least one line.");
        if (BaseCurrencyId.HasValue)
        {
            if (_lines.Any(x => !x.BaseAmount.HasValue || x.BaseAmount.Value <= 0)) throw new InvalidOperationException("All settlement lines must contain a valid base amount.");
            var expected = _lines.Sum(x => x.BaseAmount!.Value);
            if (BaseTotalAmount != expected) throw new InvalidOperationException("Receipt voucher base total is inconsistent with its lines.");
        }
    }

    private void RecalculateBaseTotalFromSettlementLines()
    {
        if (_lines.Count == 0) return;
        if (_lines.All(x => x.BaseAmount.HasValue))
        {
            var total = _lines.Sum(x => x.BaseAmount!.Value);
            BaseTotalAmount = total;
            TotalAmount = total;
        }
    }

    private void ReNumberLines()
    {
        for (var i = 0; i < _lines.Count; i++) _lines[i].SetLineNumber(i + 1);
    }

    private void EnsureDraft()
    {
        if (Status != ReceiptVoucherStatus.Draft) throw new InvalidOperationException("Only draft receipt vouchers can be modified.");
    }

    private static void ValidateIdentity(Guid id, string voucherNumber)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(voucherNumber)) throw new ArgumentException("Voucher number is required.", nameof(voucherNumber));
    }

    private static void ValidateBaseCurrency(Guid id, string code, byte decimalPlaces, decimal total)
    {
        if (id == Guid.Empty) throw new ArgumentException("Base currency is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Base currency snapshot is required.", nameof(code));
        if (decimalPlaces > 6) throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
        if (total <= 0) throw new ArgumentOutOfRangeException(nameof(total), "Base total amount must be greater than zero.");
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Total amount must be greater than zero.");
    }

    private static Guid? Normalize(Guid? value) => value is { } id && id != Guid.Empty ? id : null;
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
