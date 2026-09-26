using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PaymentVoucher : AuditableEntity<Guid>
{
    private readonly List<PaymentVoucherLine> _lines = [];
    private PaymentVoucher() { }

    public string VoucherNumber { get; private set; } = null!;
    public DateOnly VoucherDate { get; private set; }

    public PaymentPartyType PartyType { get; private set; }
    public Guid? SupplierId { get; private set; }
    public string? BeneficiaryName { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public Guid? CashAccountId { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public decimal TotalAmount { get; private set; }

    public Guid? BaseCurrencyId { get; private set; }
    public string? BaseCurrencyCodeSnapshot { get; private set; }
    public byte? BaseCurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal? BaseTotalAmount { get; private set; }

    public PaymentVoucherStatus Status { get; private set; }
    public string? Description { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public Guid? PostedBy { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<PaymentVoucherLine> Lines => _lines.AsReadOnly();

    public static PaymentVoucher CreateSettlementDocument(
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

        return new PaymentVoucher
        {
            Id = id,
            VoucherNumber = voucherNumber.Trim(),
            VoucherDate = voucherDate,
            PartyType = PaymentPartyType.Other,
            PaymentMethod = PaymentMethod.Other,
            TotalAmount = baseTotalAmount,
            BaseCurrencyId = baseCurrencyId,
            BaseCurrencyCodeSnapshot = baseCurrencyCodeSnapshot.Trim().ToUpperInvariant(),
            BaseCurrencyDecimalPlacesSnapshot = baseCurrencyDecimalPlacesSnapshot,
            BaseTotalAmount = baseTotalAmount,
            Status = PaymentVoucherStatus.Draft,
            Description = Normalize(description)
        };
    }

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
        ValidateIdentity(id, voucherNumber);
        ValidateAmount(totalAmount);

        return new PaymentVoucher
        {
            Id = id,
            VoucherNumber = voucherNumber.Trim(),
            VoucherDate = voucherDate,
            PartyType = partyType,
            SupplierId = Normalize(supplierId),
            BeneficiaryName = Normalize(beneficiaryName),
            PaymentMethod = paymentMethod,
            CashAccountId = Normalize(cashAccountId),
            BankAccountId = Normalize(bankAccountId),
            TotalAmount = totalAmount,
            Status = status,
            Description = Normalize(description),
            JournalEntryId = Normalize(journalEntryId)
        };
    }

    public void AddLine(PaymentVoucherLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        EnsureDraft();
        if (line.PaymentVoucherId != Id) throw new InvalidOperationException("Line does not belong to this payment voucher.");
        if (_lines.Any(x => x.Id == line.Id)) throw new InvalidOperationException("Payment voucher line already exists.");
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

    public void ReplaceLines(IEnumerable<PaymentVoucherLine> lines)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(lines);
        var newLines = lines.ToList();
        if (newLines.Any(x => x.PaymentVoucherId != Id)) throw new InvalidOperationException("All lines must belong to this payment voucher.");
        if (newLines.GroupBy(x => x.Id).Any(g => g.Count() > 1)) throw new InvalidOperationException("Duplicate payment voucher lines are not allowed.");
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
        PartyType = PaymentPartyType.Other;
        SupplierId = null;
        BeneficiaryName = null;
        PaymentMethod = PaymentMethod.Other;
        CashAccountId = null;
        BankAccountId = null;
        Description = Normalize(description);
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
        ValidateAmount(totalAmount);
        VoucherDate = voucherDate;
        PartyType = partyType;
        SupplierId = Normalize(supplierId);
        BeneficiaryName = Normalize(beneficiaryName);
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
        if (Status != PaymentVoucherStatus.Draft) throw new InvalidOperationException("Only draft payment vouchers can be approved.");
        EnsureReadyForPosting();
        Status = PaymentVoucherStatus.Approved;
    }

    public void Post(Guid postedBy, DateTime postedAtUtc)
    {
        if (postedBy == Guid.Empty) throw new ArgumentException("Posted by is required.", nameof(postedBy));
        if (Status != PaymentVoucherStatus.Approved) throw new InvalidOperationException("Only approved payment vouchers can be posted.");
        EnsureReadyForPosting();
        Status = PaymentVoucherStatus.Posted;
        PostedBy = postedBy;
        PostedAtUtc = postedAtUtc;
    }

    public void Cancel()
    {
        if (Status == PaymentVoucherStatus.Posted) throw new InvalidOperationException("Posted payment vouchers cannot be cancelled directly.");
        Status = PaymentVoucherStatus.Cancelled;
    }

    private void EnsureReadyForPosting()
    {
        if (_lines.Count == 0) throw new InvalidOperationException("A payment voucher must contain at least one line.");
        if (BaseCurrencyId.HasValue)
        {
            if (_lines.Any(x => !x.BaseAmount.HasValue || x.BaseAmount.Value <= 0)) throw new InvalidOperationException("All settlement lines must contain a valid base amount.");
            var expected = _lines.Sum(x => x.BaseAmount!.Value);
            if (BaseTotalAmount != expected) throw new InvalidOperationException("Payment voucher base total is inconsistent with its lines.");
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
        if (Status != PaymentVoucherStatus.Draft) throw new InvalidOperationException("Only draft payment vouchers can be modified.");
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
