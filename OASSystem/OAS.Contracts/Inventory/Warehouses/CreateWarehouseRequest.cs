namespace OAS.Contracts.Inventory.Warehouses;
public sealed record CreateWarehouseRequest(string Code, string Name, string? Description, bool IsDefault = false, bool AllowsSalesAllocation = true);
