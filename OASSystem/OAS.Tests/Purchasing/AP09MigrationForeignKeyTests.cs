using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NUnit.Framework;
using OAS.Infrastructure.Persistence.Migrations;

namespace OAS.Tests.Purchasing;

[TestFixture]
public sealed class AP09MigrationForeignKeyTests
{
    [Test]
    public void IntegrationJobs_ForeignKeys_Reference_Real_Dbo_Tables()
    {
        // Inspect the actual EF migration operations: this catches accidental positional
        // argument swaps (schema/table/column) without requiring a SQL Server instance.
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var migration = new AddPurchasingIntegrationJobs();
        var up = typeof(AddPurchasingIntegrationJobs).GetMethod(
            "Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(up, Is.Not.Null);
        up!.Invoke(migration, [builder]);

        var create = builder.Operations.OfType<CreateTableOperation>()
            .Single(x => x.Name == "tbl_PurchasingIntegrationJobs");
        Assert.That(create.Schema, Is.EqualTo("dbo"));
        Assert.That(create.ForeignKeys, Has.Count.EqualTo(3));

        AssertForeignKey(create, "FK_PurchasingIntegrationJobs_Receipts",
            "PurchaseReceiptId", "tbl_PurchaseReceipts");
        AssertForeignKey(create, "FK_PurchasingIntegrationJobs_CustomerOrders",
            "CustomerOrderId", "tbl_CustomerOrders");
        AssertForeignKey(create, "FK_PurchasingIntegrationJobs_RequestLines",
            "PurchaseRequestLineId", "tbl_PurchaseRequestLines");
    }

    private static void AssertForeignKey(
        CreateTableOperation create, string name, string localColumn, string principalTable)
    {
        var key = create.ForeignKeys.Single(x => x.Name == name);
        Assert.Multiple(() =>
        {
            Assert.That(key.Columns, Is.EqualTo(new[] { localColumn }));
            Assert.That(key.PrincipalSchema, Is.EqualTo("dbo"));
            Assert.That(key.PrincipalTable, Is.EqualTo(principalTable));
            Assert.That(key.PrincipalColumns, Is.EqualTo(new[] { "Id" }));
        });
    }
}
