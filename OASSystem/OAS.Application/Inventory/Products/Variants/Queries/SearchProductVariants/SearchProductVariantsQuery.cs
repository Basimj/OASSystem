using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Variants.Queries.SearchProductVariants;

public sealed record SearchProductVariantsQuery(string? Search, int Take = 20)
    : IQuery<IReadOnlyList<ProductVariantLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [InventoryPermissions.ProductVariants.View];
}
