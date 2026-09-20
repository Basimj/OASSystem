using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Balances.Specifications;

public static class InventoryBalanceSpecification
{
    public static ISpecification<InventoryBalance> Create(
        PageRequest request,
        Guid? warehouseId = null,
        Guid? productVariantId = null)
    {
        var normalized = request.Normalize();
        var specification = new Specification<InventoryBalance>();

        if (warehouseId.HasValue && productVariantId.HasValue)
        {
            specification.Where(x => x.WarehouseId == warehouseId.Value && x.ProductVariantId == productVariantId.Value);
        }
        else if (warehouseId.HasValue)
        {
            specification.Where(x => x.WarehouseId == warehouseId.Value);
        }
        else if (productVariantId.HasValue)
        {
            specification.Where(x => x.ProductVariantId == productVariantId.Value);
        }

        var sortBy = ResolveSortProperty(normalized.SortBy);
        specification.AddSort(sortBy, normalized.SortDirection);

        specification.ApplyPaging(
            (normalized.PageNumber - 1) * normalized.PageSize,
            normalized.PageSize);

        return specification;
    }

    private static string ResolveSortProperty(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return nameof(InventoryBalance.OnHandQuantity);

        return requested.Trim().ToLowerInvariant() switch
        {
            "onhand" => nameof(InventoryBalance.OnHandQuantity),
            "onhandquantity" => nameof(InventoryBalance.OnHandQuantity),
            "available" => nameof(InventoryBalance.OnHandQuantity),
            "averageunitcost" => nameof(InventoryBalance.AverageUnitCost),
            "inventoryvalue" => nameof(InventoryBalance.InventoryValue),
            "lastmovement" => nameof(InventoryBalance.LastMovementAtUtc),
            _ => nameof(InventoryBalance.OnHandQuantity)
        };
    }
}
