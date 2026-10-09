using NUnit.Framework;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.CustomerAdvances.Commands;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.CustomerOrders.Queries.AssessCustomerOrderAvailability;
using OAS.Application.Sales.OpticalJobs.Commands;
using OAS.Application.Sales.OpticalJobs.Queries;
using OAS.Application.Sales.Prescriptions.Queries.CustomerPrescriptionContext;
using OAS.Application.Sales.Prescriptions.Queries.CustomerPrescriptionHistory;

namespace OAS.Tests.Foundation.Architecture;

[TestFixture]
public sealed class FoundationGapPermissionTests
{
    [Test]
    public void SalesFoundationQueries_RequireGranularPermissions()
    {
        Assert.That(new GetCustomerPrescriptionContextQuery(Guid.NewGuid()).RequiredPermissions,
            Is.EquivalentTo(new[] { SalesPermissions.ViewCustomerPrescriptionContext }));
        Assert.That(new GetCustomerPrescriptionHistoryQuery(Guid.NewGuid()).RequiredPermissions,
            Is.EquivalentTo(new[] { SalesPermissions.ViewCustomerPrescriptionContext }));
        Assert.That(new AssessCustomerOrderAvailabilityQuery(Guid.NewGuid()).RequiredPermissions,
            Is.EquivalentTo(new[] { SalesPermissions.ViewStockAvailability }));
    }

    [Test]
    public void OpticalJobRequests_UseOpticalPermissions_NotAccountingPermissions()
    {
        Assert.That(new GetOpticalJobWorkQueueQuery().RequiredPermissions,
            Is.EquivalentTo(new[] { SalesPermissions.OpticalJobs.View }));
        Assert.That(new GetOpticalJobQuery(Guid.NewGuid()).RequiredPermissions,
            Is.EquivalentTo(new[] { SalesPermissions.OpticalJobs.View }));
        Assert.That(new StartOpticalJobCommand(Guid.NewGuid(), new("AA==")).RequiredPermissions,
            Is.EquivalentTo(new[] { SalesPermissions.OpticalJobs.Start }));
        Assert.That(new MarkOpticalJobReadyCommand(Guid.NewGuid(), new("AA==")).RequiredPermissions,
            Is.EquivalentTo(new[] { SalesPermissions.OpticalJobs.MarkReady }));
    }

    [Test]
    public void CustomerAdvanceCommands_RequireDedicatedAccountingPermissions()
    {
        var create = new CreateCustomerAdvanceCommand(new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        var apply = new ApplyCustomerAdvanceCommand(Guid.NewGuid(), new(Guid.NewGuid(), 10m, "AA=="));

        Assert.That(create.RequiredPermissions, Is.EquivalentTo(new[] { AccountingPermissions.CustomerAdvances.Create }));
        Assert.That(apply.RequiredPermissions, Is.EquivalentTo(new[] { AccountingPermissions.CustomerAdvances.Apply }));
    }
}
