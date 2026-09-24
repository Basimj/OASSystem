using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Guid ProductId, UpdateProductRequest Request)
    : ICommand<ProductDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [InventoryPermissions.Products.Edit];
}
