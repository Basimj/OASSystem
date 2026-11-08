using System;
using System.Globalization;
using NUnit.Framework;
using OAS.Client.Inventory.Common;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class InventoryDisplayFormatTests
{
    [TestCase("125000.00", "125,000")]
    [TestCase("125000.50", "125,000.5")]
    [TestCase("125000.05", "125,000.05")]
    [TestCase("20.000", "20")]
    [TestCase("0.050", "0.05")]
    [TestCase("-25.000", "-25")]
    [TestCase("0.000", "0")]
    [TestCase("0.000000123456789", "0.000000123456789")]
    public void Inventory_numbers_remove_only_insignificant_trailing_zeroes(string input, string expected)
    {
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);
        Assert.That(InventoryDisplayFormat.Number(value), Is.EqualTo(expected));
    }

    [TestCase("2000.00", "2000")]
    [TestCase("0.1250", "0.125")]
    [TestCase("12.000", "12")]
    public void Numeric_form_inputs_remain_ungrouped_and_without_padding(string input, string expected)
    {
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);
        Assert.That(InventoryDisplayFormat.InputNumber(value), Is.EqualTo(expected));
    }

    [Test]
    public void Stock_count_date_is_isolated_in_RTL_text()
    {
        Assert.That(InventoryDisplayFormat.Date(new DateOnly(2026, 11, 8)),
            Is.EqualTo("\u20662026 / 11 / 08\u2069"));
    }

    [Test]
    public void Stock_movement_dates_keep_month_year_and_clock_separate()
    {
        var date = new DateTimeOffset(2026, 11, 8, 14, 35, 0, TimeSpan.FromHours(3));
        Assert.That(InventoryDisplayFormat.Date(date), Is.EqualTo("\u20662026 / 11 / 08\u2069"));
        Assert.That(InventoryDisplayFormat.DateTime(date), Is.EqualTo("\u20662026 / 11 / 08  14:35\u2069"));
        Assert.That(InventoryDisplayFormat.DateTime((DateTimeOffset?)null), Is.EqualTo("—"));
    }
}
