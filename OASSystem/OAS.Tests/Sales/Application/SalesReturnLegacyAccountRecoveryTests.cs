using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Returns.Services;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class SalesReturnLegacyAccountRecoveryTests
{
    private static readonly DateOnly PostingDate = new(2026, 10, 1);

    [Test]
    public void TaxedLegacyInvoice_RecoversRevenueAndTaxFromUniqueHistoricalAmounts()
    {
        var customerId = Guid.NewGuid();
        var ar = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        var tax = Guid.NewGuid();
        var journalId = Guid.NewGuid();
        var currency = Guid.NewGuid();
        var lines = new[]
        {
            Line(journalId, 1, ar, 115m, 0m, currency, customerId),
            Line(journalId, 2, revenue, 0m, 100m, currency),
            Line(journalId, 3, tax, 0m, 15m, currency)
        };
        Assert.That(SalesReturnHistoricalAccountResolver.ResolveCustomerAccount(lines, customerId), Is.EqualTo(ar));
        Assert.That(SalesReturnHistoricalAccountResolver.ResolveRevenueAccount(lines, ar, true, 100m, 15m), Is.EqualTo(revenue));
        Assert.That(SalesReturnHistoricalAccountResolver.ResolveTaxAccount(lines, ar, revenue, 15m), Is.EqualTo(tax));
    }

    [Test]
    public void TaxedLegacyInvoice_AmbiguousCredits_StillStopsForAccountReview()
    {
        var ar = Guid.NewGuid();
        var journal = Guid.NewGuid();
        var currency = Guid.NewGuid();
        var lines = new[]
        {
            Line(journal, 1, ar, 200m, 0m, currency),
            Line(journal, 2, Guid.NewGuid(), 0m, 100m, currency),
            Line(journal, 3, Guid.NewGuid(), 0m, 100m, currency)
        };
        var ex = Assert.Throws<ConflictException>(() =>
            SalesReturnHistoricalAccountResolver.ResolveRevenueAccount(lines, ar, true, 100m, 100m));
        Assert.That(ex?.Code, Is.EqualTo("sales_return_legacy_revenue_role_review_required"));
    }

    [Test]
    public void TaxedLegacyInvoice_UnknownTaxAccount_DoesNotGuess()
    {
        var ar = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        var journal = Guid.NewGuid();
        var currency = Guid.NewGuid();
        var lines = new[]
        {
            Line(journal, 1, ar, 115m, 0m, currency),
            Line(journal, 2, revenue, 0m, 100m, currency),
            Line(journal, 3, Guid.NewGuid(), 0m, 8m, currency),
            Line(journal, 4, Guid.NewGuid(), 0m, 7m, currency)
        };
        var ex = Assert.Throws<ConflictException>(() =>
            SalesReturnHistoricalAccountResolver.ResolveTaxAccount(lines, ar, revenue, 15m));
        Assert.That(ex?.Code, Is.EqualTo("sales_return_legacy_tax_role_review_required"));
    }

    [Test]
    public void Preflight_TaxedLegacyInvoice_ReportsNothingWhenUnique()
    {
        var ar = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        var tax = Guid.NewGuid();
        var customer = Guid.NewGuid();
        var journal = Guid.NewGuid();
        var currency = Guid.NewGuid();
        var lines = new[]
        {
            Line(journal, 1, ar, 115m, 0m, currency, customer),
            Line(journal, 2, revenue, 0m, 100m, currency),
            Line(journal, 3, tax, 0m, 15m, currency)
        };
        Assert.That(SalesReturnHistoricalAccountResolver.TryResolveCustomerAccount(lines, customer, out var resolvedAr), Is.True);
        Assert.That(resolvedAr, Is.EqualTo(ar));
        Assert.That(SalesReturnHistoricalAccountResolver.TryResolveRevenueAccount(lines, ar, 100m, 15m, out var resolvedRevenue), Is.True);
        Assert.That(resolvedRevenue, Is.EqualTo(revenue));
        Assert.That(SalesReturnHistoricalAccountResolver.TryResolveTaxAccount(lines, ar, revenue, 15m, out var resolvedTax), Is.True);
        Assert.That(resolvedTax, Is.EqualTo(tax));
    }

    [Test]
    public void Preflight_AmbiguousLegacyRevenue_ReturnsFalseWithoutThrowing()
    {
        var journal = Guid.NewGuid();
        var currency = Guid.NewGuid();
        var ar = Guid.NewGuid();
        var lines = new[]
        {
            Line(journal, 1, ar, 200m, 0m, currency),
            Line(journal, 2, Guid.NewGuid(), 0m, 100m, currency),
            Line(journal, 3, Guid.NewGuid(), 0m, 100m, currency)
        };
        Assert.That(SalesReturnHistoricalAccountResolver.TryResolveRevenueAccount(lines, ar, 100m, 100m, out var resolved), Is.False);
        Assert.That(resolved, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public void Preflight_MissingInventoryLineage_ReturnsFalseWithoutThrowing()
    {
        var journal = Guid.NewGuid();
        var currency = Guid.NewGuid();
        var lines = new[] { Line(journal, 1, Guid.NewGuid(), 0m, 100m, currency) };
        Assert.That(SalesReturnHistoricalAccountResolver.TryResolveSourceLineAccount(
            lines, Guid.NewGuid(), OAS.Domain.Accounting.AccountingPostingRoles.SalesInventory,
            true, out var result), Is.False);
        Assert.That(result, Is.EqualTo(Guid.Empty));
    }

    private static JournalEntryLine Line(Guid journal, int number, Guid account,
        decimal debit, decimal credit, Guid currency, Guid? customer = null) =>
        JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), journal, number, account,
            debit, credit, currency, "YER", 2, debit, credit, 1m, PostingDate,
            ExchangeRateType.Accounting, ExchangeRateSource.System, null,
            customer, null, null, null, null, null, null, null);
}
