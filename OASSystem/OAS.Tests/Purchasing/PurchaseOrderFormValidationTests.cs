using NUnit.Framework;
using OAS.UiLib.Core.Models.Purchasing;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchaseOrderFormValidationTests
{
    [Test]
    public void Empty_order_returns_explanations_for_required_fields_and_lines()
    {
        var errors = UiPurchaseOrderFormValidator.Validate(new UiPurchaseOrderEditorModel());
        Assert.Multiple(() =>
        {
            Assert.That(errors.ContainsKey("SupplierId"), Is.True);
            Assert.That(errors.ContainsKey("DestinationWarehouseId"), Is.True);
            Assert.That(errors.ContainsKey("CurrencyId"), Is.True);
            Assert.That(errors.ContainsKey("Lines"), Is.True);
        });
    }

    [Test]
    public void Invalid_line_reports_each_problem_without_removing_existing_values()
    {
        var model = ValidOrder();
        var line = model.Lines[0];
        line.PurchaseUnitId = null;
        line.OrderedQuantity = 0;
        line.UnitPrice = -1;
        line.UnitConversionFactor = 0;
        line.TaxRate = -2;
        var errors = UiPurchaseOrderFormValidator.Validate(model);
        Assert.Multiple(() =>
        {
            Assert.That(errors.ContainsKey("Lines[0].PurchaseUnitId"), Is.True);
            Assert.That(errors.ContainsKey("Lines[0].OrderedQuantity"), Is.True);
            Assert.That(errors.ContainsKey("Lines[0].UnitPrice"), Is.True);
            Assert.That(errors.ContainsKey("Lines[0].UnitConversionFactor"), Is.True);
            Assert.That(errors.ContainsKey("Lines[0].TaxRate"), Is.True);
            Assert.That(line.UnitPrice, Is.EqualTo(-1));
        });
    }

    [Test]
    public void Expected_date_before_order_date_is_invalid()
    {
        var model = ValidOrder();
        model.ExpectedDeliveryDate = model.OrderDate.AddDays(-1);
        Assert.That(UiPurchaseOrderFormValidator.Validate(model), Does.ContainKey("ExpectedDeliveryDate"));
    }

    [Test]
    public void Discount_exceeding_line_value_is_explained_inline()
    {
        var model = ValidOrder();
        model.Lines[0].UnitPrice = 30m;
        model.Lines[0].DiscountAmount = 35m;
        Assert.That(UiPurchaseOrderFormValidator.Validate(model).ContainsKey("Lines[0].DiscountAmount"), Is.True);
    }

    [Test]
    public void Order_totals_are_updated_from_edited_lines_before_saving()
    {
        var model = ValidOrder();
        model.Lines[0].OrderedQuantity = 3;
        model.Lines[0].UnitPrice = 20;
        model.Lines[0].DiscountAmount = 10;
        model.Lines[0].TaxRate = 10;
        var totals = UiPurchaseOrderAmountPreview.FromModel(model);
        Assert.Multiple(() =>
        {
            Assert.That(totals.Subtotal, Is.EqualTo(60m));
            Assert.That(totals.Discount, Is.EqualTo(10m));
            Assert.That(totals.Tax, Is.EqualTo(5m));
            Assert.That(totals.Total, Is.EqualTo(55m));
        });
    }

    [Test]
    public void Tax_inclusive_preview_keeps_total_equal_to_price_after_discount()
    {
        var model = ValidOrder();
        model.TaxCalculationMode = 2;
        model.Lines[0].OrderedQuantity = 3;
        model.Lines[0].UnitPrice = 20;
        model.Lines[0].DiscountAmount = 10;
        model.Lines[0].TaxRate = 10;
        var totals = UiPurchaseOrderAmountPreview.FromModel(model);
        Assert.Multiple(() =>
        {
            Assert.That(totals.Total, Is.EqualTo(50m));
            Assert.That(totals.Tax, Is.EqualTo(4.5455m));
        });
    }

    [Test]
    public void Legacy_draft_can_be_inspected_but_zero_price_is_rejected_before_save()
    {
        var model = ValidOrder();
        model.Lines[0].UnitPrice = 0;
        Assert.That(UiPurchaseOrderFormValidator.Validate(model), Is.Empty);
        Assert.That(UiPurchaseOrderFormValidator.ValidateForSave(model),
            Does.ContainKey("Lines[0].UnitPrice"));
    }

    [Test]
    public void Saving_checks_positive_price_without_modifying_the_input()
    {
        var model = ValidOrder();
        var errors = UiPurchaseOrderFormValidator.ValidateForSave(model);
        Assert.That(errors, Does.ContainKey("Lines[0].UnitPrice"));
        Assert.That(model.Lines[0].UnitPrice, Is.EqualTo(0m));
    }

    [Test]
    public void Save_rejects_full_discount_even_with_positive_unit_price()
    {
        var model = ValidOrder();
        model.Lines[0].UnitPrice = 20m;
        model.Lines[0].DiscountAmount = 20m;
        var errors = UiPurchaseOrderFormValidator.ValidateForSave(model);
        Assert.That(errors, Does.ContainKey("Lines[0].DiscountAmount"));
    }

    [Test]
    public void Save_rejects_base_unit_cost_rounded_to_zero()
    {
        var model = ValidOrder();
        model.Lines[0].UnitPrice = 0.0001m;
        model.ExchangeRate = 0.1m;
        Assert.That(UiPurchaseOrderFormValidator.Validate(model), Is.Empty);
        Assert.That(UiPurchaseOrderFormValidator.ValidateForSave(model),
            Does.ContainKey("Lines[0].UnitPrice"));
    }

    [Test]
    public void Save_rejects_base_quantity_rounded_to_zero()
    {
        var model = ValidOrder();
        model.Lines[0].UnitPrice = 100m;
        model.Lines[0].UnitConversionFactor = 0.0001m;
        Assert.That(UiPurchaseOrderFormValidator.ValidateForSave(model),
            Does.ContainKey("Lines[0].UnitConversionFactor"));
    }

    [Test]
    public void Save_accepts_all_priced_lines()
    {
        var model = ValidOrder();
        model.Lines[0].UnitPrice = 25m;
        model.Lines.Add(new UiPurchaseOrderLineModel
        {
            LineSequence = 2,
            ProductVariantId = Guid.NewGuid(),
            PurchaseUnitId = Guid.NewGuid(),
            UnitConversionFactor = 12m,
            OrderedQuantity = 3m,
            UnitPrice = 120m,
            DiscountAmount = 5m
        });
        Assert.That(UiPurchaseOrderFormValidator.ValidateForSave(model), Is.Empty);
    }

    private static UiPurchaseOrderEditorModel ValidOrder()
    {
        var model = new UiPurchaseOrderEditorModel
        {
            SupplierId = Guid.NewGuid(),
            DestinationWarehouseId = Guid.NewGuid(),
            CurrencyId = Guid.NewGuid(),
            OrderDate = new DateOnly(2026, 11, 6),
            ExchangeRate = 1m,
            ExchangeRateDate = new DateOnly(2026, 11, 6)
        };
        model.Lines.Add(new UiPurchaseOrderLineModel
        {
            ProductVariantId = Guid.NewGuid(),
            PurchaseUnitId = Guid.NewGuid(),
            OrderedQuantity = 1m,
            UnitPrice = 0m,
            UnitConversionFactor = 1m
        });
        return model;
    }
}
