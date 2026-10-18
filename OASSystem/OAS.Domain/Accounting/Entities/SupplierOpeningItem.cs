using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class SupplierOpeningItem : AuditableEntity<Guid>
{
    private SupplierOpeningItem() { }

    private SupplierOpeningItem(
        Guid id,
        Guid batchId,
        Guid supplierId,
        string? legacyReference,
        DateOnly documentDate,
        DateOnly? dueDate,
        SupplierOpeningDueDateProvenance dueDateProvenance,
        Guid currencyId,
        string currencyCodeSnapshot,
        byte currencyDecimalPlacesSnapshot,
        decimal originalOpenAmount,
        decimal exchangeRateSnapshot,
        decimal baseOpenAmount)
    {
        if (id == Guid.Empty) throw new ArgumentException("Opening item id is required.", nameof(id));
        if (batchId == Guid.Empty) throw new ArgumentException("Opening batch id is required.", nameof(batchId));
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier id is required.", nameof(supplierId));
        if (currencyId == Guid.Empty) throw new ArgumentException("Currency id is required.", nameof(currencyId));
        if (string.IsNullOrWhiteSpace(currencyCodeSnapshot)) throw new ArgumentException("Currency snapshot is required.", nameof(currencyCodeSnapshot));
        if (currencyDecimalPlacesSnapshot > 6) throw new ArgumentOutOfRangeException(nameof(currencyDecimalPlacesSnapshot));
        if (originalOpenAmount <= 0m) throw new ArgumentOutOfRangeException(nameof(originalOpenAmount));
        if (exchangeRateSnapshot <= 0m) throw new ArgumentOutOfRangeException(nameof(exchangeRateSnapshot));
        if (baseOpenAmount <= 0m) throw new ArgumentOutOfRangeException(nameof(baseOpenAmount));
        if (!Enum.IsDefined(dueDateProvenance)) throw new ArgumentOutOfRangeException(nameof(dueDateProvenance));
        if (dueDateProvenance == SupplierOpeningDueDateProvenance.Unknown && dueDate.HasValue)
            throw new ArgumentException("Unknown opening due-date provenance cannot contain a due date.", nameof(dueDate));
        if (dueDateProvenance != SupplierOpeningDueDateProvenance.Unknown && !dueDate.HasValue)
            throw new ArgumentException("Due date is required when opening due-date provenance is known.", nameof(dueDate));
        if (dueDate.HasValue && dueDate.Value < documentDate)
            throw new ArgumentException("Due date cannot be before opening document date.", nameof(dueDate));

        Id = id;
        BatchId = batchId;
        SupplierId = supplierId;
        LegacyReference = Normalize(legacyReference, 100);
        DocumentDate = documentDate;
        DueDate = dueDate;
        DueDateProvenance = dueDateProvenance;
        CurrencyId = currencyId;
        CurrencyCodeSnapshot = currencyCodeSnapshot.Trim().ToUpperInvariant();
        CurrencyDecimalPlacesSnapshot = currencyDecimalPlacesSnapshot;
        OriginalOpenAmount = Round(originalOpenAmount);
        ExchangeRateSnapshot = Math.Round(exchangeRateSnapshot, 8, MidpointRounding.AwayFromZero);
        BaseOpenAmount = Round(baseOpenAmount);
    }

    public Guid BatchId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string? LegacyReference { get; private set; }
    public DateOnly DocumentDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public SupplierOpeningDueDateProvenance DueDateProvenance { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; } = string.Empty;
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal OriginalOpenAmount { get; private set; }
    public decimal ExchangeRateSnapshot { get; private set; }
    public decimal BaseOpenAmount { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SupplierOpeningItem Create(
        Guid id,
        Guid batchId,
        Guid supplierId,
        string? legacyReference,
        DateOnly documentDate,
        DateOnly? dueDate,
        SupplierOpeningDueDateProvenance dueDateProvenance,
        Guid currencyId,
        string currencyCodeSnapshot,
        byte currencyDecimalPlacesSnapshot,
        decimal originalOpenAmount,
        decimal exchangeRateSnapshot,
        decimal baseOpenAmount) =>
        new(id, batchId, supplierId, legacyReference, documentDate, dueDate, dueDateProvenance,
            currencyId, currencyCodeSnapshot, currencyDecimalPlacesSnapshot,
            originalOpenAmount, exchangeRateSnapshot, baseOpenAmount);

    public void VerifyDueDate(DateOnly dueDate)
    {
        if (dueDate < DocumentDate) throw new ArgumentException("Due date cannot be before opening document date.", nameof(dueDate));
        DueDate = dueDate;
        DueDateProvenance = SupplierOpeningDueDateProvenance.VerifiedHistorical;
    }

    private static decimal Round(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentOutOfRangeException(nameof(value));
        return normalized;
    }
}
