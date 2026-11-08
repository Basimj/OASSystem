using NUnit.Framework;
using OAS.UiLib.Core.Formatting;
using OAS.UiLib.Core.Models.Purchasing;

namespace OAS.Tests.Purchasing.Client;

[TestFixture]
public class QuantityInputRulesTests
{
    [TestCase("PCS", "قطعة", true)]
    [TestCase("ea", "حبة", true)]
    [TestCase("", "كرتون", true)]
    [TestCase("KG", "كيلوجرام", false)]
    [TestCase("L", "لتر", false)]
    [TestCase("M", "متر", false)]
    [TestCase("", "", false)]
    public void WholeUnitDetection_IsConservative(string code, string name, bool expected)
        => Assert.That(QuantityInputRules.UsesWholeNumbers(code, name), Is.EqualTo(expected));

    [Test]
    public void ExistingFractions_AreNotSilentlyRoundedOrBlockedByInputStep()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QuantityInputRules.StepForUnit("PCS", "قطعة", 2.5m), Is.EqualTo("0.001"));
            Assert.That(QuantityInputRules.StepForUnit("PCS", "قطعة", 2m), Is.EqualTo("1"));
            Assert.That(QuantityInputRules.StepForUnit("KG", "كيلوجرام", 2m), Is.EqualTo("0.001"));
            Assert.That(QuantityInputRules.IsWhole(4m), Is.True);
            Assert.That(QuantityInputRules.IsWhole(4.25m), Is.False);
        });
    }

    [Test]
    public void OrderValidator_ExplainsFractionalPieceQuantity()
    {
        var model = new UiPurchaseOrderEditorModel
        {
            SupplierId = Guid.NewGuid(),
            DestinationWarehouseId = Guid.NewGuid(),
            CurrencyId = Guid.NewGuid(),
            OrderDate = new DateOnly(2026, 11, 8),
            ExchangeRateDate = new DateOnly(2026, 11, 8),
            ExchangeRate = 1m
        };
        model.Lines.Add(new UiPurchaseOrderLineModel
        {
            ProductVariantId = Guid.NewGuid(),
            PurchaseUnitId = Guid.NewGuid(),
            PurchaseUnitName = "قطعة",
            OrderedQuantity = 2.5m,
            UnitConversionFactor = 1m,
            UnitPrice = 100m
        });
        var errors = UiPurchaseOrderFormValidator.Validate(model);
        Assert.That(errors.TryGetValue("Lines[0].OrderedQuantity", out var error), Is.True);
        Assert.That(error, Does.Contain("دون كسور"));
    }

    [Test]
    public void ReceiptValidator_KeepsFractionRulesWhenUnitIsKnown()
    {
        var receipt = new UiPurchaseReceiptEditorModel { PurchaseOrderId = Guid.NewGuid() };
        receipt.Lines.Add(new UiPurchaseReceiptLineModel
        {
            PurchaseOrderLineId = Guid.NewGuid(), LineSequence = 1, ProductVariantId = Guid.NewGuid(),
            PurchaseUnitName = "قطعة", OrderedQuantitySnapshot = 8m, RemainingReceivableQuantity = 8m,
            ReceivedQuantity = 1.5m, AcceptedQuantity = 1.5m
        });
        Assert.That(UiPurchaseReceiptFormValidator.Validate(receipt).ContainsKey("Lines[0].ReceivedQuantity"), Is.True);
        receipt.Lines[0].PurchaseUnitName = "كيلوجرام";
        Assert.That(UiPurchaseReceiptFormValidator.Validate(receipt).ContainsKey("Lines[0].ReceivedQuantity"), Is.False);
    }

    [Test]
    public void OrderValidator_AllowsMeasuredQuantities()
    {
        var model = new UiPurchaseOrderEditorModel
        {
            SupplierId = Guid.NewGuid(), DestinationWarehouseId = Guid.NewGuid(), CurrencyId = Guid.NewGuid(),
            OrderDate = new DateOnly(2026, 11, 8), ExchangeRateDate = new DateOnly(2026, 11, 8), ExchangeRate = 1m
        };
        model.Lines.Add(new UiPurchaseOrderLineModel
        {
            ProductVariantId = Guid.NewGuid(), PurchaseUnitId = Guid.NewGuid(), PurchaseUnitName = "كيلوجرام",
            OrderedQuantity = 2.5m, UnitConversionFactor = 1m, UnitPrice = 100m
        });
        var errors = UiPurchaseOrderFormValidator.Validate(model);
        Assert.That(errors.ContainsKey("Lines[0].OrderedQuantity"), Is.False);
    }
}
