using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Posting;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Tests.Accounting.Application.Common;
using OAS.Tests.Inventory.Fakes;
using NUnit.Framework;

namespace OAS.Tests.Accounting.Application.Posting;

[TestFixture]
public sealed class ProfileAccountingPostingServiceTests
{
    [Test]
    public async Task PostAsync_UsesPostingProfileAndCreatesBalancedAutomaticJournal()
    {
        var currency = Currency.Create(Guid.NewGuid(), "YER", "ريال", null, "﷼", 2);
        var inventory = CreateAccount("1100", AccountClass.Asset, NormalBalance.Debit);
        var grni = CreateAccount("2100", AccountClass.Liability, NormalBalance.Credit);
        var profile = PostingProfile.Create(Guid.NewGuid(), "PUR-REC", "Purchase Receipt", "Purchasing", "PurchaseReceipt", true);
        var profileLines = new List<PostingProfileLine>
        {
            PostingProfileLine.Create(Guid.NewGuid(), profile.Id, "Inventory", inventory.Id, true),
            PostingProfileLine.Create(Guid.NewGuid(), profile.Id, "GoodsReceivedNotInvoiced", grni.Id, true)
        };
        var period = FiscalPeriod.Create(Guid.NewGuid(), Guid.NewGuid(), 1, "2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), FiscalPeriodStatus.Open, false, false, false);
        var journalRepo = new FakeGenericRepository<JournalEntry, Guid>();
        var service = new ProfileAccountingPostingService(
            journalRepo,
            new FakeGenericRepository<FiscalPeriod, Guid>([period]),
            new FakeGenericRepository<PostingProfile, Guid>([profile]),
            new FakeGenericRepository<PostingProfileLine, Guid>(profileLines),
            new FakeGenericRepository<Account, Guid>([inventory, grni]),
            new FakeGenericRepository<Supplier, Guid>(),
            new FakeGenericRepository<AccountingSettings, Guid>([AccountingSettings.Create(currency.Id)]),
            new FakeGenericRepository<Currency, Guid>([currency]),
            new FakeSequenceNumberGenerator());

        var documentId = Guid.NewGuid();
        var request = new ProfileAccountingPostingRequest(
            "Purchasing", "PurchaseReceipt", documentId,
            new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 5), "Purchase receipt GR-1",
            null, 1m,
            [
                new("PurchaseReceipt", "Inventory", false, 100m, 0m, false, "Inventory"),
                new("PurchaseReceipt", "GoodsReceivedNotInvoiced", false, 0m, 100m, false, "GRNI")
            ]);

        var journalId = await service.PostAsync(request, Guid.NewGuid(), DateTime.UtcNow);

        Assert.That(journalRepo.Items, Has.Count.EqualTo(1));
        var journal = journalRepo.Items.Single();
        Assert.That(journal.Id, Is.EqualTo(journalId));
        Assert.That(journal.SourceModule, Is.EqualTo("Purchasing"));
        Assert.That(journal.SourceDocumentType, Is.EqualTo("PurchaseReceipt"));
        Assert.That(journal.SourceDocumentId, Is.EqualTo(documentId));
        Assert.That(journal.IsBalanced(), Is.True);
        Assert.That(journal.Lines, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task PostAsync_ProvisionMissingPurchaseReceiptProfile_FromPersistedAccountingSettings()
    {
        var currency = Currency.Create(Guid.NewGuid(), "YER", "ريال", null, "﷼", 2);
        var inventory = CreateAccount("1100", AccountClass.Asset, NormalBalance.Debit);
        var grni = CreateAccount("2100", AccountClass.Liability, NormalBalance.Credit);

        var profileRepo = new FakeGenericRepository<PostingProfile, Guid>();
        var profileLineRepo = new FakeGenericRepository<PostingProfileLine, Guid>();

        var settingsEntity = AccountingSettings.Create(
            currency.Id,
            grniAccountId: grni.Id,
            inventoryAccountId: inventory.Id);

        var period = FiscalPeriod.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "2026",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            FiscalPeriodStatus.Open,
            false,
            false,
            false);

        var journalRepo = new FakeGenericRepository<JournalEntry, Guid>();
        var service = new ProfileAccountingPostingService(
            journalRepo,
            new FakeGenericRepository<FiscalPeriod, Guid>([period]),
            profileRepo,
            profileLineRepo,
            new FakeGenericRepository<Account, Guid>([inventory, grni]),
            new FakeGenericRepository<Supplier, Guid>(),
            new FakeGenericRepository<AccountingSettings, Guid>([settingsEntity]),
            new FakeGenericRepository<Currency, Guid>([currency]),
            new FakeSequenceNumberGenerator());

        var request = new ProfileAccountingPostingRequest(
            "Purchasing",
            "PurchaseReceipt",
            Guid.NewGuid(),
            new DateOnly(2026, 10, 6),
            new DateOnly(2026, 10, 6),
            "Receipt",
            null,
            1m,
            [
                new("PurchaseReceipt", "Inventory", false, 50m, 0m, false, "Inventory"),
                new("PurchaseReceipt", "GoodsReceivedNotInvoiced", false, 0m, 50m, false, "GRNI")
            ]);

        _ = await service.PostAsync(request, Guid.NewGuid(), DateTime.UtcNow);

        Assert.That(profileRepo.Items.Count(x =>
            x.Module == "Purchasing" &&
            x.DocumentType == "PurchaseReceipt" &&
            x.IsActive), Is.EqualTo(1));

        var receiptProfile = profileRepo.Items.Single(x => x.Module == "Purchasing" && x.DocumentType == "PurchaseReceipt");
        Assert.That(profileLineRepo.Items.Any(x => x.PostingProfileId == receiptProfile.Id && x.AccountRole == "Inventory" && x.AccountId == inventory.Id), Is.True);
        Assert.That(profileLineRepo.Items.Any(x => x.PostingProfileId == receiptProfile.Id && x.AccountRole == "GoodsReceivedNotInvoiced" && x.AccountId == grni.Id), Is.True);
        Assert.That(journalRepo.Items, Has.Count.EqualTo(1));
        Assert.That(journalRepo.Items.Single().IsBalanced(), Is.True);
    }

    [Test]
    public async Task PostAsync_IsIdempotentBySourceDocument()
    {
        var currency = Currency.Create(Guid.NewGuid(), "YER", "ريال", null, null, 2);
        var inventory = CreateAccount("1100", AccountClass.Asset, NormalBalance.Debit);
        var grni = CreateAccount("2100", AccountClass.Liability, NormalBalance.Credit);
        var profile = PostingProfile.Create(Guid.NewGuid(), "PUR-REC", "Purchase Receipt", "Purchasing", "PurchaseReceipt", true);
        var lines = new List<PostingProfileLine>
        {
            PostingProfileLine.Create(Guid.NewGuid(), profile.Id, "Inventory", inventory.Id, true),
            PostingProfileLine.Create(Guid.NewGuid(), profile.Id, "GoodsReceivedNotInvoiced", grni.Id, true)
        };
        var period = FiscalPeriod.Create(Guid.NewGuid(), Guid.NewGuid(), 1, "2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), FiscalPeriodStatus.Open, false, false, false);
        var journals = new FakeGenericRepository<JournalEntry, Guid>();
        var service = new ProfileAccountingPostingService(
            journals,
            new FakeGenericRepository<FiscalPeriod, Guid>([period]),
            new FakeGenericRepository<PostingProfile, Guid>([profile]),
            new FakeGenericRepository<PostingProfileLine, Guid>(lines),
            new FakeGenericRepository<Account, Guid>([inventory, grni]),
            new FakeGenericRepository<Supplier, Guid>(),
            new FakeGenericRepository<AccountingSettings, Guid>([AccountingSettings.Create(currency.Id)]),
            new FakeGenericRepository<Currency, Guid>([currency]),
            new FakeSequenceNumberGenerator());

        var sourceId = Guid.NewGuid();
        var request = new ProfileAccountingPostingRequest("Purchasing", "PurchaseReceipt", sourceId,
            new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 5), "Receipt", null, 1m,
            [new("PurchaseReceipt", "Inventory", false, 10m, 0m, false, "Inventory"),
             new("PurchaseReceipt", "GoodsReceivedNotInvoiced", false, 0m, 10m, false, "GRNI")]);

        var first = await service.PostAsync(request, Guid.NewGuid(), DateTime.UtcNow);
        var second = await service.PostAsync(request, Guid.NewGuid(), DateTime.UtcNow);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(journals.Items, Has.Count.EqualTo(1));
    }

    private static Account CreateAccount(string code, AccountClass accountClass, NormalBalance normalBalance) =>
        Account.Create(Guid.NewGuid(), code, code, null, null, 1, accountClass, AccountType.Posting,
            normalBalance, true, false, false, true, true, null);
}
