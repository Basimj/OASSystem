using OAS.Domain.Common.Entities;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Entities.Inventory;

public sealed class StockReservation : AuditableEntity<Guid>
{
    private StockReservation() { }

    private StockReservation(
        Guid id, Guid productVariantId, Guid warehouseId, decimal quantity, string sourceModule,
        string sourceDocumentType, Guid sourceDocumentId, Guid sourceLineId, DateTimeOffset reservedAtUtc)
    {
        if (id == Guid.Empty) throw new DomainException("Stock reservation id is required.");
        if (productVariantId == Guid.Empty) throw new DomainException("Product variant id is required.");
        if (warehouseId == Guid.Empty) throw new DomainException("Warehouse id is required.");
        if (quantity <= 0) throw new DomainException("Reservation quantity must be greater than zero.");
        if (sourceDocumentId == Guid.Empty) throw new DomainException("Source document id is required.");
        if (sourceLineId == Guid.Empty) throw new DomainException("Source line id is required.");

        Id = id;
        ProductVariantId = productVariantId;
        WarehouseId = warehouseId;
        Quantity = quantity;
        SourceModule = NormalizeRequired(sourceModule, 50, "Source module");
        SourceDocumentType = NormalizeRequired(sourceDocumentType, 50, "Source document type");
        SourceDocumentId = sourceDocumentId;
        SourceLineId = sourceLineId;
        Status = StockReservationStatus.Active;
        ReservedAtUtc = reservedAtUtc;
        IsActive = true;
    }

    public Guid ProductVariantId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal Quantity { get; private set; }
    public string SourceModule { get; private set; } = string.Empty;
    public string SourceDocumentType { get; private set; } = string.Empty;
    public Guid SourceDocumentId { get; private set; }
    public Guid SourceLineId { get; private set; }
    public StockReservationStatus Status { get; private set; }
    public DateTimeOffset ReservedAtUtc { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static StockReservation Create(
        Guid id, Guid productVariantId, Guid warehouseId, decimal quantity, string sourceModule,
        string sourceDocumentType, Guid sourceDocumentId, Guid sourceLineId, DateTimeOffset reservedAtUtc) =>
        new(id, productVariantId, warehouseId, quantity, sourceModule, sourceDocumentType, sourceDocumentId, sourceLineId, reservedAtUtc);

    public void Release(DateTimeOffset releasedAtUtc)
    {
        EnsureActive();
        Status = StockReservationStatus.Released;
        ReleasedAtUtc = releasedAtUtc;
        IsActive = false;
    }

    public void Consume(DateTimeOffset consumedAtUtc)
    {
        EnsureActive();
        Status = StockReservationStatus.Consumed;
        ConsumedAtUtc = consumedAtUtc;
        IsActive = false;
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        EnsureActive();
        Status = StockReservationStatus.Cancelled;
        ReleasedAtUtc = cancelledAtUtc;
        IsActive = false;
    }

    private void EnsureActive()
    {
        if (Status != StockReservationStatus.Active || !IsActive)
            throw new DomainException("Only active stock reservations can be changed.");
    }

    private static string NormalizeRequired(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{name} is required.");
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new DomainException($"{name} cannot exceed {maxLength} characters.");
        return normalized;
    }
}
