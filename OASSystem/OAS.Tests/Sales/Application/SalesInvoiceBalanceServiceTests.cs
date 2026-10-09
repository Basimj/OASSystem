using NUnit.Framework;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Services;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class SalesInvoiceBalanceServiceTests
{
    [Test]
    public async Task PostedReturn_ReducesOutstandingBeforePaymentAllocations()
    {
        var currencyId = Guid.NewGuid();
        var invoice = CreatePostedInvoice(currencyId, 1000m);
        var salesReturn = CreatePostedReturn(invoice, currencyId, 200m);
        var allocation = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), Guid.NewGuid(), null,
            AllocationTargetDocumentType.SalesInvoice, invoice.Id,
            currencyId, "YER", 300m, 1m, 300m, DateTime.UtcNow, 300m);

        var service = new SalesInvoiceBalanceService(
            new ReadRepository<SalesReturn>([salesReturn]),
            new ReadRepository<PaymentAllocation>([allocation]));

        var balance = await service.GetAsync(invoice);

        Assert.Multiple(() =>
        {
            Assert.That(balance.InvoiceAmount, Is.EqualTo(1000m));
            Assert.That(balance.ReturnedAmount, Is.EqualTo(200m));
            Assert.That(balance.NetInvoiceAmount, Is.EqualTo(800m));
            Assert.That(balance.AllocatedAmount, Is.EqualTo(300m));
            Assert.That(balance.OutstandingAmount, Is.EqualTo(500m));
            Assert.That(balance.OutstandingBaseAmount, Is.EqualTo(500m));
        });
    }


    [Test]
    public async Task AllocationValidator_SameInvoiceCurrency_UsesHistoricalInvoiceRateForTargetBalance()
    {
        var invoiceCurrencyId = Guid.NewGuid();
        var baseCurrencyId = Guid.NewGuid();
        var invoice = CreatePostedInvoice(invoiceCurrencyId, 1000m, 250m, baseCurrencyId);
        var balance = new SalesInvoiceBalance(
            1000m, 0m, 1000m, 0m, 1000m,
            250000m, 0m, 250000m, 0m, 250000m);
        var validator = new SalesPaymentAllocationTargetValidator(
            new ReadRepository<SalesInvoice>([invoice]),
            new FixedBalanceService(balance));

        // The cash receipt may be worth 78,000 base units at today's rate (260), but the
        // historical invoice is settled by 300 * 250 = 75,000 base units.
        var result = await validator.ValidateAsync(
            invoice.Id, invoiceCurrencyId, 300m, 78000m);

        Assert.That(result.TargetBaseAllocatedAmount, Is.EqualTo(75000m));
    }

    private static SalesInvoice CreatePostedInvoice(
        Guid currencyId,
        decimal amount,
        decimal exchangeRate = 1m,
        Guid? baseCurrencyId = null)
    {
        var date = new DateOnly(2026, 10, 7);
        var effectiveBaseCurrencyId = baseCurrencyId ?? currencyId;
        var invoice = SalesInvoice.Create(
            Guid.NewGuid(), "SI-BAL-001", Guid.NewGuid(), null, null,
            date, date, currencyId, "YER", "ر.ي", 2, exchangeRate, date,
            ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, SalesPaymentTermType.Immediate, 0,
            effectiveBaseCurrencyId, "BASE", 2, "balance test");
        invoice.AddLine(SalesInvoiceLine.Create(
            Guid.NewGuid(), invoice.Id, 1, null, null,
            SalesLineType.Service, null, null, null,
            "Service", "Service", "Service", 1m, amount, amount,
            SalesDiscountType.None, null, null, null, null, false, null,
            TaxCalculationMode.Exclusive, 2, exchangeRate, 2));
        invoice.Confirm(DateTimeOffset.UtcNow, "tester");
        invoice.SetJournalEntry(Guid.NewGuid());
        invoice.Post(DateTimeOffset.UtcNow, "tester");
        return invoice;
    }

    private static SalesReturn CreatePostedReturn(SalesInvoice invoice, Guid currencyId, decimal amount)
    {
        var date = new DateOnly(2026, 10, 7);
        var salesReturn = SalesReturn.Create(
            Guid.NewGuid(), "SR-BAL-001", invoice.Id, invoice.CustomerId,
            date, date, currencyId, "YER", 2, 1m, date,
            ExchangeRateType.Accounting, ExchangeRateSource.System,
            currencyId, "YER", 2, "balance test return");
        salesReturn.AddLine(SalesReturnLine.Create(
            Guid.NewGuid(), salesReturn.Id, 1, invoice.Lines.Single().Id,
            SalesLineType.Service, null, null, null, "Service",
            1m, amount, 0m, amount, amount, 0m, amount, null, null));
        salesReturn.Confirm(DateTimeOffset.UtcNow, "tester");
        salesReturn.MarkPosted(Guid.NewGuid(), DateTimeOffset.UtcNow, "tester");
        return salesReturn;
    }

    private sealed class FixedBalanceService(SalesInvoiceBalance balance) : ISalesInvoiceBalanceService
    {
        public Task<SalesInvoiceBalance> GetAsync(
            SalesInvoice invoice,
            Guid? excludingAllocationId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(balance);
    }

    private sealed class ReadRepository<TEntity>(IReadOnlyList<TEntity> items) : IReadRepository<TEntity, Guid>
        where TEntity : Entity<Guid>
    {
        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<TEntity> query = items;
            if (specification?.Criteria is not null)
                query = query.Where(specification.Criteria.Compile());
            return Task.FromResult<IReadOnlyList<TEntity>>(query.ToArray());
        }

        public async Task<PagedData<TEntity>> GetPageAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        {
            var rows = await ListAsync(specification, cancellationToken);
            return new PagedData<TEntity>(rows, rows.Count);
        }

        public async Task<long> CountAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default) =>
            (await ListAsync(specification, cancellationToken)).Count;

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.Any(x => x.Id == id));
    }
}
