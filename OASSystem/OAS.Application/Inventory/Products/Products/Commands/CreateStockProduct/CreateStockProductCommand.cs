using OAS.Application.Abstractions.Messaging;
using OAS.Application.Inventory.Authorization;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;

public sealed record CreateStockProductCommand(CreateStockProductRequest Request)
    : ICommand<CreateStockProductResult>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions
    {
        get
        {
            var permissions = new List<string> { InventoryPermissions.Products.Create };
            if (Request.Variant is not null)
                permissions.Add(InventoryPermissions.ProductVariants.Create);
            if (Request.FrameDetails is not null)
                permissions.Add(InventoryPermissions.FrameDetails.Create);
            if (Request.LensDetails is not null)
                permissions.Add(InventoryPermissions.LensDetails.Create);
            if (Request.OpeningInventory is not null)
            {
                permissions.Add(InventoryPermissions.Transactions.Create);
                permissions.Add(InventoryPermissions.Transactions.Post);
            }
            return permissions;
        }
    }
}
