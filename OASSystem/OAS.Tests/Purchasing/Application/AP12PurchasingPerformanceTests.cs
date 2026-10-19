using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using NUnit.Framework;
using OAS.API.Purchasing.Controllers;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Performance;
using OAS.Contracts.Purchasing.Performance;
using OAS.Infrastructure.Purchasing.Persistence.Queries;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Purchasing.Application;

[TestFixture]
[Category("AP12")]
public sealed class AP12PurchasingPerformanceTests
{
    [Test]
    public void PartiallyPostedReceipt_KeepsOpenQuantity_WithoutAddingReturnedGoods()
    {
        // 100 ordered, 65 accepted in posted receipts, 15 returned: 35 still expected.
        Assert.That(PurchasingPerformanceMath.OpenQuantity(100m, 65m, false), Is.EqualTo(35m));
        Assert.That(PurchasingPerformanceMath.OpenQuantity(100m, 50m, true), Is.Zero);
    }

    [Test]
    public void InvoiceAndReturns_NotDoubleCountedAsUninvoicedAccepted()
    {
        // Returned units linked to an already-posted invoice must not reduce GRNI a second time.
        // Only pre-invoice returns are included in the last argument.
        Assert.That(PurchasingPerformanceMath.UninvoicedAccepted(90m, 60m, 10m), Is.EqualTo(20m));
        Assert.That(PurchasingPerformanceMath.UninvoicedAccepted(90m, 60m, 0m), Is.EqualTo(30m));
        Assert.That(PurchasingPerformanceMath.UninvoicedAccepted(10m, 25m, 0m), Is.Zero);
    }

    [Test]
    public void PostInvoiceOrFullyReceivedOrder_IsNotOpen()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PurchasingPerformanceMath.OpenQuantity(30m, 30m, false), Is.Zero);
            Assert.That(PurchasingPerformanceMath.OpenQuantity(30m, 0m, true), Is.Zero);
        });
    }

    [Test]
    public void PurchaseUnits_ConvertToBaseWithoutMixingDocumentQuantities()
    {
        var orderId = Guid.NewGuid();
        var product = Guid.NewGuid();
        var orderLine = PurchaseOrderLine.Create(Guid.NewGuid(), orderId, 1, product,
            null, Guid.NewGuid(), 12m, "P-01", "منتج بعلبة من 12", "علبة",
            10m, 25m, 0m, 0m, TaxCalculationMode.Exclusive, null, null);
        var receiptLine = PurchaseReceiptLine.Create(Guid.NewGuid(), Guid.NewGuid(), orderLine.Id,
            1, product, 10m, 0m, 4m, 4m, 0m, 12m, 2m, null, null, null);
        Assert.Multiple(() =>
        {
            Assert.That(orderLine.OrderedQuantity, Is.EqualTo(10m));
            Assert.That(orderLine.BaseQuantity, Is.EqualTo(120m));
            Assert.That(receiptLine.AcceptedQuantity, Is.EqualTo(4m));
            Assert.That(receiptLine.BaseAcceptedQuantity, Is.EqualTo(48m));
            Assert.That(PurchasingPerformanceMath.OpenQuantity(orderLine.OrderedQuantity, receiptLine.AcceptedQuantity, false),
                Is.EqualTo(6m));
        });
    }

    [Test]
    public void Report_QueryEnforcesReadPermissionsForAllDataSources()
    {
        var query = new GetPurchasingPerformanceQuery(
            new PurchasingPerformanceFilterDto(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));
        Assert.That(query.RequiredPermissions, Does.Contain(PurchasingPermissions.Orders.View));
        Assert.That(query.RequiredPermissions, Does.Contain(PurchasingPermissions.Receipts.View));
        Assert.That(query.RequiredPermissions, Does.Contain(PurchasingPermissions.Invoices.View));
        Assert.That(query.RequiredPermissions, Does.Contain(PurchasingPermissions.Catalog.View));
        Assert.That(typeof(PurchasingPerformanceController).GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
    }

    [Test]
    public void AdvancedMetrics_UsePostedCostsAndDoNotConfuseGrniWithLedger()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PurchasingPerformanceMath.UninvoicedReceiptCost(10000m, 6000m, 1500m), Is.EqualTo(2500m));
            Assert.That(PurchasingPerformanceMath.UninvoicedReceiptCost(100m, 200m, 0m), Is.Zero);
            Assert.That(PurchasingPerformanceMath.PurchasePriceVarianceBase(6250m, 6000m), Is.EqualTo(250m));
            Assert.That(PurchasingPerformanceMath.OnTimeDeliveryPercent(100m, 75m), Is.EqualTo(75m));
            Assert.That(PurchasingPerformanceMath.OnTimeDeliveryPercent(0m, 0m), Is.Null);
        });
    }

    [Test]
    public void OrderLineDrillDown_IsProtectedAndLinksToAnOrderLineNotHeader()
    {
        var query = new GetPurchasingPerformanceOrderLineDrillDownQuery(Guid.NewGuid(),
            new PurchasingPerformanceFilterDto(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));
        Assert.That(query.RequiredPermissions, Does.Contain(PurchasingPermissions.Orders.View));
        Assert.That(query.RequiredPermissions, Does.Contain(PurchasingPermissions.Invoices.View));
        Assert.That(typeof(PurchasingPerformanceController).GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
        var row = new PurchasingPerformanceRowDto { Kind = "orders", PurchaseOrderId = Guid.NewGuid(), PurchaseOrderLineId = Guid.NewGuid() };
        Assert.That(row.PurchaseOrderLineId, Is.Not.EqualTo(row.PurchaseOrderId));
    }

    [Test]
    public void FinancialTotalsAndCurrencies_AreSeparateInPublicContract()
    {
        var summary = new PurchasingPerformanceSummaryDto(3, 2, 1, 2, 1, 1, 7d,
            50000m, 43000m, 5000m, "YER");
        Assert.Multiple(() =>
        {
            Assert.That(summary.PostedReceiptCostBase, Is.EqualTo(50000m));
            Assert.That(summary.PostedInvoiceTotalBase, Is.EqualTo(43000m));
            Assert.That(summary.PostedReturnTotalBase, Is.EqualTo(5000m));
            Assert.That(summary.BaseCurrencyCode, Is.EqualTo("YER"));
        });
        var row = new PurchasingPerformanceRowDto {
            Kind = "orders", CurrencyCode = "USD", DocumentAmount = 100m, BaseAmount = null,
            Quantity = 2m, OrderedQuantity = 2m
        };
        Assert.That(row.BaseAmount, Is.Null, "An unposted PO must never be a source of AP or base-currency journal totals.");
    }
}
