using OAS.Domain.Common.Entities;
using OAS.Domain.Enums.Inventory;

namespace OAS.Domain.Entities.Inventory;

public class InventoryTransaction : AuditableEntity<Guid>
{
    public string TransactionNumber { get; private set; } = null!;
    public InventoryTransactionType TransactionType { get; private set; }

    public Guid? SourceWarehouseId { get; private set; }
    public Guid? DestinationWarehouseId { get; private set; }

    public InventoryTransactionStatus Status { get; private set; }

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
        InventoryTransactionType transactionType,
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

        Status = InventoryTransactionStatus.Draft;
        TransactionDate = transactionDate;

        ReferenceType = referenceType;
        ReferenceId = referenceId;

        Reason = reason;
        Notes = notes;
    }

    public void Post(DateTimeOffset postedAtUtc, string? postedBy)
    {
        if (Status == InventoryTransactionStatus.Posted)
            return;

        Status = InventoryTransactionStatus.Posted;
        PostedAtUtc = postedAtUtc;
        PostedBy = postedBy;
    }
}