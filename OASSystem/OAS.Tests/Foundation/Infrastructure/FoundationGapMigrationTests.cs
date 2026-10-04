using NUnit.Framework;

namespace OAS.Tests.Foundation.Infrastructure;

[TestFixture]
public sealed class FoundationGapMigrationTests
{
    [Test]
    public void FoundationMigration_UsesRawSqlAndContainsAllRequiredObjects()
    {
        var root = FindSolutionRoot();
        var path = Path.Combine(root, "OAS.Infrastructure", "Persistence", "Migrations", "20261004160000_FoundationGapCompletionInfrastructure.cs");
        Assert.That(File.Exists(path), Is.True, path);
        var text = File.ReadAllText(path);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("migrationBuilder.Sql"));
            Assert.That(text, Does.Not.Contain("migrationBuilder.CreateTable("));
            Assert.That(text, Does.Not.Contain("migrationBuilder.AddColumn("));
            Assert.That(text, Does.Not.Contain("migrationBuilder.CreateIndex("));
            Assert.That(text, Does.Contain("tbl_LensVariantDetails"));
            Assert.That(text, Does.Contain("tbl_CustomerOrderLineOpticalSnapshots"));
            Assert.That(text, Does.Contain("tbl_CustomerAdvances"));
            Assert.That(text, Does.Contain("tbl_CustomerAdvanceApplications"));
            Assert.That(text, Does.Contain("tbl_OpticalJobs"));
            Assert.That(text, Does.Contain("tbl_OpticalJobLines"));
            Assert.That(text, Does.Contain("ScheduledOrderAtUtc"));
            Assert.That(text, Does.Contain("CustomerAdvanceApplicationId"));
            Assert.That(text, Does.Contain("ON DELETE NO ACTION"));
        });
    }

    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OASSystem.sln")) || Directory.Exists(Path.Combine(current.FullName, "OAS.Infrastructure")))
                return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate OASSystem solution root.");
    }
}
