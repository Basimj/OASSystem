using NUnit.Framework;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Services;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class SalesSettlementFxAllocationTests
{
    [Test]
    public async Task SameCurrency_100Usd_UsesInvoiceHistoricalRateAndClosesDocumentAmount()
    {
        var usd = Guid.NewGuid();
        var baseCurrency = Guid.NewGuid();
        var invoice = CreatePostedInvoice(usd, baseCurrency, 100m, 1500m);
        var balance = new SalesInvoiceBalance(
            100m, 0m, 100m, 0m, 100m,
            150000m, 0m, 150000m, 0m, 150000m);
        var validator = new SalesPaymentAllocationTargetValidator(
            new ReadRepository<SalesInvoice>([invoice]),
            new FixedBalanceService(balance));

        var result = await validator.ValidateAsync(invoice.Id, usd, 100m, 160000m);

        Assert.Multiple(() =>
        {
            Assert.That(result.TargetDocumentAmount, Is.EqualTo(100m));
            Assert.That(result.TargetBaseAllocatedAmount, Is.EqualTo(150000m));
        });
    }

    [Test]
    public async Task ThirdCurrency_UsesAllocationDateInvoiceRateThenHistoricalRate()
    {
        var usd = Guid.NewGuid();
        var eur = Guid.NewGuid();
        var baseCurrency = Guid.NewGuid();
        var invoice = CreatePostedInvoice(usd, baseCurrency, 100m, 1500m);
        var balance = new SalesInvoiceBalance(
            100m, 0m, 100m, 0m, 100m,
            150000m, 0m, 150000m, 0m, 150000m);
        var validator = new SalesPaymentAllocationTargetValidator(
            new ReadRepository<SalesInvoice>([invoice]),
            new FixedBalanceService(balance),
            new FixedExchangeRateResolver(usd, 1600m));

        // EUR receipt (or any third currency) has already been valued to 160,000 base.
        // At today's USD rate 1600 this settles 100 USD; historical AR = 100 * 1500.
        var result = await validator.ValidateAsync(invoice.Id, eur, 1m, 160000m);

        Assert.Multiple(() =>
        {
            Assert.That(result.TargetDocumentAmount, Is.EqualTo(100m));
            Assert.That(result.TargetBaseAllocatedAmount, Is.EqualTo(150000m));
        });
    }

    [Test]
    public async Task TwoPartialAllocations_40And60_CloseInvoiceRegardlessOfReceiptBaseRates()
    {
        var usd = Guid.NewGuid();
        var baseCurrency = Guid.NewGuid();
        var invoice = CreatePostedInvoice(usd, baseCurrency, 100m, 1500m);
        var first = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), Guid.NewGuid(), null,
            AllocationTargetDocumentType.SalesInvoice, invoice.Id,
            usd, "USD", 40m, 1600m, 64000m, DateTime.UtcNow,
            60000m, targetDocumentAmount: 40m);
        var second = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), Guid.NewGuid(), null,
            AllocationTargetDocumentType.SalesInvoice, invoice.Id,
            usd, "USD", 60m, 1400m, 84000m, DateTime.UtcNow,
            90000m, targetDocumentAmount: 60m);

        var service = new SalesInvoiceBalanceService(
            new ReadRepository<SalesReturn>([]),
            new ReadRepository<PaymentAllocation>([first, second]));
        var result = await service.GetAsync(invoice);

        Assert.Multiple(() =>
        {
            Assert.That(result.AllocatedAmount, Is.EqualTo(100m));
            Assert.That(result.OutstandingAmount, Is.Zero);
            Assert.That(result.AllocatedBaseAmount, Is.EqualTo(150000m));
            Assert.That(result.OutstandingBaseAmount, Is.Zero);
        });
    }

    [Test]
    public async Task ReversedAllocation_IsExcludedFromBalance()
    {
        var usd = Guid.NewGuid();
        var baseCurrency = Guid.NewGuid();
        var invoice = CreatePostedInvoice(usd, baseCurrency, 100m, 1500m);
        var allocation = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(), Guid.NewGuid(), null,
            AllocationTargetDocumentType.SalesInvoice, invoice.Id,
            usd, "USD", 100m, 1600m, 160000m, DateTime.UtcNow,
            150000m, targetDocumentAmount: 100m);
        allocation.Reverse(DateTime.UtcNow, Guid.NewGuid().ToString("D"), "test");

        var service = new SalesInvoiceBalanceService(
            new ReadRepository<SalesReturn>([]),
            new ReadRepository<PaymentAllocation>([allocation]));
        var result = await service.GetAsync(invoice);

        Assert.Multiple(() =>
        {
            Assert.That(result.AllocatedAmount, Is.Zero);
            Assert.That(result.OutstandingAmount, Is.EqualTo(100m));
            Assert.That(result.OutstandingBaseAmount, Is.EqualTo(150000m));
        });
    }

    private static SalesInvoice CreatePostedInvoice(
        Guid currencyId,
        Guid baseCurrencyId,
        decimal amount,
        decimal exchangeRate)
    {
        var date = new DateOnly(2026, 10, 17);
        var invoice = SalesInvoice.Create(
            Guid.NewGuid(), "SI-SA02-001", Guid.NewGuid(), null, null,
            date, date, currencyId, "USD", "$", 2, exchangeRate, date,
            ExchangeRateType.Accounting, ExchangeRateSource.System,
            TaxCalculationMode.Exclusive, SalesPaymentTermType.Immediate, 0,
            baseCurrencyId, "YER", 2, "SA-02");
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

    private sealed class FixedBalanceService(SalesInvoiceBalance balance) : ISalesInvoiceBalanceService
    {
        public Task<SalesInvoiceBalance> GetAsync(
            SalesInvoice invoice,
            Guid? excludingAllocationId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(balance);
    }

    private sealed class FixedExchangeRateResolver(Guid expectedCurrencyId, decimal rate) : IExchangeRateResolver
    {
        public Task<ExchangeRateResolution> ResolveAsync(
            Guid currencyId,
            DateOnly documentDate,
            ExchangeRateType rateType = ExchangeRateType.Accounting,
            decimal? manualRate = null,
            bool manualOverrideAllowed = false,
            CancellationToken cancellationToken = default)
        {
            Assert.That(currencyId, Is.EqualTo(expectedCurrencyId));
            return Task.FromResult(new ExchangeRateResolution(
                currencyId, "USD", "$", 2, rate, documentDate,
                rateType, ExchangeRateSource.System, false));
        }
    }

    private sealed class ReadRepository<TEntity>(IReadOnlyList<TEntity> items)
        : IReadRepository<TEntity, Guid> where TEntity : Entity<Guid>
    {
        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<TEntity>> ListAsync(
            ISpecification<TEntity>? specification = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<TEntity> query = items;
            if (specification?.Criteria is not null)
                query = query.Where(specification.Criteria.Compile());
            return Task.FromResult<IReadOnlyList<TEntity>>(query.ToArray());
        }

        public async Task<PagedData<TEntity>> GetPageAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default)
        {
            var rows = await ListAsync(specification, cancellationToken);
            return new PagedData<TEntity>(rows, rows.Count);
        }

        public async Task<long> CountAsync(
            ISpecification<TEntity>? specification = null,
            CancellationToken cancellationToken = default) =>
            (await ListAsync(specification, cancellationToken)).Count;

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.Any(x => x.Id == id));
    }
}
