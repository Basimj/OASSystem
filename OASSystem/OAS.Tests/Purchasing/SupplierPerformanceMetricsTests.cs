using NUnit.Framework;
using OAS.Application.Purchasing.Reports;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class SupplierPerformanceMetricsTests
{
    [Test]
    public void Open_quantity_uses_posted_accepted_base_quantity_and_never_goes_negative()
    {
        Assert.That(SupplierPerformanceMetrics.OpenBaseQuantity(100m, 35m, false), Is.EqualTo(65m));
        Assert.That(SupplierPerformanceMetrics.OpenBaseQuantity(100m, 125m, false), Is.EqualTo(0m));
        Assert.That(SupplierPerformanceMetrics.OpenBaseQuantity(100m, 35m, true), Is.EqualTo(0m));
    }

    [Test]
    public void Supplier_return_is_separate_from_open_po_and_reduces_net_received()
    {
        Assert.That(SupplierPerformanceMetrics.OpenBaseQuantity(100m, 100m, false), Is.EqualTo(0m));
        Assert.That(SupplierPerformanceMetrics.NetReceivedBaseQuantity(100m, 25m), Is.EqualTo(75m));
    }

    [Test]
    public void Grni_excludes_posted_invoice_match_and_unmatched_return_cost()
    {
        var grni = SupplierPerformanceMetrics.GrniBaseAmount(10000m, 6000m, 1500m);
        Assert.That(grni, Is.EqualTo(2500m));
    }

    [Test]
    public void Ppv_is_posted_invoice_net_less_matched_receipt_cost()
    {
        Assert.That(SupplierPerformanceMetrics.PurchasePriceVarianceBase(6250m, 6000m), Is.EqualTo(250m));
        Assert.That(SupplierPerformanceMetrics.PurchasePriceVarianceBase(5750m, 6000m), Is.EqualTo(-250m));
    }

    [TestCase("2026-10-02", "2026-10-01", 10, true)]
    [TestCase("2026-10-01", "2026-10-01", 10, false)]
    [TestCase("2026-10-01", null, 10, false)]
    public void Late_delivery_requires_open_quantity_and_expected_date_before_as_of(
        string asOfText, string? expectedText, decimal openQuantity, bool expected)
    {
        var asOf = DateOnly.Parse(asOfText);
        var expectedDate = expectedText is null ? (DateOnly?)null : DateOnly.Parse(expectedText);
        Assert.That(SupplierPerformanceMetrics.IsLate(openQuantity, expectedDate, asOf), Is.EqualTo(expected));
    }

    [Test]
    public void Completed_order_still_reports_late_actual_receipt()
    {
        var due = new DateOnly(2026, 10, 8);
        Assert.That(SupplierPerformanceMetrics.WasDeliveredLate(due,
            [new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 9)]), Is.True);
        Assert.That(SupplierPerformanceMetrics.WasDeliveredLate(due,
            [new DateOnly(2026, 10, 7)]), Is.False);
    }

    [Test]
    public void Purchase_lines_preserve_unit_conversion_in_base_quantities()
    {
        var orderId = Guid.NewGuid();
        var orderLine = PurchaseOrderLine.Create(
            Guid.NewGuid(), orderId, 1, Guid.NewGuid(), null, Guid.NewGuid(), 2.5m,
            "P-1", "Product", "Box", 4m, 10m, 0m, 0m, TaxCalculationMode.Exclusive, null, null);

        var receiptLine = PurchaseReceiptLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), orderLine.Id, 1, orderLine.ProductVariantId,
            orderLine.OrderedQuantity, 0m, 2m, 2m, 0m, orderLine.UnitConversionFactor, 10m, null, null, null);

        Assert.That(orderLine.BaseQuantity, Is.EqualTo(10m));
        Assert.That(receiptLine.BaseAcceptedQuantity, Is.EqualTo(5m));
        Assert.That(SupplierPerformanceMetrics.OpenBaseQuantity(orderLine.BaseQuantity, receiptLine.BaseAcceptedQuantity, false), Is.EqualTo(5m));
    }

    [Test]
    public void Weighted_lead_time_and_on_time_rate_use_base_quantity_weights()
    {
        var lead = SupplierPerformanceMetrics.WeightedAverageDays([(25m, 4m), (75m, 8m)]);
        var onTime = SupplierPerformanceMetrics.WeightedPercent([(25m, true), (75m, false)]);

        Assert.That(lead, Is.EqualTo(7m));
        Assert.That(onTime, Is.EqualTo(25m));
    }
}
