using NUnit.Framework;
using OAS.UiLib.Core.Models.Purchasing;
using OAS.Contracts.Purchasing.Enums;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchaseOrderListCardTests
{
    [Test]
    public void CompletedPercentage_counts_completed_lines_without_mixing_units()
    {
        var card = NewCard(3, 4);
        Assert.That(card.CompletedPercentage, Is.EqualTo(75));
    }

    [Test]
    public void Empty_order_has_zero_progress()
        => Assert.That(NewCard(0, 0).CompletedPercentage, Is.Zero);

    [Test]
    public void Card_displays_arabic_currency_name_instead_of_code()
    {
        var card = NewCard(1, 2) with { CurrencyNameAr = "الريال اليمني" };
        Assert.That(card.CurrencyDisplayName, Is.EqualTo("الريال اليمني"));
    }

    [Test]
    public void Missing_arabic_currency_name_never_shows_technical_code()
    {
        var card = NewCard(1, 2);
        Assert.That(card.CurrencyDisplayName, Is.EqualTo("العملة غير محددة"));
    }

    [TestCase("125000.00", "125,000")]
    [TestCase("125000.50", "125,000.5")]
    [TestCase("125000.05", "125,000.05")]
    [TestCase("0.00", "0")]
    public void TotalAmountDisplay_removes_only_unnecessary_trailing_zeros(string value, string expected)
    {
        var card = NewCard(0, 1) with { TotalAmount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture) };
        Assert.That(card.TotalAmountDisplay, Is.EqualTo(expected));
    }

    [Test]
    public void Card_dates_have_visible_separators_and_preserve_day_month_year()
    {
        var card = NewCard(0, 1) with
        {
            OrderDate = new DateOnly(2026, 11, 6),
            ExpectedDeliveryDate = new DateOnly(2026, 12, 9)
        };
        Assert.Multiple(() =>
        {
            Assert.That(card.OrderDateDisplay, Is.EqualTo("2026 / 11 / 06"));
            Assert.That(card.ExpectedDeliveryDateDisplay, Is.EqualTo("2026 / 12 / 09"));
        });
    }

    [Test]
    public void Missing_expected_delivery_date_does_not_produce_date_text()
    {
        Assert.That(NewCard(0, 1).ExpectedDeliveryDateDisplay, Is.Null);
    }

    [Test]
    public void Group_ids_are_stable_for_server_filters()
    {
        Assert.Multiple(() =>
        {
            Assert.That((byte)PurchaseOrderListGroup.PendingApproval, Is.EqualTo(1));
            Assert.That((byte)PurchaseOrderListGroup.InSupply, Is.EqualTo(2));
            Assert.That((byte)PurchaseOrderListGroup.Completed, Is.EqualTo(3));
        });
    }

    private static UiPurchaseOrderCardItem NewCard(int completed, int total)
        => new(Guid.NewGuid(), "PO-T", "مورد", "مستودع", "مرسل", 4, 1m,
            "YER", new DateOnly(2026, 11, 1), null, completed, completed, total, false);
}
