using OAS.Application.Accounting.Posting;
using OAS.Application.Accounting.Posting.Queries;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Tests.Inventory.Fakes;
using NUnit.Framework;

namespace OAS.Tests.Accounting.Application.Posting;

[TestFixture]
public sealed class AP10PurchasingPostingReadinessTests
{
    private static readonly DateOnly Date = new(2026, 10, 18);

    [Test]
    public async Task Readiness_WithTwoExplicitProfilesAndOpenPeriod_IsReady()
    {
        var (query, _) = CreateReadiness();
        var result = await query.Handle(new GetPurchasingPostingReadinessQuery(Date), CancellationToken.None);
        Assert.That(result.IsReady, Is.True,
            string.Join("; ", result.Issues.Where(x => x.Severity == "Block").Select(x => x.Message)));
    }

    [Test]
    public async Task Readiness_RequiredTaxRoleMissing_BlocksPosting()
    {
        var (query, _) = CreateReadiness();
        var result = await query.Handle(new GetPurchasingPostingReadinessQuery(Date, RequirePurchaseTax: true), CancellationToken.None);
        Assert.That(result.IsReady, Is.False);
        Assert.That(result.Issues.Any(x => x.Severity == "Block" && x.Code.Contains("purchase_tax")), Is.True);
        Assert.That(result.Issues.Any(x => x.Code == "purchasing_role_missing" && x.Role == "PurchaseTax"), Is.True);
    }

    [Test]
    public async Task Readiness_InactiveLegacyReceiptProfile_BlocksWithoutActivatingIt()
    {
        var (handler, _) = CreateReadiness(receiptActive: false);
        var report = await handler.Handle(new GetPurchasingPostingReadinessQuery(Date), CancellationToken.None);
        Assert.That(report.IsReady, Is.False);
        Assert.That(report.Issues.Any(x => x.Code == "purchasing_profile_missing" &&
            x.DocumentType == "PurchaseReceipt"), Is.True);
    }

    [Test]
    public void PurchasingRole_DisallowsMisclassifiedInventoryAndGrni()
    {
        var liability = PostingAccount("2100", AccountClass.Liability, NormalBalance.Credit);
        var asset = PostingAccount("1100", AccountClass.Asset, NormalBalance.Debit);
        Assert.That(PurchasingPostingAccountRolePolicy.Validate("Inventory", liability)?.Code,
            Is.EqualTo("purchasing_inventory_class_invalid"));
        Assert.That(PurchasingPostingAccountRolePolicy.Validate("GoodsReceivedNotInvoiced", asset)?.Code,
            Is.EqualTo("purchasing_grni_class_invalid"));
    }

    [Test]
    public void SupplierSubledger_RequiresActiveLiabilityControlParent()
    {
        var control = Account.Create(Guid.NewGuid(), "2200", "حساب رقابة الموردين", null, null, 1,
            AccountClass.Liability, AccountType.Control, NormalBalance.Credit,
            false, true, false, false, true, null);
        var supplierAccount = Account.Create(Guid.NewGuid(), "22000001", "حساب المورد", null, control.Id, 2,
            AccountClass.Liability, AccountType.Subledger, NormalBalance.Credit,
            true, false, false, false, true, null);
        Assert.That(SupplierSubledgerPostingPolicy.Validate(supplierAccount, control), Is.Null);
        Assert.That(SupplierSubledgerPostingPolicy.Validate(supplierAccount, null)?.Code,
            Is.EqualTo("accounting_supplier_control_invalid"));
        control.SetActive(false);
        Assert.That(SupplierSubledgerPostingPolicy.Validate(supplierAccount, control)?.Code,
            Is.EqualTo("accounting_supplier_control_invalid"));
    }

    private static (GetPurchasingPostingReadinessQueryHandler Handler, Guid InventoryId) CreateReadiness(bool receiptActive = true)
    {
        var currency = Currency.Create(Guid.NewGuid(), "YER", "ريال", null, null, 2);
        var inventory = PostingAccount("1100", AccountClass.Asset, NormalBalance.Debit);
        var grni = PostingAccount("2100", AccountClass.Liability, NormalBalance.Credit);
        var receipt = PostingProfile.Create(Guid.NewGuid(), "PUR-REC", "استلام مشتريات", "Purchasing", "PurchaseReceipt", receiptActive);
        var invoice = PostingProfile.Create(Guid.NewGuid(), "PUR-INV", "فاتورة مشتريات", "Purchasing", "PurchaseInvoice", true);
        var roleLines = new[]
        {
            PostingProfileLine.Create(Guid.NewGuid(), receipt.Id, "Inventory", inventory.Id, true),
            PostingProfileLine.Create(Guid.NewGuid(), receipt.Id, "GoodsReceivedNotInvoiced", grni.Id, true),
            PostingProfileLine.Create(Guid.NewGuid(), invoice.Id, "GoodsReceivedNotInvoiced", grni.Id, true)
        };
        var period = FiscalPeriod.Create(Guid.NewGuid(), Guid.NewGuid(), 1, "October",
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), FiscalPeriodStatus.Open, false, false, false);
        var settings = AccountingSettings.Create(currency.Id, grniAccountId: grni.Id, inventoryAccountId: inventory.Id);
        var handler = new GetPurchasingPostingReadinessQueryHandler(
            new FakeGenericRepository<AccountingSettings, Guid>([settings]),
            new FakeGenericRepository<Currency, Guid>([currency]),
            new FakeGenericRepository<FiscalPeriod, Guid>([period]),
            new FakeGenericRepository<PostingProfile, Guid>([receipt, invoice]),
            new FakeGenericRepository<PostingProfileLine, Guid>(roleLines),
            new FakeGenericRepository<Account, Guid>([inventory, grni]),
            new FakeGenericRepository<Supplier, Guid>());
        return (handler, inventory.Id);
    }

    private static Account PostingAccount(string code, AccountClass accountClass, NormalBalance balance) =>
        Account.Create(Guid.NewGuid(), code, code, null, null, 1, accountClass, AccountType.Posting,
            balance, true, false, false, true, true, null);
}
