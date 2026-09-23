using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using OAS.API.Inventory.Controllers;
using OAS.Application.Inventory.Authorization;
using OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;
using OAS.Contracts.Inventory.Products;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class InventoryProductApiContractTests
{
    [Test]
    public void ProductsController_RemainsAuthenticatedAndUsesExpectedRoute()
    {
        Assert.That(typeof(ProductsController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true), Is.Not.Empty);
        var route = (RouteAttribute?)typeof(ProductsController).GetCustomAttributes(typeof(RouteAttribute), inherit: true).SingleOrDefault();
        Assert.That(route?.Template, Is.EqualTo("api/inventory/products"));
    }

    [Test]
    public void CreateWithOpeningInventory_RequiresCatalogAndInventoryPermissions()
    {
        var command = new CreateStockProductCommand(new CreateStockProductRequest(
            new CreateProductRequest("P-API", "منتج", null, Guid.NewGuid(), null, Guid.NewGuid(), null, true),
            new InitialProductVariantRequest("SKU-API", null, null, null, null, null, 10m, 20m),
            new OpeningInventoryRequest(Guid.NewGuid(), 1m, 10m)));

        Assert.That(command.RequiredPermissions, Does.Contain(InventoryPermissions.Products.Create));
        Assert.That(command.RequiredPermissions, Does.Contain(InventoryPermissions.ProductVariants.Create));
        Assert.That(command.RequiredPermissions, Does.Contain(InventoryPermissions.Transactions.Create));
        Assert.That(command.RequiredPermissions, Does.Contain(InventoryPermissions.Transactions.Post));
    }
}
