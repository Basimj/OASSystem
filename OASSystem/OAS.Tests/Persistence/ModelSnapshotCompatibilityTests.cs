using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Persistence;

/// <summary>
/// This regression runs without opening SQL Server: it verifies that EF Core can
/// materialize the migration snapshot without shared-type/CLR-entity name collisions.
/// </summary>
[TestFixture]
public sealed class ModelSnapshotCompatibilityTests
{
    [Test]
    public void Ap09FollowUp_UsesPropertyBagEntitiesWithoutClrEntityNameCollision()
    {
        using var db = new OasDbContext(new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=OAS_Snapshot_MetadataOnly;Integrated Security=True;")
            .Options);

        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot;
        Assert.That(snapshot, Is.Not.Null);

        const string jobName = "OAS.Domain.Purchasing.Entities.PurchaseReceiptFollowUpJob";
        const string receiptName = "OAS.Domain.Purchasing.Entities.PurchaseReceipt";
        var model = snapshot!.Model;
        var job = model.FindEntityType(jobName);
        var receipt = model.FindEntityType(receiptName);

        Assert.Multiple(() =>
        {
            Assert.That(job, Is.Not.Null);
            Assert.That(receipt, Is.Not.Null);
            Assert.That(job?.GetTableName(), Is.EqualTo("tbl_PurchasingIntegrationJobs"));
            Assert.That(job?.GetForeignKeys().Any(fk =>
                fk.PrincipalEntityType.Name == receiptName &&
                fk.Properties.Any(p => p.Name == "PurchaseReceiptId")), Is.True);
            Assert.That(job?.FindProperty("RowVersion")?.IsConcurrencyToken, Is.True);
        });
    }
}
