using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Foundation.Infrastructure;

[TestFixture]
public sealed class FoundationGapEfConfigurationTests
{
    private static OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=oas_foundation_model_tests;Trusted_Connection=True;")
        .Options);

    [Test]
    public void NewFoundationEntities_AreMappedToExpectedDboTables()
    {
        using var context = Context();
        var expected = new (Type Type, string Table)[]
        {
            (typeof(LensVariantDetail), "tbl_LensVariantDetails"),
            (typeof(CustomerOrderLineOpticalSnapshot), "tbl_CustomerOrderLineOpticalSnapshots"),
            (typeof(CustomerAdvance), "tbl_CustomerAdvances"),
            (typeof(CustomerAdvanceApplication), "tbl_CustomerAdvanceApplications"),
            (typeof(OpticalJob), "tbl_OpticalJobs"),
            (typeof(OpticalJobLine), "tbl_OpticalJobLines")
        };

        foreach (var item in expected)
        {
            var entity = context.Model.FindEntityType(item.Type);
            Assert.That(entity, Is.Not.Null, item.Type.Name);
            Assert.That(entity!.GetSchema(), Is.EqualTo("dbo"));
            Assert.That(entity.GetTableName(), Is.EqualTo(item.Table));
            var rowVersion = entity.FindProperty("RowVersion");
            Assert.That(rowVersion, Is.Not.Null);
            Assert.That(rowVersion!.IsConcurrencyToken, Is.True);
        }
    }

    [Test]
    public void LensVariant_AndOpticalSnapshot_HaveRequiredUniqueIndexesAndChecks()
    {
        using var context = Context();
        var model = context.GetService<IDesignTimeModel>().Model;
        var lens = model.FindEntityType(typeof(LensVariantDetail))!;
        var lensIndex = lens.GetIndexes().Single(x => x.GetDatabaseName() == "UX_LensVariantDetails_ProductVariantId");
        Assert.That(lensIndex.IsUnique, Is.True);
        Assert.That(lens.GetCheckConstraints().Select(x => x.Name), Does.Contain("CK_LensVariantDetails_BaseCurve"));

        var snapshot = model.FindEntityType(typeof(CustomerOrderLineOpticalSnapshot))!;
        var snapshotIndex = snapshot.GetIndexes().Single(x => x.GetDatabaseName() == "UX_CustomerOrderLineOpticalSnapshots_CustomerOrderLineId");
        Assert.That(snapshotIndex.IsUnique, Is.True);
        Assert.That(snapshot.GetCheckConstraints().Select(x => x.Name), Does.Contain("CK_CustomerOrderLineOpticalSnapshots_Source"));
        Assert.That(snapshot.GetCheckConstraints().Select(x => x.Name), Does.Contain("CK_CustomerOrderLineOpticalSnapshots_Axis"));
    }

    [Test]
    public void CustomerAdvance_HasUniqueReceiptSource_AndPaymentAllocationTypedSource()
    {
        using var context = Context();
        var advance = context.Model.FindEntityType(typeof(CustomerAdvance))!;
        Assert.That(advance.GetIndexes().Single(x => x.GetDatabaseName() == "UX_CustomerAdvances_ReceiptVoucherLineId").IsUnique, Is.True);

        var allocation = context.Model.FindEntityType(typeof(PaymentAllocation))!;
        Assert.That(allocation.FindProperty(nameof(PaymentAllocation.CustomerAdvanceApplicationId)), Is.Not.Null);
        Assert.That(allocation.GetIndexes().Any(x => x.GetDatabaseName() == "IX_PaymentAllocations_CustomerAdvanceApplicationId"), Is.True);
    }

    [Test]
    public void OpticalJob_AllowsOneActiveJobPerOrder_AndLineBusinessKeysAreUnique()
    {
        using var context = Context();
        var job = context.Model.FindEntityType(typeof(OpticalJob))!;
        var activeOrder = job.GetIndexes().Single(x => x.GetDatabaseName() == "UX_OpticalJobs_CustomerOrderId");
        Assert.Multiple(() =>
        {
            Assert.That(activeOrder.IsUnique, Is.True);
            Assert.That(activeOrder.GetFilter(), Is.EqualTo("[IsActive] = 1"));
        });

        var line = context.Model.FindEntityType(typeof(OpticalJobLine))!;
        Assert.That(line.GetIndexes().Single(x => x.GetDatabaseName() == "UX_OpticalJobLines_Job_LineNumber").IsUnique, Is.True);
        Assert.That(line.GetIndexes().Single(x => x.GetDatabaseName() == "UX_OpticalJobLines_Job_CustomerOrderLine").IsUnique, Is.True);
    }

    [Test]
    public void PurchaseRequestLine_HasSchedulingAndCustomerDemandLookupIndex()
    {
        using var context = Context();
        var line = context.Model.FindEntityType(typeof(PurchaseRequestLine))!;
        Assert.That(line.FindProperty(nameof(PurchaseRequestLine.ScheduledOrderAtUtc)), Is.Not.Null);
        Assert.That(line.GetIndexes().Any(x => x.GetDatabaseName() == "IX_PurchaseRequestLines_ScheduledOrderAtUtc"), Is.True);
        Assert.That(line.GetIndexes().Any(x => x.GetDatabaseName() == "IX_PurchaseRequestLines_CustomerOrderLine_ProductVariant"), Is.True);
    }
}
