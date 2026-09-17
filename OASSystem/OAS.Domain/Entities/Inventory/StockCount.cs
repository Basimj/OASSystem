using OAS.Domain.Common.Entities;
using OAS.Domain.Enums.Inventory;

namespace OAS.Domain.Entities.Inventory;

public class StockCount : AuditableEntity<Guid>
{
    public string CountNumber { get; private set; } = null!;
    public Guid WarehouseId { get; private set; }

    public StockCountStatus Status { get; private set; }

    public DateOnly CountDate { get; private set; }

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
        DateOnly countDate,
        string? notes = null)
    {
        Id = Guid.NewGuid();

        CountNumber = countNumber;
        WarehouseId = warehouseId;
        CountDate = countDate;

        Status = StockCountStatus.Draft;

        Notes = notes;
    }

    public void Start(DateTimeOffset startedAtUtc)
    {
        if (Status != StockCountStatus.Draft)
            return;

        Status = StockCountStatus.Counting;
        StartedAtUtc = startedAtUtc;
    }

    public void Complete(DateTimeOffset completedAtUtc)
    {
        if (Status != StockCountStatus.Counting)
            return;

        Status = StockCountStatus.Review;
        CompletedAtUtc = completedAtUtc;
    }

    public void Approve(
        DateTimeOffset approvedAtUtc,
        string? approvedBy)
    {
        if (Status != StockCountStatus.Review)
            return;

        Status = StockCountStatus.Approved;
        ApprovedAtUtc = approvedAtUtc;
        ApprovedBy = approvedBy;
    }

    public void Post(
        DateTimeOffset postedAtUtc,
        string? postedBy)
    {
        if (Status != StockCountStatus.Approved)
            return;

        Status = StockCountStatus.Posted;
        PostedAtUtc = postedAtUtc;
        PostedBy = postedBy;
    }

    public void Cancel()
    {
        if (Status == StockCountStatus.Posted)
            return;

        Status = StockCountStatus.Cancelled;
    }

}