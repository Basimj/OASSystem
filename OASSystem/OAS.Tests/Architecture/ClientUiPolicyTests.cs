using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OAS.Tests.Architecture;

[TestFixture]
public sealed class ClientUiPolicyTests
{
    private static readonly Regex RawHtmlTagRegex = new(
        @"<\s*(div|span|main|section|header|nav|form|input|button|label|table|select|textarea|p|h1|h2|ul|li)(?=\s|/?>)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    // Legacy violations that pre-date the accounting/foundation work.
    // The policy still blocks any new Client-side CSS/raw HTML outside this explicit baseline.
    private static readonly HashSet<string> LegacyComponentCssFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Identity/Pages/Roles.razor.css",
        "Identity/Pages/Users.razor.css",
        "Inventory/Pages/BrandsPage.razor.css",
        "Inventory/Pages/InventoryLedgerPage.razor.css",
        "Inventory/Pages/InventoryPage.razor.css",
        "Inventory/Pages/InventoryTransactionsPage.razor.css",
        "Inventory/Pages/ProductCategoriesPage.razor.css",
        "Inventory/Pages/ProductsPage.razor.css",
        "Inventory/Pages/ProductTypesPage.razor.css",
        "Inventory/Pages/StockCountsPage.razor.css",
        "Inventory/Pages/WarehousesPage.razor.css",
        "Features/Employees/Pages/JobTitles.razor.css",
        "Identity/Users/Components/UserEditor.razor.css"
    };

    private static readonly HashSet<string> LegacyRawHtmlFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Identity/Pages/Roles.razor",
        "Identity/Pages/Users.razor",
        "Inventory/Pages/BrandsPage.razor",
        "Inventory/Pages/InventoryLedgerPage.razor",
        "Inventory/Pages/InventoryPage.razor",
        "Inventory/Pages/InventoryTransactionsPage.razor",
        "Inventory/Pages/ProductCategoriesPage.razor",
        "Inventory/Pages/ProductsPage.razor",
        "Inventory/Pages/ProductTypesPage.razor",
        "Inventory/Pages/StockCountsPage.razor",
        "Inventory/Pages/WarehousesPage.razor",
        "Features/Employees/Pages/JobTitles.razor",
        "Identity/Users/Components/UserEditor.razor"
    };

    [Test]
    public void ClientMustNotContainComponentCssFiles()
    {
        var clientRoot = FindClientRoot();
        var unexpected = Directory.GetFiles(clientRoot, "*.razor.css", SearchOption.AllDirectories)
            .Select(file => Normalize(Path.GetRelativePath(clientRoot, file)))
            .Where(file => !LegacyComponentCssFiles.Contains(file))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.That(unexpected, Is.Empty,
            "Client page/component CSS belongs in OAS.UiLib. Existing legacy exceptions are baselined; new exceptions are not allowed.");
    }

    [Test]
    public void ClientRazorFilesMustNotRenderRawHtmlElements()
    {
        var clientRoot = FindClientRoot();
        var violations = Directory.GetFiles(clientRoot, "*.razor", SearchOption.AllDirectories)
            .Select(file => new
            {
                File = file,
                Relative = Normalize(Path.GetRelativePath(clientRoot, file))
            })
            .Where(x => !LegacyRawHtmlFiles.Contains(x.Relative))
            .SelectMany(x => RawHtmlTagRegex.Matches(File.ReadAllText(x.File))
                .Select(match => $"{x.Relative}: {match.Value}"))
            .ToArray();

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void LegacyUiPolicyBaselineMustReferenceExistingFiles()
    {
        var clientRoot = FindClientRoot();
        var missing = LegacyComponentCssFiles
            .Concat(LegacyRawHtmlFiles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(relative => !File.Exists(Path.Combine(clientRoot, relative.Replace('/', Path.DirectorySeparatorChar))))
            .OrderBy(relative => relative, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.That(missing, Is.Empty,
            "Remove entries from the legacy UI-policy baseline as soon as the corresponding Client files are migrated to OAS.UiLib.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static string FindClientRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "OAS.Client");
            if (Directory.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate OAS.Client from the test output directory.");
    }
}
