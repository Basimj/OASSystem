using System.Text.Json;
using NUnit.Framework;
using OAS.Contracts.Identity.Appearance;

namespace OAS.Tests.Identity;

[TestFixture]
public sealed class AppearanceContractTests
{
    [Test]
    public void NewUser_DefaultsToOceanWithoutLocalStorage()
    {
        var settings = new AppearanceSettingsDto();
        Assert.Multiple(() =>
        {
            Assert.That(settings.ThemePreset, Is.EqualTo("Ocean"));
            Assert.That(settings.SidebarBackground, Is.EqualTo("#1B4659"));
            Assert.That(settings.HeaderLayout, Is.EqualTo("Classic"));
            Assert.That(settings.HeaderOrder, Is.EquivalentTo(new[] { "user", "notifications", "session" }));
        });
    }

    [Test]
    public void AppearanceContract_RoundTripsIndividualTabStripPreferences()
    {
        var original = new AppearanceSettingsDto
        {
            TabsBarBackground = "#000000", TabsBackground = "#FF0000",
            TabsTextColor = "#FF0000", TabsIconColor = "#010101",
            TabsActiveBackground = "#FFFF00", TabsActiveTextColor = "#FFFF00",
            TabsActiveIconColor = "#000000", TabsBorderColor = "#111111",
            TabsStyle = "Pills", TabsSize = "Comfortable"
        };
        var restored = JsonSerializer.Deserialize<AppearanceSettingsDto>(JsonSerializer.Serialize(original));
        Assert.That(restored, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(restored!.TabsStyle, Is.EqualTo("Pills"));
            Assert.That(restored!.TabsSize, Is.EqualTo("Comfortable"));
            Assert.That(restored!.TabsTextColor, Is.EqualTo("#FF0000"));
            Assert.That(restored!.TabsActiveBackground, Is.EqualTo("#FFFF00"));
            Assert.That(restored!.TabsBarBackground, Is.EqualTo("#000000"));
        });
    }

    [Test]
    public void PreviouslySavedThemeWithoutTabs_UsesOceanTabDefaults()
    {
        var legacy = JsonSerializer.Deserialize<AppearanceSettingsDto>("{\"themePreset\":\"Custom\",\"sidebarBackground\":\"#123456\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.That(legacy, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(legacy!.TabsStyle, Is.EqualTo("Classic"));
            Assert.That(legacy!.TabsSize, Is.EqualTo("Compact"));
            Assert.That(legacy!.TabsBarBackground, Is.EqualTo("#FFFFFF"));
        });
    }

    [Test]
    public void AppearanceContract_RoundTripsHeaderOrderAndCustomColors()
    {
        var original = new AppearanceSettingsDto
        {
            ThemePreset = "Custom", SidebarBackground = "#182A3B",
            SidebarIconColor = "#FFFFFF", HeaderOrder = ["session", "user", "notifications"]
        };
        var restored = JsonSerializer.Deserialize<AppearanceSettingsDto>(JsonSerializer.Serialize(original));
        Assert.That(restored, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(restored!.SidebarBackground, Is.EqualTo(original.SidebarBackground));
            Assert.That(restored.HeaderOrder, Is.EqualTo(original.HeaderOrder));
        });
    }
}
