using System.Text.RegularExpressions;
using NUnit.Framework;
namespace OAS.Tests.Accounting.Client;
[TestFixture]
public sealed class AccountingUiPolicyTests
{
    [Test] public void AccountingClientContainsNoHtmlOrCss()
    {
        var directory=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(directory is not null && !Directory.Exists(Path.Combine(directory.FullName,"OAS.Client"))) directory=directory.Parent;
        Assert.That(directory,Is.Not.Null);
        var root=Path.Combine(directory!.FullName,"OAS.Client","Accounting");
        Assert.That(Directory.GetFiles(root,"*.css",SearchOption.AllDirectories),Is.Empty);
        var regex=new Regex(@"<\s*/?\s*[a-z][a-z0-9-]*(?=\s|/?>)",RegexOptions.CultureInvariant);
        var violations=Directory.GetFiles(root,"*.razor",SearchOption.AllDirectories).SelectMany(file=>regex.Matches(File.ReadAllText(file)).Select(m=>$"{file}: {m.Value}"));
        Assert.That(violations,Is.Empty);
    }
}
