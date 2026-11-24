namespace OAS.Contracts.Inventory.Reports;

public sealed record MovementReportOption(Guid Id, string Code, string Name);
public sealed record MovementReportItem(Guid Id, Guid ProductId, Guid CategoryId, string Code,
    string Name, string ProductCode, string ProductName, Guid? UnitId, string UnitName);

// Null opening/closing means that supplier-level stock provenance is not available.
public sealed record MovementReportAmounts(decimal? Opening, decimal Purchases, decimal OtherIn,
    decimal PurchaseReturns, decimal OtherOut, decimal Sales, decimal SalesReturns, decimal? Closing)
{
    public decimal In => Purchases + OtherIn + SalesReturns;
    public decimal Out => PurchaseReturns + OtherOut + Sales;
}
public sealed record SupplierProductMovementRow(Guid VariantId, Guid? SupplierId,
    MovementReportAmounts Quantity, MovementReportAmounts Cost);
public sealed record SupplierProductMovementReportDto(
    IReadOnlyList<MovementReportOption> Suppliers, IReadOnlyList<MovementReportOption> Categories,
    IReadOnlyList<MovementReportOption> Warehouses, IReadOnlyList<MovementReportItem> Items,
    IReadOnlyList<SupplierProductMovementRow> ProductRows,
    IReadOnlyList<SupplierProductMovementRow> SupplierRows);
