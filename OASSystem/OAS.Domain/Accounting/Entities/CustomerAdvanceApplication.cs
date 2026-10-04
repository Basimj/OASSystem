using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class CustomerAdvanceApplication : AuditableEntity<Guid>
{
    private CustomerAdvanceApplication() { }

    private CustomerAdvanceApplication(
        Guid id,
        Guid customerAdvanceId,
        Guid salesInvoiceId,
        decimal amount,
        decimal baseAmount,
        decimal targetBaseAmount,
        Guid journalEntryId,
        DateTime appliedAtUtc,
        string? appliedBy)
    {
        if (id == Guid.Empty) throw new ArgumentException("Customer advance application id is required.", nameof(id));
        if (customerAdvanceId == Guid.Empty) throw new ArgumentException("Customer advance id is required.", nameof(customerAdvanceId));
        if (salesInvoiceId == Guid.Empty) throw new ArgumentException("Sales invoice id is required.", nameof(salesInvoiceId));
        if (journalEntryId == Guid.Empty) throw new ArgumentException("Journal entry id is required.", nameof(journalEntryId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (baseAmount <= 0) throw new ArgumentOutOfRangeException(nameof(baseAmount));
        if (targetBaseAmount <= 0) throw new ArgumentOutOfRangeException(nameof(targetBaseAmount));

        Id = id;
        CustomerAdvanceId = customerAdvanceId;
        SalesInvoiceId = salesInvoiceId;
        Amount = amount;
        BaseAmount = baseAmount;
        TargetBaseAmount = targetBaseAmount;
        JournalEntryId = journalEntryId;
        AppliedAtUtc = appliedAtUtc;
        AppliedBy = NormalizeUser(appliedBy);
    }

    public Guid CustomerAdvanceId { get; private set; }
    public Guid SalesInvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BaseAmount { get; private set; }
    public decimal TargetBaseAmount { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }
    public string? AppliedBy { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static CustomerAdvanceApplication Create(
        Guid id,
        Guid customerAdvanceId,
        Guid salesInvoiceId,
        decimal amount,
        decimal baseAmount,
        decimal targetBaseAmount,
        Guid journalEntryId,
        DateTime appliedAtUtc,
        string? appliedBy) =>
        new(id, customerAdvanceId, salesInvoiceId, amount, baseAmount, targetBaseAmount, journalEntryId, appliedAtUtc, appliedBy);

    private static string? NormalizeUser(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > 64) throw new ArgumentOutOfRangeException(nameof(value), "Applied by cannot exceed 64 characters.");
        return normalized;
    }
}
