using OAS.Contracts.Inventory.Products;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Common;

internal static class SalesProductTypeMapping
{
    public static SalesLineType ToLineType(string? systemKey) => systemKey?.Trim().ToUpperInvariant() switch
    {
        ProductTypeSystemKeys.Frame => SalesLineType.Frame,
        ProductTypeSystemKeys.Sunglasses => SalesLineType.Frame,
        ProductTypeSystemKeys.Lens => SalesLineType.Lens,
        ProductTypeSystemKeys.Accessory => SalesLineType.Accessory,
        ProductTypeSystemKeys.Service => SalesLineType.Service,
        _ => SalesLineType.Other
    };
}
