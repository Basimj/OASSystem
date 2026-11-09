using System.Globalization;
using NUnit.Framework;
using OAS.UiLib.Core.Models.Purchasing;
using OAS.Contracts.Purchasing.Enums;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchaseInvoiceListCardTests
{
    [TestCase("125000.00", "125,000")]
    [TestCase("125000.50", "125,000.5")]
    [TestCase("125000.05", "125,000.05")]
    [TestCase("0.00", "0")]
    public void Amount_hides_only_trailing_zeros(string amount, string expected)
    {
        var card = NewCard() with { TotalAmount = decimal.Parse(amount, CultureInfo.InvariantCulture) };
        Assert.That(card.TotalAmountDisplay, Is.EqualTo(expected));
    }

    [Test]
    public void Arabic_currency_name_is_used_when_present()
        => Assert.That((NewCard() with { CurrencyNameAr = "الريال اليمني" }).CurrencyDisplayName,
            Is.EqualTo("الريال اليمني"));

    [Test]
    public void Currency_code_is_fallback_only_when_arabic_name_missing()
        => Assert.That(NewCard().CurrencyDisplayName, Is.EqualTo("YER"));

    [Test]
    public void Dates_have_explicit_separators_and_remain_in_order()
    {
        var card = NewCard() with { DueDate = new DateOnly(2026, 12, 9) };
        Assert.Multiple(() =>
        {
            Assert.That(card.InvoiceDateDisplay, Is.EqualTo("2026 / 11 / 08"));
            Assert.That(card.DueDateDisplay, Is.EqualTo("2026 / 12 / 09"));
        });
    }

    [Test]
    public void Missing_due_date_is_not_made_up()
        => Assert.That(NewCard().DueDateDisplay, Is.Null);

    [TestCase(PurchaseInvoiceStatus.Draft, "is-draft")]
    [TestCase(PurchaseInvoiceStatus.Confirmed, "is-confirmed")]
    [TestCase(PurchaseInvoiceStatus.PendingMatchApproval, "is-matching")]
    [TestCase(PurchaseInvoiceStatus.Posted, "is-posted")]
    [TestCase(PurchaseInvoiceStatus.Cancelled, "is-cancelled")]
    public void Statuses_have_distinct_appearance_labels(PurchaseInvoiceStatus status, string expected)
        => Assert.That((NewCard() with { Status = (byte)status }).StatusClass, Is.EqualTo(expected));

    [Test]
    public void Pending_variance_approval_has_clear_next_step()
        => Assert.That((NewCard() with { Status = (byte)PurchaseInvoiceStatus.PendingMatchApproval }).NextStepText,
            Does.Contain("اعتماد الفروقات"));

    [TestCase(0, 0, "لم تبدأ المطابقة", "is-unmatched")]
    [TestCase(2, 0, "مطابقة مكتملة", "is-matched")]
    [TestCase(2, 1, "فروقات قيد المراجعة", "is-pending")]
    public void Matching_status_uses_saved_allocations_not_invoice_document_status(
        int allocations, int unresolved, string text, string css)
    {
        var card = NewCard() with { MatchingAllocationCount = allocations, UnresolvedMatchingAllocationCount = unresolved };
        Assert.Multiple(() =>
        {
            Assert.That(card.MatchingText, Is.EqualTo(text));
            Assert.That(card.MatchingClass, Is.EqualTo(css));
        });
    }

    [Test]
    public void Confirmed_invoice_advises_matching_only_when_none_is_saved()
    {
        var card = NewCard() with { Status = (byte)PurchaseInvoiceStatus.Confirmed, MatchingAllocationCount = 0 };
        Assert.That(card.NextStepText, Does.Contain("شغّل المطابقة"));
    }

    [Test]
    public void Confirmed_invoice_with_saved_matching_does_not_request_repeating_matching()
    {
        var card = NewCard() with { Status = (byte)PurchaseInvoiceStatus.Confirmed,
            MatchingAllocationCount = 2, UnresolvedMatchingAllocationCount = 0 };
        Assert.That(card.NextStepText, Does.Contain("المطابقة مكتملة"));
    }

    [Test]
    public void Missing_matching_data_is_not_guessed_from_workflow_status()
    {
        var card = NewCard() with { Status = (byte)PurchaseInvoiceStatus.Posted };
        Assert.That(card.MatchingText, Does.Contain("غير متاحة"));
    }

    private static UiPurchaseInvoiceCardItem NewCard()
        => new(Guid.NewGuid(), "PI-001", "مورد اختبار", "SUP-001",
            (byte)PurchaseInvoiceStatus.Draft, "مسودة", 125000m, null, "YER",
            new DateOnly(2026, 11, 8), null, 2);
}
