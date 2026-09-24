using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Variants.Commands.UpdateProductVariant;

public sealed record UpdateProductVariantCommand(Guid VariantId, UpdateProductVariantRequest Request)
    : ICommand<ProductVariantDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [InventoryPermissions.ProductVariants.Edit];
}
