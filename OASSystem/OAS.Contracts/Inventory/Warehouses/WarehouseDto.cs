namespace OAS.Contracts.Inventory.Warehouses;
public sealed record WarehouseDto(Guid Id, string Code, string Name, string? Description, bool IsDefault, bool IsActive, bool AllowsSalesAllocation = true);
