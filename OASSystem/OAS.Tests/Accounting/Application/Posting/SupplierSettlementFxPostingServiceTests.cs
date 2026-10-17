using NUnit.Framework;
using OAS.Application.Accounting.Posting;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Tests.Accounting.Application.Common;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Accounting.Application.Posting;

[TestFixture]
public sealed class SupplierSettlementFxPostingServiceTests
{
    [Test]
    public async Task PostAsync_WhenCashBaseExceedsHistoricalAp_PostsBalancedFxLossAndApCredit()
    {
        var fixture = CreateFixture();
        var allocationId = Guid.NewGuid();

        var result = await fixture.Service.PostAsync(
            new(
                allocationId,
                Guid.NewGuid(),
                fixture.SupplierAccount.Id,
                new DateOnly(2026, 10, 17),
                160000m,
                150000m,
                "FX loss"),
            Guid.NewGuid(),
            DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.RealizedExchangeDifferenceBase, Is.EqualTo(10000m));
            Assert.That(result.JournalEntryId, Is.Not.Null);
            Assert.That(fixture.Journals.Items, Has.Count.EqualTo(1));
        });

        var journal = fixture.Journals.Items.Single();
        Assert.Multiple(() =>
        {
            Assert.That(journal.IsBalanced(), Is.True);
            Assert.That(journal.SourceDocumentType, Is.EqualTo("SupplierSettlementFx"));
            Assert.That(journal.SourceDocumentId, Is.EqualTo(allocationId));
            Assert.That(journal.Lines.Single(x => x.AccountId == fixture.LossAccount.Id).DebitAmount, Is.EqualTo(10000m));
            Assert.That(journal.Lines.Single(x => x.AccountId == fixture.SupplierAccount.Id).CreditAmount, Is.EqualTo(10000m));
        });
    }

    [Test]
    public async Task PostAsync_WhenCashBaseBelowHistoricalAp_PostsBalancedApDebitAndFxGain()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.PostAsync(
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                fixture.SupplierAccount.Id,
                new DateOnly(2026, 10, 17),
                140000m,
                150000m,
                "FX gain"),
            Guid.NewGuid(),
            DateTime.UtcNow);

        var journal = fixture.Journals.Items.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.RealizedExchangeDifferenceBase, Is.EqualTo(-10000m));
            Assert.That(journal.IsBalanced(), Is.True);
            Assert.That(journal.Lines.Single(x => x.AccountId == fixture.SupplierAccount.Id).DebitAmount, Is.EqualTo(10000m));
            Assert.That(journal.Lines.Single(x => x.AccountId == fixture.GainAccount.Id).CreditAmount, Is.EqualTo(10000m));
        });
    }

    [Test]
    public async Task PostAsync_WhenNoDifference_DoesNotCreateAdjustmentJournal()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.PostAsync(
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                fixture.SupplierAccount.Id,
                new DateOnly(2026, 10, 17),
                150000m,
                150000m,
                "No FX"),
            Guid.NewGuid(),
            DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.RealizedExchangeDifferenceBase, Is.Zero);
            Assert.That(result.JournalEntryId, Is.Null);
            Assert.That(fixture.Journals.Items, Is.Empty);
        });
    }

    [Test]
    public async Task PostAsync_RepeatedAllocationId_IsIdempotent()
    {
        var fixture = CreateFixture();
        var allocationId = Guid.NewGuid();
        var request = new OAS.Application.Accounting.Abstractions.SupplierSettlementFxPostingRequest(
            allocationId,
            Guid.NewGuid(),
            fixture.SupplierAccount.Id,
            new DateOnly(2026, 10, 17),
            160000m,
            150000m,
            "Idempotent FX");

        var first = await fixture.Service.PostAsync(request, Guid.NewGuid(), DateTime.UtcNow);
        var second = await fixture.Service.PostAsync(request, Guid.NewGuid(), DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(second.JournalEntryId, Is.EqualTo(first.JournalEntryId));
            Assert.That(fixture.Journals.Items, Has.Count.EqualTo(1));
        });
    }

    private static Fixture CreateFixture()
    {
        var baseCurrency = Currency.Create(Guid.NewGuid(), "YER", "ريال", null, null, 2);
        var supplierAccount = CreateAccount("210100", AccountClass.Liability, NormalBalance.Credit);
        var gainAccount = CreateAccount("470100", AccountClass.Revenue, NormalBalance.Credit);
        var lossAccount = CreateAccount("570100", AccountClass.Expense, NormalBalance.Debit);
        var settings = AccountingSettings.Create(
            baseCurrency.Id,
            exchangeGainAccountId: gainAccount.Id,
            exchangeLossAccountId: lossAccount.Id);
        var period = FiscalPeriod.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, "2026",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            FiscalPeriodStatus.Open, false, false, false);
        var journals = new FakeGenericRepository<JournalEntry, Guid>();

        var service = new SupplierSettlementFxPostingService(
            journals,
            new FakeGenericRepository<FiscalPeriod, Guid>([period]),
            new FakeGenericRepository<Account, Guid>([supplierAccount, gainAccount, lossAccount]),
            new FakeGenericRepository<AccountingSettings, Guid>([settings]),
            new FakeGenericRepository<Currency, Guid>([baseCurrency]),
            new FakeInventorySequenceNumberGenerator());

        return new Fixture(service, journals, supplierAccount, gainAccount, lossAccount);
    }

    private static Account CreateAccount(string code, AccountClass accountClass, NormalBalance normalBalance) =>
        Account.Create(Guid.NewGuid(), code, code, null, null, 1, accountClass, AccountType.Posting,
            normalBalance, true, false, false, true, true, null);

    private sealed record Fixture(
        SupplierSettlementFxPostingService Service,
        FakeGenericRepository<JournalEntry, Guid> Journals,
        Account SupplierAccount,
        Account GainAccount,
        Account LossAccount);
}
