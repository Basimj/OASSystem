using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class StockCountLine : Entity<Guid>
{
    public Guid StockCountId { get; private set; }
    public Guid ProductVariantId { get; private set; }

    public decimal SystemQuantity { get; private set; }
    public decimal CountedQuantity { get; private set; }
    public decimal DifferenceQuantity { get; private set; }

    public decimal AverageCostSnapshot { get; private set; }
    public decimal VarianceValue { get; private set; }

    public DateTimeOffset? CountedAtUtc { get; private set; }
    public string? CountedBy { get; private set; }

    public string? Notes { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private StockCountLine()
    {
    }

    public StockCountLine(
        Guid stockCountId,
        Guid productVariantId,
        decimal systemQuantity,
        decimal averageCostSnapshot,
        string? notes = null)
    {
        Id = Guid.NewGuid();

        StockCountId = stockCountId;
        ProductVariantId = productVariantId;

        SystemQuantity = systemQuantity;
        AverageCostSnapshot = averageCostSnapshot;

        Notes = notes;
    }

    public void RecordCount(
        decimal countedQuantity,
        DateTimeOffset countedAtUtc,
        string? countedBy)
    {
        CountedQuantity = countedQuantity;
        DifferenceQuantity = countedQuantity - SystemQuantity;

        VarianceValue = DifferenceQuantity * AverageCostSnapshot;

        CountedAtUtc = countedAtUtc;
        CountedBy = countedBy;
    }
}