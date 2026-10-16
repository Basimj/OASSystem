namespace OAS.Contracts.Inventory.Warehouses;
public sealed record UpdateWarehouseRequest(string Code, string Name, string? Description, bool IsDefault, bool IsActive, bool AllowsSalesAllocation = true);
