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
    [TestCase("0.0001", "1", "opening_quantity_precision_invalid")]
    [TestCase("1", "0.0000000000000000000000000001", "opening_unit_cost_precision_invalid")]
    [TestCase("1", "0.001", "opening_unit_cost_precision_invalid")]
    [TestCase("1000000000000000", "1", "opening_quantity_precision_invalid")]
    [TestCase("100000000000000", "1000", "opening_total_cost_precision_invalid")]
    [TestCase("79228162514264337593543950335", "1", "opening_quantity_precision_invalid")]
    public void OpeningValuesOutsideDatabasePrecision_AreRejected(string quantity, string cost, string code)
    {
        var result = new CreateStockProductCommandValidator().Validate(BuildCommand(
            decimal.Parse(quantity, System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture)));
        Assert.That(result.Errors.Any(x => x.ErrorCode == code), Is.True);
    }

    [Test]
    public void FractionalOpeningQuantityAndZeroCost_AreAllowed()
    {
        var result = new CreateStockProductCommandValidator().Validate(BuildCommand(0.001m, 0m));
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void MinimumDecimalDetail_ReturnsValidationErrorWithoutOverflow()
    {
        var command = BuildCommand();
        command = command with { Request = command.Request with {
            FrameDetails = new InitialFrameDetailsRequest("Frame", null, null, null, null, decimal.MinValue, null, null)
        }};
        var result = new CreateStockProductCommandValidator().Validate(command);
        Assert.That(result.Errors.Any(x => x.ErrorCode == "frame_temple_length_invalid"), Is.True);
    }

    [Test]
    public void InitialVariantPrice_WithExcessDecimalPlaces_IsRejected()
    {
        var command = BuildCommand();
        command = command with { Request = command.Request with {
            Variant = command.Request.Variant! with { PurchasePrice = 1.001m }
        }};
        var result = new CreateStockProductCommandValidator().Validate(command);
        Assert.That(result.Errors.Any(x => x.ErrorCode == "purchase_price_precision_invalid"), Is.True);
    }
}
