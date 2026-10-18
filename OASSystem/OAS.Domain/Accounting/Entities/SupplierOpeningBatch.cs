using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class SupplierOpeningBatch : AuditableEntity<Guid>
{
    private readonly List<SupplierOpeningItem> _items = [];
    private SupplierOpeningBatch() { }

    private SupplierOpeningBatch(Guid id, string batchCode, DateOnly asOfDate, Guid baseCurrencyId, string? notes)
    {
        if (id == Guid.Empty) throw new ArgumentException("Supplier opening batch id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(batchCode)) throw new ArgumentException("Batch code is required.", nameof(batchCode));
        if (baseCurrencyId == Guid.Empty) throw new ArgumentException("Base currency is required.", nameof(baseCurrencyId));
        Id = id;
        BatchCode = batchCode.Trim();
        AsOfDate = asOfDate;
        BaseCurrencyId = baseCurrencyId;
        Notes = Normalize(notes);
        Status = SupplierOpeningBatchStatus.Draft;
    }

    public string BatchCode { get; private set; } = string.Empty;
    public DateOnly AsOfDate { get; private set; }
    public Guid BaseCurrencyId { get; private set; }
    public SupplierOpeningBatchStatus Status { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public string? Notes { get; private set; }
    public string? PostedBy { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<SupplierOpeningItem> Items => _items.AsReadOnly();

    public static SupplierOpeningBatch Create(Guid id, string batchCode, DateOnly asOfDate, Guid baseCurrencyId, string? notes = null)
        => new(id, batchCode, asOfDate, baseCurrencyId, notes);

    public void AddItem(SupplierOpeningItem item)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(item);
        if (item.BatchId != Id) throw new InvalidOperationException("Opening item does not belong to this batch.");
        if (_items.Any(x => x.Id == item.Id)) throw new InvalidOperationException("Opening item already exists.");
        if (!string.IsNullOrWhiteSpace(item.LegacyReference) &&
            _items.Any(x => x.SupplierId == item.SupplierId && string.Equals(x.LegacyReference, item.LegacyReference, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Duplicate supplier legacy reference is not allowed in the same opening batch.");
        _items.Add(item);
    }

    public void ReplaceItems(IEnumerable<SupplierOpeningItem> items)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(items);
        var materialized = items.ToArray();
        if (materialized.Any(x => x.BatchId != Id)) throw new InvalidOperationException("Opening item does not belong to this batch.");
        if (materialized.GroupBy(x => x.Id).Any(g => g.Count() > 1)) throw new InvalidOperationException("Duplicate opening item is not allowed.");
        if (materialized.Where(x => !string.IsNullOrWhiteSpace(x.LegacyReference))
            .GroupBy(x => new { x.SupplierId, Reference = x.LegacyReference!.ToUpperInvariant() }).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Duplicate supplier legacy reference is not allowed in the same opening batch.");
        _items.Clear();
        _items.AddRange(materialized);
    }

    public void MarkPosted(Guid journalEntryId, string postedBy, DateTime postedAtUtc)
    {
        EnsureDraft();
        EnsureItems();
        if (journalEntryId == Guid.Empty) throw new ArgumentException("Journal entry id is required.", nameof(journalEntryId));
        if (string.IsNullOrWhiteSpace(postedBy)) throw new ArgumentException("Posted by is required.", nameof(postedBy));
        JournalEntryId = journalEntryId;
        PostedBy = postedBy.Trim();
        PostedAtUtc = EnsureUtc(postedAtUtc);
        Status = SupplierOpeningBatchStatus.Posted;
    }

    public void LinkExistingJournal(Guid journalEntryId, string linkedBy, DateTime linkedAtUtc)
    {
        EnsureDraft();
        EnsureItems();
        if (journalEntryId == Guid.Empty) throw new ArgumentException("Journal entry id is required.", nameof(journalEntryId));
        if (string.IsNullOrWhiteSpace(linkedBy)) throw new ArgumentException("Linked by is required.", nameof(linkedBy));
        JournalEntryId = journalEntryId;
        PostedBy = linkedBy.Trim();
        PostedAtUtc = EnsureUtc(linkedAtUtc);
        Status = SupplierOpeningBatchStatus.LinkedExistingJournal;
    }

    private void EnsureDraft()
    {
        if (Status != SupplierOpeningBatchStatus.Draft)
            throw new InvalidOperationException("Only a draft supplier opening batch can be modified.");
    }

    private void EnsureItems()
    {
        if (_items.Count == 0) throw new InvalidOperationException("Supplier opening batch must contain at least one item.");
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
