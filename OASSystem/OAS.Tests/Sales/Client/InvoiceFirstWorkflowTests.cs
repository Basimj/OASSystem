using OAS.Application.Sales.Services;
using OAS.Client.Sales.Mapping;
using OAS.Client.Sales.Services;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.UiLib.Core.Models.Sales;
using NUnit.Framework;

namespace OAS.Tests.Sales.Client;

[TestFixture]
public sealed class InvoiceFirstWorkflowTests
{
    [Test]
    public void AnyProductionLineRequiresCustomerOrderAndCheckout()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SalesInvoiceOperationalRoutingPolicy.RequiresOrder(Line(SalesLineType.Lens)), Is.True);
            Assert.That(SalesInvoiceOperationalRoutingPolicy.RequiresOrder(Line(SalesLineType.Frame, group: Guid.NewGuid())), Is.True);
            Assert.That(SalesInvoiceOperationalRoutingPolicy.RequiresOrder(Line(SalesLineType.Service, production: true)), Is.True);
            Assert.That(SalesInvoiceOperationalRoutingPolicy.RequiresOrder(Line(SalesLineType.Frame)), Is.False);
            Assert.That(SalesInvoiceOperationalRoutingPolicy.RequiresOrder(Line(SalesLineType.Accessory)), Is.False);
        });
    }

    [Test]
    public void InvoiceFirstOrderUsesReservedHumanOrderNumber()
    {
        var model = new UiSalesInvoiceFormModel
        {
            InvoiceCode = "SI-2026-000123",
            CustomerId = Guid.NewGuid().ToString(), CurrencyId = Guid.NewGuid().ToString(),
            InvoiceDate = new DateOnly(2026, 11, 20),
            Lines = [new UiSalesLineModel { LineType = "Service", Quantity = 1m, ActualUnitPrice = 10m }]
        };
        var reservedCode = "SO-2026-000124";
        Assert.That(SalesUiMapper.ToOperationalOrder(model, reservedCode).OrderCode, Is.EqualTo(reservedCode));
        Assert.That(SalesUiMapper.ToOperationalOrder(model, reservedCode).OrderCode, Does.StartWith("SO-"));
    }

    [Test]
    public void InvoiceFormCreatesOperationalOrderWithMeasurementsAndPaymentPlan()
    {
        var customerId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var model = new UiSalesInvoiceFormModel
        {
            InvoiceCode = "SI-2026-000125",
            CustomerId = customerId.ToString(),
            CurrencyId = currencyId.ToString(),
            InvoiceDate = new DateOnly(2026, 11, 20),
            RequiredDate = new DateOnly(2026, 11, 24),
            PaymentPlan = "PartialNow",
            Lines =
            [
                new UiSalesLineModel
                {
                    LineType = "Lens", LineNumber = 1,
                    ProductVariantId = lineId.ToString(),
                    WarehouseId = Guid.NewGuid().ToString(),
                    PrescriptionEye = "RightOD", PrescriptionRequired = true,
                    OpticalMeasurementSource = "Manual", SPH = 3m, CYL = -6m, Axis = 150, ADD = 9m,
                    Quantity = 1m, ActualUnitPrice = 150m
                }
            ]
        };
        var result = SalesUiMapper.ToOperationalOrder(model, "SO-2026-000125");
        Assert.Multiple(() =>
        {
            Assert.That(result.CustomerId, Is.EqualTo(customerId));
            Assert.That(result.CurrencyId, Is.EqualTo(currencyId));
            Assert.That(result.RequiredDate, Is.EqualTo(model.RequiredDate));
            Assert.That(result.PaymentPlan, Is.EqualTo(SalesPaymentPlan.PartialNow));
            Assert.That(result.Lines, Has.Count.EqualTo(1));
            Assert.That(result.Lines[0].OpticalSnapshot?.SPH, Is.EqualTo(3m));
            Assert.That(result.Lines[0].OpticalSnapshot?.CYL, Is.EqualTo(-6m));
            Assert.That(result.Lines[0].OpticalSnapshot?.Axis, Is.EqualTo((short)150));
        });
    }

    [Test]
    public void InvoicePreviewUpdatesTaxAndDiscountBeforeSave()
    {
        var invoice = new UiSalesInvoiceFormModel
        {
            CurrencyDecimalPlaces = 2,
            TaxCalculationMode = "Exclusive",
            Lines = [new UiSalesLineModel
            {
                Quantity = 2m, ActualUnitPrice = 100m,
                DiscountType = "Percentage", DiscountValue = 10m, TaxRate = 15m
            }]
        };
        Assert.That(UiSalesInvoicePreviewCalculator.TryRecalculate(invoice), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(invoice.Subtotal, Is.EqualTo(200m));
            Assert.That(invoice.DiscountAmount, Is.EqualTo(20m));
            Assert.That(invoice.TaxAmount, Is.EqualTo(27m));
            Assert.That(invoice.TotalAmount, Is.EqualTo(207m));
            Assert.That(invoice.Lines[0].FinalAmount, Is.EqualTo(207m));
        });
        invoice.TaxCalculationMode = "Inclusive";
        Assert.That(UiSalesInvoicePreviewCalculator.TryRecalculate(invoice), Is.True);
        Assert.That(invoice.TotalAmount, Is.EqualTo(180m));
    }

    [Test]
    public void InvalidPreviewDoesNotReplaceLastValidTotal()
    {
        var model = new UiSalesInvoiceFormModel
        {
            Lines = [new UiSalesLineModel { Quantity = 1m, ActualUnitPrice = 100m }]
        };
        Assert.That(UiSalesInvoicePreviewCalculator.TryRecalculate(model), Is.True);
        model.Lines[0].Quantity = -1m;
        Assert.That(UiSalesInvoicePreviewCalculator.TryRecalculate(model), Is.False);
        Assert.That(model.TotalAmount, Is.EqualTo(100m));
    }

    [Test]
    public void InvoiceCollectionRetryRecordPreservesOriginalVoucherOperation()
    {
        var invoiceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var receiptRequest = new CollectSalesInvoicePaymentRequest(
            [new CheckoutPaymentLineRequest(PaymentMethod.Cash, currencyId, 200m, Guid.NewGuid(), null, null, null, null)],
            operationId);
        var pending = new SalesInvoicePendingCollection(invoiceId, receiptRequest);
        var restored = System.Text.Json.JsonSerializer.Deserialize<SalesInvoicePendingCollection>(
            System.Text.Json.JsonSerializer.Serialize(pending));
        Assert.Multiple(() =>
        {
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.IsValidFor(invoiceId), Is.True);
            Assert.That(restored.Request.OperationId, Is.EqualTo(operationId));
            Assert.That(restored.Request.PaymentLines[0].Amount, Is.EqualTo(200m));
            Assert.That(restored.IsValidFor(Guid.NewGuid()), Is.False);
            Assert.That(restored.Request.PaymentLines[0].CurrencyId, Is.EqualTo(currencyId));
        });
    }

    [Test]
    public void InvoiceDraftRequiresNoPaymentAndKeepsReservedOrderCodeAcrossRetry()
    {
        var model = new UiSalesInvoiceFormModel
        {
            InvoiceCode = "SI-2026-000160", CustomerId = Guid.NewGuid().ToString(),
            CurrencyId = Guid.NewGuid().ToString(), InvoiceDate = new DateOnly(2026, 11, 20),
            Lines = [new UiSalesLineModel { LineType = "Lens", Quantity = 1m, ActualUnitPrice = 250m }]
        };
        var pending = new SalesInvoicePendingCheckout(model.InvoiceCode,
            SalesUiMapper.ToOperationalOrder(model, "SO-2026-000161"), [],
            Guid.NewGuid().ToString("N"), DraftOnly: true);
        var restored = System.Text.Json.JsonSerializer.Deserialize<SalesInvoicePendingCheckout>(
            System.Text.Json.JsonSerializer.Serialize(pending));
        Assert.Multiple(() =>
        {
            Assert.That(restored?.IsValid(), Is.True);
            Assert.That(restored?.DraftOnly, Is.True);
            Assert.That(restored?.Payments, Is.Empty);
            Assert.That(restored?.Order.OrderCode, Is.EqualTo("SO-2026-000161"));
            Assert.That(restored?.CheckoutSubmitted, Is.False);
        });
    }

    [Test]
    public void PendingSaleSurvivesSerializationWithSameOrderCodeAndCheckoutKey()
    {
        var customer = Guid.NewGuid();
        var currency = Guid.NewGuid();
        var model = new UiSalesInvoiceFormModel
        {
            InvoiceCode = "SI-2026-000123", CustomerId = customer.ToString(),
            CurrencyId = currency.ToString(), InvoiceDate = new DateOnly(2026, 11, 20),
            Lines = [new UiSalesLineModel { LineType = "Service", Quantity = 1m, ActualUnitPrice = 10m }]
        };
        var order = SalesUiMapper.ToOperationalOrder(model, "SO-2026-000124");
        var attempt = new SalesInvoicePendingCheckout("SI-2026-000123", order, [], "checkout-operation-id");
        var json = System.Text.Json.JsonSerializer.Serialize(attempt);
        var restored = System.Text.Json.JsonSerializer.Deserialize<SalesInvoicePendingCheckout>(json);
        Assert.Multiple(() =>
        {
            Assert.That(restored?.IsValid(), Is.True);
            Assert.That(restored?.Order.OrderCode, Is.EqualTo("SO-2026-000124"));
            Assert.That(restored?.CheckoutKey, Is.EqualTo("checkout-operation-id"));
            Assert.That(restored?.SourceInvoiceCode, Is.EqualTo("SI-2026-000123"));
            Assert.That(restored?.WithOrder(Guid.NewGuid()).OrderId, Is.Not.Null);
            Assert.That(System.Text.Json.JsonSerializer.Serialize(attempt.ToMarker()), Does.Not.Contain("OpticalSnapshot"));
            Assert.That(System.Text.Json.JsonSerializer.Serialize(attempt.ToMarker()), Does.Not.Contain("PaymentLines"));
        });
    }

    private static SalesInvoiceLineRequest Line(SalesLineType type, bool production = false, Guid? group = null) =>
        new(null, null, group, type, null, null, "اختبار", 1m, 100m,
            SalesDiscountType.None, null, null, null, null, production, null);
}
