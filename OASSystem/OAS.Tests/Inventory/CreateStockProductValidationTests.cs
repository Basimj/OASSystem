using NUnit.Framework;
using OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;
using OAS.Contracts.Inventory.Products;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class CreateStockProductValidationTests
{
    private static CreateStockProductCommand BuildCommand(decimal quantity = 10m, decimal unitCost = 100m, bool includeVariant = true)
    {
        var product = new CreateProductRequest(
            "P-VALID",
            "منتج",
            null,
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            null,
            true);

        var variant = includeVariant
            ? new InitialProductVariantRequest("SKU-VALID", null, null, null, null, null, 50m, 80m)
            : null;

        return new CreateStockProductCommand(new CreateStockProductRequest(
            product,
            variant,
            new OpeningInventoryRequest(Guid.NewGuid(), quantity, unitCost)));
    }

    [Test]
    public void StockProductWithoutVariant_IsRejected()
    {
        var validator = new CreateStockProductCommandValidator();
        var result = validator.Validate(BuildCommand(includeVariant: false));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(x => x.ErrorCode == "stock_product_variant_required"), Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void OpeningQuantityMustBePositive(decimal quantity)
    {
        var validator = new CreateStockProductCommandValidator();
        var result = validator.Validate(BuildCommand(quantity: quantity));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(x => x.ErrorCode == "opening_quantity_must_be_positive"), Is.True);
    }

    [Test]
    public void OpeningUnitCostCannotBeNegative()
    {
        var validator = new CreateStockProductCommandValidator();
        var result = validator.Validate(BuildCommand(unitCost: -0.01m));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(x => x.ErrorCode == "opening_unit_cost_invalid"), Is.True);
    }
}
