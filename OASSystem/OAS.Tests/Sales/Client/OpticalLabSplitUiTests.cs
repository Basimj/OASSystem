using NUnit.Framework;

namespace OAS.Tests.Sales.Client;

[TestFixture]
public sealed class OpticalLabSplitUiTests
{
    [Test]
    public void ListAndProductionHaveSeparateRoutesAndSurfaces()
    {
        var page = Read("OAS.Client/Optical/Pages/OpticalJobsPage.razor");
        Assert.That(page, Does.Contain("@page \"/optical/jobs\""));
        Assert.That(page, Does.Contain("@page \"/optical/jobs/production/{ProductionJobId:guid}\""));
        Assert.That(page, Does.Contain("UiOpticalJobsSurface"));
        Assert.That(page, Does.Contain("UiOpticalJobProductionSurface"));
    }

    [Test]
    public void OrderQueueOnlyShowsTrackingNotWriteOperations()
    {
        var list = Read("Shared/UiLib/Components/Optical/UiOpticalJobsSurface.razor");
        Assert.That(list, Does.Contain("قائمة أوامر المعمل"));
        Assert.That(list, Does.Contain("فتح التصنيع"));
        Assert.That(list, Does.Not.Contain("OnRecordBreakage"));
        Assert.That(list, Does.Not.Contain("OnIssueMaterials"));
    }

    [Test]
    public void ProductionRestrictsActionsByServerState()
    {
        var production = Read("Shared/UiLib/Components/Optical/UiOpticalJobProductionSurface.razor");
        Assert.That(production, Does.Contain("CanIssue =>"));
        Assert.That(production, Does.Contain("CanBreakage =>"));
        Assert.That(production, Does.Contain("CanAssign =>"));
        Assert.That(production, Does.Contain("_section == \"exceptions\""));
    }

    [Test]
    public void RemakeStartRequiresStockVerificationAndClosedJobIsProtected()
    {
        var service = Read("OAS.Application/Sales/OpticalJobs/Services/OpticalJobService.cs");
        Assert.That(service, Does.Contain("optical_remake_job_closed"));
        Assert.That(service, Does.Contain("optical_remake_warehouse_required"));
        Assert.That(service, Does.Contain("optical_remake_materials_unavailable"));
        Assert.That(service, Does.Contain("remake.MarkReady()"));
    }

    [Test]
    public void StatusFilterUsesDomainEnumBeforeEfParameterization()
    {
        var query = Read("OAS.Infrastructure/Sales/Persistence/Queries/OpticalJobQueryService.cs");
        Assert.That(query, Does.Contain("var domainStatus = (OAS.Domain.Sales.Enums.OpticalJobStatus)(byte)request.Status.Value"));
        Assert.That(query, Does.Contain("query.Where(x => x.Status == domainStatus)"));
    }

    private static string Read(string relativePath)
    {
        var folder = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (folder is not null)
        {
            var path = Path.Combine(folder.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path)) return File.ReadAllText(path);
            folder = folder.Parent;
        }
        throw new FileNotFoundException(relativePath);
    }
}
