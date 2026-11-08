using System;
using System.Globalization;
using NUnit.Framework;
using static OAS.UiLib.Core.Formatting.PurchasingDisplayFormat;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class PurchasingDisplayFormatTests
{
    [TestCase("125000.00", "125,000")]
    [TestCase("125000.50", "125,000.5")]
    [TestCase("125000.05", "125,000.05")]
    [TestCase("0.00", "0")]
    [TestCase("0.50", "0.5")]
    public void Currency_display_removes_only_fractional_padding_within_its_precision(string raw, string expected)
    {
        var value = decimal.Parse(raw, CultureInfo.InvariantCulture);
        Assert.That(value.ToString("#,##0.##", CultureInfo.InvariantCulture), Is.EqualTo(expected));
    }

    [TestCase("3.000", "3")]
    [TestCase("3.125", "3.125")]
    [TestCase("0.050", "0.05")]
    public void Quantity_display_removes_decimal_padding(string raw, string expected)
    {
        var value = decimal.Parse(raw, CultureInfo.InvariantCulture);
        Assert.That(value.ToString("#,##0.###", CultureInfo.InvariantCulture), Is.EqualTo(expected));
    }

    [Test]
    public void Decimal_input_display_does_not_write_insignificant_zeroes()
    {
        var value = decimal.Parse("12.00000000", CultureInfo.InvariantCulture);
        Assert.That(value.ToString("0.############################", CultureInfo.InvariantCulture), Is.EqualTo("12"));
    }

    [Test]
    public void Date_displays_year_month_and_day_in_an_isolated_left_to_right_sequence()
    {
        var date = new DateOnly(2026, 11, 8);
        Assert.That(DisplayDate(date), Is.EqualTo("\u20662026 / 11 / 08\u2069"));
        Assert.That(DisplayShortDate(date), Is.EqualTo("\u206608/11\u2069"));
    }

    [Test]
    public void Date_time_keeps_clock_time_in_same_direction()
    {
        var date = new DateTime(2026, 11, 8, 14, 35, 0);
        Assert.That(DisplayDateTime(date), Is.EqualTo("\u20662026 / 11 / 08  14:35\u2069"));
    }

    [Test]
    public void Iso_string_dates_used_by_shared_mappers_are_isolated_only_when_displayed()
    {
        Assert.That(DisplayDateText("2026-11-08"), Is.EqualTo("\u20662026 / 11 / 08\u2069"));
        Assert.That(DisplayDateText(null), Is.EqualTo("—"));
        Assert.That(DisplayDateText(DisplayDate(new DateOnly(2026, 11, 8))),
            Is.EqualTo("\u20662026 / 11 / 08\u2069"));
    }

    [Test]
    public void Nullable_dates_keep_clear_missing_date_labels()
    {
        DateOnly? missing = null;
        Assert.That(DisplayDate(missing), Is.EqualTo("—"));
        Assert.That(DisplayDate(missing, "مستمر"), Is.EqualTo("مستمر"));
    }
}
