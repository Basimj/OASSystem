using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OAS.Tests.Architecture;

[TestFixture]
public sealed class ClientUiPolicyTests
{
    private static readonly Regex RawHtmlTagRegex = new(
        @"<\s*(div|span|main|section|header|nav|form|input|button|label|table|select|textarea|p|h1|h2|ul|li)(?=\s|/?>)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    [Test]
    public void ClientMustNotContainComponentCssFiles()
    {
        var clientRoot = FindClientRoot();
        var cssFiles = Directory.GetFiles(clientRoot, "*.razor.css", SearchOption.AllDirectories);
        Assert.That(cssFiles, Is.Empty, "Client page/component CSS belongs in OAS.UiLib.");
    }

    [Test]
    public void ClientRazorFilesMustNotRenderRawHtmlElements()
    {
        var clientRoot = FindClientRoot();
        var razorFiles = Directory.GetFiles(clientRoot, "*.razor", SearchOption.AllDirectories);
        var violations = razorFiles.SelectMany(file => RawHtmlTagRegex.Matches(File.ReadAllText(file))
            .Select(match => $"{Path.GetRelativePath(clientRoot, file)}: {match.Value}"))
            .ToArray();
        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
    }

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
