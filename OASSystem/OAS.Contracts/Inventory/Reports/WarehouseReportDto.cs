namespace OAS.Contracts.Inventory.Reports;

public sealed record WarehouseReportWarehouse(Guid Id, string Code, string Name, bool IsActive);
public sealed record WarehouseReportStock(Guid WarehouseId, Guid VariantId, decimal OnHand,
    decimal? Reserved, decimal? OnOrder, decimal? AverageCost, decimal? Value, DateTimeOffset? LastMovement);
public sealed record WarehouseFlow(decimal? Opening, decimal Purchases, decimal OtherIn, decimal TransferIn,
    decimal SalesReturns, decimal Sales, decimal PurchaseReturns, decimal TransferOut, decimal OtherOut,
    decimal? Closing)
{
    public decimal In => Purchases + OtherIn + TransferIn + SalesReturns;
    public decimal Out => Sales + PurchaseReturns + TransferOut + OtherOut;
}
public sealed record WarehouseReportMovement(Guid WarehouseId, Guid VariantId, WarehouseFlow Quantity, WarehouseFlow Cost);
public sealed record WarehouseReportTransfer(Guid Id, string Number, DateTimeOffset Date, Guid? SourceId,
    Guid? DestinationId, Guid VariantId, decimal Quantity, decimal? Cost, int Status);
public sealed record WarehouseReportCount(Guid Id, string Number, DateOnly Date, Guid WarehouseId, Guid VariantId,
    decimal SystemQuantity, decimal? CountedQuantity, decimal? Difference, decimal AverageCost, decimal? Variance, int Status);
public sealed record WarehouseReportDto(IReadOnlyList<WarehouseReportWarehouse> Warehouses,
    IReadOnlyList<MovementReportOption> Categories, IReadOnlyList<MovementReportItem> Items,
    IReadOnlyList<WarehouseReportStock> Stocks, IReadOnlyList<WarehouseReportMovement> Movements,
    IReadOnlyList<WarehouseReportTransfer> Transfers, IReadOnlyList<WarehouseReportCount> Counts);
