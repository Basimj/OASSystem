using NUnit.Framework;
using OAS.Client.Purchasing.Common;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.FiscalPeriods;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchaseReceiptPostingPeriodValidationTests
{
    private static readonly DateOnly PostingDate = new(2026, 11, 7);

    private static FiscalPeriodDto Period(
        DateOnly start, DateOnly end,
        FiscalPeriodStatus status = FiscalPeriodStatus.Open,
        bool inventoryLocked = false, bool accountingLocked = false) =>
        new(Guid.NewGuid(), Guid.NewGuid(), 11, "نوفمبر 2026", start, end, status,
            false, inventoryLocked, accountingLocked, null, null, "rv");

    [Test]
    public void NoPeriod_ShowsArabicDateAndSetupGuidance()
    {
        var result = PurchaseReceiptPostingPeriodValidation.Validate(PostingDate, []);
        Assert.That(result, Does.Contain("2026/11/07"));
        Assert.That(result, Does.Contain("الفترات المالية"));
    }

    [Test]
    public void OpenMatchingPeriod_AllowsPosting()
    {
        var period = Period(new(2026, 11, 1), new(2026, 11, 30));
        Assert.That(PurchaseReceiptPostingPeriodValidation.Validate(PostingDate, [period]), Is.Null);
    }

    [Test]
    public void DateOutsideAllPeriods_IsNotCovered()
    {
        var period = Period(new(2026, 12, 1), new(2026, 12, 31));
        Assert.That(PurchaseReceiptPostingPeriodValidation.Validate(PostingDate, [period]),
            Does.Contain("لا توجد فترة مالية"));
    }

    [Test]
    public void OverlappingPeriods_BlocksPosting()
    {
        var a = Period(new(2026, 11, 1), new(2026, 11, 30));
        var b = Period(new(2026, 11, 7), new(2026, 11, 20));
        Assert.That(PurchaseReceiptPostingPeriodValidation.Validate(PostingDate, [a, b]),
            Does.Contain("أكثر من فترة"));
    }

    [Test]
    public void InventoryLock_BlocksPosting()
    {
        var period = Period(new(2026, 11, 1), new(2026, 11, 30), inventoryLocked: true);
        Assert.That(PurchaseReceiptPostingPeriodValidation.Validate(PostingDate, [period]),
            Does.Contain("قفل المخزون"));
    }

    [Test]
    public void AccountingLock_BlocksPosting()
    {
        var period = Period(new(2026, 11, 1), new(2026, 11, 30), accountingLocked: true);
        Assert.That(PurchaseReceiptPostingPeriodValidation.Validate(PostingDate, [period]),
            Does.Contain("قفل المحاسبة"));
    }

    [Test]
    public void ClosedPeriod_BlocksPosting()
    {
        var period = Period(new(2026, 11, 1), new(2026, 11, 30), FiscalPeriodStatus.Closed);
        Assert.That(PurchaseReceiptPostingPeriodValidation.Validate(PostingDate, [period]),
            Does.Contain("مغلقة"));
    }

    [Test]
    public void SoftClosedUnlockedPeriod_FollowsServerBehavior()
    {
        var period = Period(new(2026, 11, 1), new(2026, 11, 30), FiscalPeriodStatus.SoftClosed);
        Assert.That(PurchaseReceiptPostingPeriodValidation.Validate(PostingDate, [period]), Is.Null);
    }

    [Test]
    public void UnrelatedServerError_IsNotMistakenForPeriodError()
    {
        Assert.That(PurchaseReceiptPostingPeriodValidation.Explain("unexpected_error", PostingDate), Is.Null);
    }
}
