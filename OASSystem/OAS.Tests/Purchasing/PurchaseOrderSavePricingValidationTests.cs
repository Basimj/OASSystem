using NUnit.Framework;
using OAS.Application.Purchasing.PurchaseOrders.Commands;
using OAS.Application.Purchasing.PurchaseOrders.Validators;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Contracts.Sales.Enums;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchaseOrderSavePricingValidationTests
{
    [TestCase(0)]
    [TestCase(-1)]
    public void Create_endpoint_rejects_nonpositive_unit_price(decimal price)
    {
        var result = new CreatePurchaseOrderCommandValidator().Validate(
            new CreatePurchaseOrderCommand(CreateRequest(price)));
        Assert.That(result.Errors.Any(x => x.PropertyName.EndsWith("Lines[0].UnitPrice", StringComparison.Ordinal)), Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Update_endpoint_rejects_nonpositive_unit_price(decimal price)
    {
        var baseRequest = CreateRequest(price);
        var line = baseRequest.Lines.Single();
        var request = new UpdatePurchaseOrderRequest(
            baseRequest.SupplierId, baseRequest.DestinationWarehouseId,
            baseRequest.OrderDate, baseRequest.ExpectedDeliveryDate,
            baseRequest.CurrencyId, baseRequest.ExchangeRate, baseRequest.ExchangeRateDate,
            baseRequest.TaxCalculationMode, baseRequest.PaymentTermDays, baseRequest.Notes,
            [new UpdatePurchaseOrderLineRequest(null, line.LineSequence, line.ProductVariantId,
                line.SupplierCatalogItemId, line.PurchaseUnitId, line.UnitConversionFactor,
                line.OrderedQuantity, line.UnitPrice, line.DiscountAmount, line.TaxRate,
                line.ExpectedDeliveryDate, line.Notes, [], null)],
            Convert.ToBase64String(new byte[] { 1 }));
        var result = new UpdatePurchaseOrderCommandValidator().Validate(
            new UpdatePurchaseOrderCommand(Guid.NewGuid(), request));
        Assert.That(result.Errors.Any(x => x.PropertyName.EndsWith("Lines[0].UnitPrice", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void Create_endpoint_accepts_positive_unit_price()
    {
        var result = new CreatePurchaseOrderCommandValidator().Validate(
            new CreatePurchaseOrderCommand(CreateRequest(50m)));
        Assert.That(result.Errors, Is.Empty);
    }

    private static CreatePurchaseOrderRequest CreateRequest(decimal unitPrice) => new(
        Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 11), null,
        Guid.NewGuid(), 1m, new DateOnly(2026, 11, 11),
        TaxCalculationMode.Exclusive, 0, null,
        [new CreatePurchaseOrderLineRequest(1, Guid.NewGuid(), null, Guid.NewGuid(),
            1m, 2m, unitPrice, 0m, 0m, null, null, [])]);
}
