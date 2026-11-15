using NUnit.Framework;
using OAS.Application.Sales.SalesInvoices.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class SalesInvoiceDashboardFilterTests
{
    private static SalesInvoice CreateDraft(Guid customer, DateOnly date, string code) =>
        SalesInvoice.Create(
            Guid.NewGuid(), code, customer, null, null,
            date, date, Guid.NewGuid(), "SAR", "ر.س", 2,
            1m, date, ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, SalesPaymentTermType.Immediate, 0,
            Guid.NewGuid(), "SAR", 2, "فحص الفلترة");

    [Test]
    public void StatusAndDates_KeepAllActiveFilters_UsingDomainStatus()
    {
        var owner = Guid.NewGuid();
        var inRange = CreateDraft(owner, new DateOnly(2026, 11, 15), "INV-OK");
        var outOfRange = CreateDraft(owner, new DateOnly(2026, 10, 15), "INV-OLD");
        var anotherCustomer = CreateDraft(Guid.NewGuid(), new DateOnly(2026, 11, 15), "INV-OTHER");
        var filters = new PageRequest
        {
            Status = "Draft", DateFrom = new DateOnly(2026, 11, 1),
            DateTo = new DateOnly(2026, 11, 30), PageSize = 10
        };
        var spec = new SalesInvoiceSpecificationFactory().CreatePageSpecification(filters, owner);
        var predicate = spec.Criteria!.Compile();
        Assert.Multiple(() =>
        {
            Assert.That(predicate(inRange), Is.True);
            Assert.That(predicate(outOfRange), Is.False);
            Assert.That(predicate(anotherCustomer), Is.False);
            Assert.That(spec.Take, Is.EqualTo(10));
        });
    }

    [Test]
    public void SearchByMatchedCustomer_CanFindInvoiceWithoutInvoiceCodeMatch()
    {
        var customerId = Guid.NewGuid();
        var invoice = CreateDraft(customerId, new DateOnly(2026, 11, 15), "INV-X");
        var spec = new SalesInvoiceSpecificationFactory().CreatePageSpecification(
            new PageRequest { Search = "عميل مسجل" }, null, [customerId]);
        Assert.That(spec.Criteria!.Compile()(invoice), Is.True);
    }
}
