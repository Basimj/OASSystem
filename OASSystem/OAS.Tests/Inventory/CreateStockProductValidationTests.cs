using NUnit.Framework;
using OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;
using OAS.Contracts.Inventory.Products;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class CreateStockProductValidationTests
{
    private static CreateStockProductCommand BuildCommand(bool includeVariant = true,
        OpeningInventoryRequest? opening = null)
    {
        var product = new CreateProductRequest("P-VALID", "منتج", Guid.NewGuid(), null, null, true);
        var variant = includeVariant
            ? new InitialProductVariantRequest("SKU-VALID", null, null, null, null, null, 50m, 80m)
            : null;
        return new CreateStockProductCommand(new CreateStockProductRequest(product, variant, opening));
    }

    [Test]
    public void StockProductWithoutVariant_IsRejected()
    {
        var result = new CreateStockProductCommandValidator().Validate(BuildCommand(includeVariant: false));
        Assert.That(result.Errors.Any(x => x.ErrorCode == "stock_product_variant_required"), Is.True);
    }

    [TestCase("0", "0")]
    [TestCase("1", "100")]
    [TestCase("0.001", "0")]
    public void AnyOpeningPayload_IsRejected(string quantity, string cost)
    {
        var command = BuildCommand(opening: new OpeningInventoryRequest(Guid.NewGuid(),
            decimal.Parse(quantity, System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture)));
        var result = new CreateStockProductCommandValidator().Validate(command);
        Assert.That(result.Errors.Any(x => x.ErrorCode == "opening_inventory_disabled"), Is.True);
    }

    [Test]
    public void ProductWithoutOpening_IsValid()
    {
        Assert.That(new CreateStockProductCommandValidator().Validate(BuildCommand()).IsValid, Is.True);
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
