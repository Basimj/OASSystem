using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class StockCount : AuditableEntity<Guid>
{
    public string CountNumber { get; private set; } = null!;
    public Guid WarehouseId { get; private set; }

    public string Status { get; private set; } = null!;

    public DateTimeOffset CountDate { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }

    public DateTimeOffset? PostedAtUtc { get; private set; }
    public string? PostedBy { get; private set; }

    public string? Notes { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private StockCount()
    {
    }

    public StockCount(
        string countNumber,
        Guid warehouseId,
        DateTimeOffset countDate,
        string? notes = null)
    {
        Id = Guid.NewGuid();

        CountNumber = countNumber;
        WarehouseId = warehouseId;
        CountDate = countDate;
        Notes = notes;

        Status = "Draft";
    }

    public void Start(DateTimeOffset startedAtUtc)
    {
        if (Status != "Draft")
            return;

        Status = "Counting";
        StartedAtUtc = startedAtUtc;
    }

    public void Complete(DateTimeOffset completedAtUtc)
    {
        if (Status != "Counting")
            return;

        Status = "Review";
        CompletedAtUtc = completedAtUtc;
    }

    public void Approve(DateTimeOffset approvedAtUtc, string? approvedBy)
    {
        if (Status != "Review")
            return;

        Status = "Approved";
        ApprovedAtUtc = approvedAtUtc;
        ApprovedBy = approvedBy;
    }

    public void Post(DateTimeOffset postedAtUtc, string? postedBy)
    {
        if (Status != "Approved")
            return;

        Status = "Posted";
        PostedAtUtc = postedAtUtc;
        PostedBy = postedBy;
    }
}