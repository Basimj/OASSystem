using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Returns.Services;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class SalesReturnHistoricalAccountResolverTests
{
    [Test]
    public void PostingRoles_ResolveHistoricalAccounts_RegardlessOfDescriptionLanguageOrText()
    {
        var journalId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var sourceLineId = Guid.NewGuid();
        var ar = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        var tax = Guid.NewGuid();
        var inventory = Guid.NewGuid();
        var cogs = Guid.NewGuid();

        var lines = new[]
        {
            Create(journalId, 1, ar, 120m, 0m, currencyId, "أي وصف للعميل", customerId, null, AccountingPostingRoles.SalesCustomerReceivable),
            Create(journalId, 2, revenue, 0m, 100m, currencyId, "Revenue wording changed completely", null, null, AccountingPostingRoles.SalesRevenue),
            Create(journalId, 3, tax, 0m, 20m, currencyId, "ضريبة بصيغة جديدة", null, null, AccountingPostingRoles.SalesTax),
            Create(journalId, 4, cogs, 60m, 0m, currencyId, "تكلفة", null, sourceLineId, AccountingPostingRoles.SalesCOGS),
            Create(journalId, 5, inventory, 0m, 60m, currencyId, "مخزون", null, sourceLineId, AccountingPostingRoles.SalesInventory)
        };

        Assert.Multiple(() =>
        {
            Assert.That(SalesReturnHistoricalAccountResolver.ResolveCustomerAccount(lines, customerId), Is.EqualTo(ar));
            Assert.That(SalesReturnHistoricalAccountResolver.ResolveRevenueAccount(lines, ar, hasTax: true), Is.EqualTo(revenue));
            Assert.That(SalesReturnHistoricalAccountResolver.ResolveRequiredRoleAccount(lines, AccountingPostingRoles.SalesTax, "x", "x"), Is.EqualTo(tax));
            Assert.That(SalesReturnHistoricalAccountResolver.ResolveSourceLineAccount(lines, sourceLineId, AccountingPostingRoles.SalesCOGS, false, "x", "x"), Is.EqualTo(cogs));
            Assert.That(SalesReturnHistoricalAccountResolver.ResolveSourceLineAccount(lines, sourceLineId, AccountingPostingRoles.SalesInventory, true, "x", "x"), Is.EqualTo(inventory));
        });
    }

    [Test]
    public void LegacyTaxedInvoice_WithAmbiguousHeaderCredits_IsSentToReview_InsteadOfGuessingFromDescription()
    {
        var journalId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var ar = Guid.NewGuid();
        var lines = new[]
        {
            Create(journalId, 1, ar, 120m, 0m, currencyId, "Customer / old localized text", customerId, null, null),
            Create(journalId, 2, Guid.NewGuid(), 0m, 100m, currencyId, "Sales revenue / old text", null, null, null),
            Create(journalId, 3, Guid.NewGuid(), 0m, 20m, currencyId, "Sales tax / old text", null, null, null)
        };

        var ex = Assert.Throws<ConflictException>(() =>
            SalesReturnHistoricalAccountResolver.ResolveRevenueAccount(lines, ar, hasTax: true));

        Assert.That(ex!.Code, Is.EqualTo("sales_return_legacy_revenue_role_review_required"));
    }

    [Test]
    public void LegacyInventoryLine_CanBeReconstructedFromSourceLineDirection_WithoutDescription()
    {
        var journalId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var sourceLineId = Guid.NewGuid();
        var cogs = Guid.NewGuid();
        var inventory = Guid.NewGuid();
        var lines = new[]
        {
            Create(journalId, 1, cogs, 70m, 0m, currencyId, "not-cogs-text", null, sourceLineId, null),
            Create(journalId, 2, inventory, 0m, 70m, currencyId, "not-inventory-text", null, sourceLineId, null)
        };

        Assert.That(
            SalesReturnHistoricalAccountResolver.ResolveSourceLineAccount(lines, sourceLineId, AccountingPostingRoles.SalesCOGS, false, "x", "x"),
            Is.EqualTo(cogs));
        Assert.That(
            SalesReturnHistoricalAccountResolver.ResolveSourceLineAccount(lines, sourceLineId, AccountingPostingRoles.SalesInventory, true, "x", "x"),
            Is.EqualTo(inventory));
    }

    private static JournalEntryLine Create(
        Guid journalId,
        int lineNo,
        Guid accountId,
        decimal debit,
        decimal credit,
        Guid currencyId,
        string description,
        Guid? customerId,
        Guid? sourceLineId,
        string? postingRole) =>
        JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journalId, lineNo, accountId, debit, credit,
            currencyId, "YER", 2, debit, credit, 1m, new DateOnly(2026, 10, 16),
            ExchangeRateType.Accounting, ExchangeRateSource.System, description,
            customerId, null, null, null, null, null, null, sourceLineId, postingRole);
}
