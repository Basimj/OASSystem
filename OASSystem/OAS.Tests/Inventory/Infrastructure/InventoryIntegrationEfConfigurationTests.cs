using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Inventory.Infrastructure;

[TestFixture]
public sealed class InventoryIntegrationEfConfigurationTests
{
    private DbContextOptions<OasDbContext> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=dummy_inventory_tests;Trusted_Connection=True;")
            .Options;
    }

    [Test]
    public void Warehouse_EnforcesAtMostOneActiveDefault()
    {
        using var context = new OasDbContext(_options);
        var entity = context.Model.FindEntityType(typeof(Warehouse));
        Assert.That(entity, Is.Not.Null);

        var index = entity!.GetIndexes().SingleOrDefault(x => x.GetDatabaseName() == "UX_Warehouses_ActiveDefault");
        Assert.That(index, Is.Not.Null);
        Assert.That(index!.IsUnique, Is.True);
        Assert.That(index.GetFilter(), Is.EqualTo("[IsDefault] = 1 AND [IsActive] = 1"));
    }

    [Test]
    public void InventoryBalance_HasLogicalWarehouseVariantUniqueKeyAndRowVersionConcurrency()
    {
        using var context = new OasDbContext(_options);
        var entity = context.Model.FindEntityType(typeof(InventoryBalance));
        Assert.That(entity, Is.Not.Null);

        var logicalIndex = entity!.GetIndexes().SingleOrDefault(x => x.GetDatabaseName() == "UX_InventoryBalances_Warehouse_ProductVariant");
        Assert.That(logicalIndex, Is.Not.Null);
        Assert.That(logicalIndex!.IsUnique, Is.True);
        Assert.That(logicalIndex.Properties.Select(x => x.Name), Is.EqualTo(new[] { nameof(InventoryBalance.WarehouseId), nameof(InventoryBalance.ProductVariantId) }));

        var rowVersion = entity.FindProperty(nameof(InventoryBalance.RowVersion));
        Assert.That(rowVersion, Is.Not.Null);
        Assert.That(rowVersion!.IsConcurrencyToken, Is.True);
    }
    [Test]
    public void ProductAndVariantIdentityFields_AreProtectedByUniqueIndexes()
    {
        using var context = new OasDbContext(_options);

        var product = context.Model.FindEntityType(typeof(Product));
        Assert.That(product, Is.Not.Null);
        var productCode = product!.GetIndexes().SingleOrDefault(x => x.GetDatabaseName() == "UX_Products_ProductCode");
        Assert.That(productCode, Is.Not.Null);
        Assert.That(productCode!.IsUnique, Is.True);

        var variant = context.Model.FindEntityType(typeof(ProductVariant));
        Assert.That(variant, Is.Not.Null);
        var sku = variant!.GetIndexes().SingleOrDefault(x => x.GetDatabaseName() == "UX_ProductVariants_SKU");
        var barcode = variant.GetIndexes().SingleOrDefault(x => x.GetDatabaseName() == "UX_ProductVariants_Barcode");
        Assert.That(sku, Is.Not.Null);
        Assert.That(sku!.IsUnique, Is.True);
        Assert.That(barcode, Is.Not.Null);
        Assert.That(barcode!.IsUnique, Is.True);
        Assert.That(barcode.GetFilter(), Is.EqualTo("[Barcode] IS NOT NULL"));
    }

}
