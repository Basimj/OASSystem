using OAS.Contracts.Common.Pagination;

namespace OAS.Contracts.Inventory.Stock;

public sealed record InventoryBalanceFilters
{
    public Guid? WarehouseId { get; init; }
    public Guid? ProductTypeId { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string? ProductSearch { get; init; }
    public string? BalanceStatus { get; init; }
    public string? PurchaseStatus { get; init; }
    public string? ActivityStatus { get; init; }
}

public sealed class InventoryBalanceWorkspaceRow
{
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = "";
    public bool WarehouseActive { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public string ProductCode { get; init; } = "";
    public Guid VariantId { get; init; }
    public string? VariantName { get; init; }
    public string SKU { get; init; } = "";
    public string? Barcode { get; init; }
    public bool IsActive { get; init; }
    public Guid? UnitId { get; init; }
    public string? UnitName { get; init; }
    public decimal OnHand { get; init; }
    public decimal Reserved { get; init; }
    public decimal Available { get; init; }
    public decimal OnOrder { get; init; }
    public decimal AverageCost { get; init; }
    public decimal Value { get; init; }
    public DateTimeOffset? LastMovement { get; init; }
}
public sealed record BalanceFilterOption(Guid Id, string Name, Guid? ParentId = null);
public sealed record BalanceWorkspaceLookups(IReadOnlyList<BalanceFilterOption> Warehouses,
    IReadOnlyList<BalanceFilterOption> Types, IReadOnlyList<BalanceFilterOption> Categories,
    IReadOnlyList<BalanceFilterOption> Brands);
public sealed record BalanceUnitTotal(Guid? UnitId, string? UnitName, decimal OnHand, decimal Reserved, decimal Available, decimal OnOrder);
public sealed record BalanceWorkspaceSummary(long VariantCount, long RowCount, decimal InventoryValue, IReadOnlyList<BalanceUnitTotal> Units);
public sealed record InventoryBalanceWorkspaceDto(PagedResult<InventoryBalanceWorkspaceRow> Page,
    BalanceWorkspaceSummary Summary, string? CurrencyCode, string? CurrencyName, BalanceWorkspaceLookups Lookups);
