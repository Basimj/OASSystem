using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class InventoryTransaction : AuditableEntity<Guid>
{
    public string TransactionNumber { get; private set; } = null!;
    public string TransactionType { get; private set; } = null!;

    public Guid? SourceWarehouseId { get; private set; }
    public Guid? DestinationWarehouseId { get; private set; }

    public string Status { get; private set; } = null!;

    public DateTimeOffset TransactionDate { get; private set; }

    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }

    public string? Reason { get; private set; }
    public string? Notes { get; private set; }

    public DateTimeOffset? PostedAtUtc { get; private set; }
    public string? PostedBy { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private InventoryTransaction()
    {
    }

    public InventoryTransaction(
        string transactionNumber,
        string transactionType,
        DateTimeOffset transactionDate,
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        string? referenceType = null,
        Guid? referenceId = null,
        string? reason = null,
        string? notes = null)
    {
        Id = Guid.NewGuid();

        TransactionNumber = transactionNumber;
        TransactionType = transactionType;

        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;

        Status = "Draft";
        TransactionDate = transactionDate;

        ReferenceType = referenceType;
        ReferenceId = referenceId;

        Reason = reason;
        Notes = notes;
    }

    public void Post(DateTimeOffset postedAtUtc, string? postedBy)
    {
        if (Status == "Posted")
            return;

        Status = "Posted";
        PostedAtUtc = postedAtUtc;
        PostedBy = postedBy;
    }
}