using NUnit.Framework;
using OAS.Application.Sales.CustomerOrders.Validation;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class CustomerOrderPricingValidationTests
{
    private readonly CustomerOrderLineRequestValidator _validator = new();

    private static CustomerOrderLineRequest Line(SalesDiscountType type, decimal? discount, decimal? taxRate) =>
        new(null, null, SalesLineType.Other, null, null, "خدمة", 2m, 100m,
            type, discount, taxRate, null, null, false, null);

    [Test]
    public void PercentageSelectedWithoutValue_IsRejectedBeforeDomainPricing()
    {
        var result = _validator.Validate(Line(SalesDiscountType.Percentage, null, null));
        Assert.That(result.Errors.Any(error => error.PropertyName == "DiscountValue"), Is.True);
    }

    [Test]
    public void PercentageAboveOneHundred_IsRejected()
    {
        var result = _validator.Validate(Line(SalesDiscountType.Percentage, 110m, null));
        Assert.That(result.Errors.Any(x => x.PropertyName == "DiscountValue"), Is.True);
    }

    [Test]
    public void TaxRateAboveOneHundred_IsRejected()
    {
        var result = _validator.Validate(Line(SalesDiscountType.None, null, 101m));
        Assert.That(result.Errors.Any(x => x.PropertyName == "TaxRate"), Is.True);
    }

    [Test]
    public void FixedDiscountAboveLineGross_IsRejected()
    {
        var result = _validator.Validate(Line(SalesDiscountType.FixedAmount, 250m, 5m));
        Assert.That(result.Errors.Any(x => x.PropertyName == "DiscountValue"), Is.True);
    }

    [Test]
    public void OptionalDiscountAndTaxOrValidValues_AreAccepted()
    {
        Assert.That(_validator.Validate(Line(SalesDiscountType.None, null, null)).IsValid, Is.True);
        Assert.That(_validator.Validate(Line(SalesDiscountType.Percentage, 10m, 15m)).IsValid, Is.True);
        Assert.That(_validator.Validate(Line(SalesDiscountType.FixedAmount, 20m, 15m)).IsValid, Is.True);
    }
}
