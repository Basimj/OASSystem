using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Services;
using OAS.Client.Sales.Mapping;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.UiLib.Core.Models.Sales;

namespace OAS.Tests.Sales;

[TestFixture]
public sealed class LinkedSalesDraftEditingTests
{
    [Test]
    public void LinkedOrderAndInvoice_AreEditableOnlyAsDrafts()
    {
        var order = MakeOrder();
        var invoice = MakeInvoice(order);
        Assert.DoesNotThrow(() => LinkedSalesDraftSynchronizer.EnsureEditable(order, invoice));

        invoice.AddLine(SalesInvoiceLine.Create(
            Guid.NewGuid(), invoice.Id, 1, null, null, SalesLineType.Service,
            null, null, null, "خدمة", "خدمة", "خدمة",
            1m, 1m, 1m, SalesDiscountType.None, null, null,
            null, null, false, null, TaxCalculationMode.Exclusive, 2, 1m, 2));
        invoice.Confirm(DateTimeOffset.UtcNow, "test");
        Assert.Throws<ConflictException>(() => LinkedSalesDraftSynchronizer.EnsureEditable(order, invoice));
    }

    [Test]
    public void LinkedInvoiceDraft_CannotBeEditedAfterOrderConfirmation()
    {
        var order = MakeOrder();
        var invoice = MakeInvoice(order);
        order.AddLine(CustomerOrderLine.Create(
            Guid.NewGuid(), order.Id, 1, null, SalesLineType.Service,
            null, null, "خدمة", 1m, 1m, 1m, SalesDiscountType.None,
            null, null, null, null, false, null, TaxCalculationMode.Exclusive, 2));
        order.Confirm(CustomerOrderStatus.Confirmed, DateTimeOffset.UtcNow, "test");
        Assert.Throws<ConflictException>(() => LinkedSalesDraftSynchronizer.EnsureEditable(order, invoice));
    }

    [Test]
    public void EditingOrderDraft_DoesNotErasePartialPaymentPlan()
    {
        var form = new UiCustomerOrderFormModel
        {
            CustomerId = Guid.NewGuid().ToString(),
            CurrencyId = Guid.NewGuid().ToString(),
            OrderDate = new DateOnly(2026, 11, 20),
            PaymentPlan = "PartialNow"
        };
        Assert.That(SalesUiMapper.ToUpdate(form).PaymentPlan,
            Is.EqualTo(OAS.Contracts.Sales.Enums.SalesPaymentPlan.PartialNow));
    }

    private static CustomerOrder MakeOrder()
    {
        var currency = Guid.NewGuid();
        return CustomerOrder.Create(Guid.NewGuid(), "SO-2026-TEST", Guid.NewGuid(), null,
            new DateOnly(2026, 11, 20), null, currency, "YER", "ر.ي", 2,
            1m, new DateOnly(2026, 11, 20), ExchangeRateType.Accounting,
            ExchangeRateSource.System, TaxCalculationMode.Exclusive,
            SalesPaymentPlan.FullNow, 0);
    }

    private static SalesInvoice MakeInvoice(CustomerOrder order) =>
        SalesInvoice.Create(Guid.NewGuid(), "SI-2026-TEST", order.CustomerId,
            order.Id, null, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 20),
            order.CurrencyId, "YER", "ر.ي", 2, 1m, new DateOnly(2026, 11, 20),
            ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, SalesPaymentPlan.FullNow, 0,
            order.CurrencyId, "YER", 2);
}
