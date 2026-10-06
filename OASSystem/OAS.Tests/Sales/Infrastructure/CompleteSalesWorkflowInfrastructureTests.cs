using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Sales.Infrastructure;

[TestFixture]
public sealed class CompleteSalesWorkflowInfrastructureTests
{
    private static OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=oas_sales_workflow_model_tests;Trusted_Connection=True;")
        .Options);

    [Test]
    public void PaymentPlan_IsMappedAndIndexed_OnOrderAndInvoice()
    {
        using var context = Context();
        var order = context.Model.FindEntityType(typeof(CustomerOrder))!;
        var invoice = context.Model.FindEntityType(typeof(SalesInvoice))!;

        Assert.Multiple(() =>
        {
            Assert.That(order.FindProperty(nameof(CustomerOrder.PaymentPlan)), Is.Not.Null);
            Assert.That(order.GetIndexes().Any(x => x.GetDatabaseName() == "IX_CustomerOrders_PaymentPlan"), Is.True);
            Assert.That(invoice.FindProperty(nameof(SalesInvoice.PaymentPlan)), Is.Not.Null);
            Assert.That(invoice.GetIndexes().Any(x => x.GetDatabaseName() == "IX_SalesInvoices_PaymentPlan"), Is.True);
        });
    }

    [Test]
    public void InvoicePrescriptionSnapshot_AllowsManualMeasurementsWithoutRevision()
    {
        using var context = Context();
        var snapshot = context.Model.FindEntityType(typeof(SalesInvoiceLinePrescriptionSnapshot))!;
        var revision = snapshot.FindProperty(nameof(SalesInvoiceLinePrescriptionSnapshot.PrescriptionRevisionId));

        Assert.That(revision, Is.Not.Null);
        Assert.That(revision!.IsNullable, Is.True);
    }

    [Test]
    public void PaymentAllocation_RequiresExactlyOneTypedSource()
    {
        using var context = Context();
        var model = context.GetService<IDesignTimeModel>().Model;
        var allocation = model.FindEntityType(typeof(PaymentAllocation))!;
        var constraint = allocation.GetCheckConstraints().Single(x => x.Name == "CK_PaymentAllocations_TypedSource");

        Assert.That(constraint.Sql, Does.Contain("= 1"));
        Assert.That(constraint.Sql, Does.Not.Contain("<= 1"));
    }

    [Test]
    public void WorkflowMigration_UsesRawSqlAndContainsRequiredChanges()
    {
        var root = FindSolutionRoot();
        var path = Path.Combine(root, "OAS.Infrastructure", "Persistence", "Migrations", "20261005170000_CompleteSalesWorkflowInfrastructure.cs");
        Assert.That(File.Exists(path), Is.True, path);
        var text = File.ReadAllText(path);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("migrationBuilder.Sql"));
            Assert.That(text, Does.Not.Contain("migrationBuilder.AddColumn("));
            Assert.That(text, Does.Not.Contain("migrationBuilder.CreateIndex("));
            Assert.That(text, Does.Contain("IX_CustomerOrders_PaymentPlan"));
            Assert.That(text, Does.Contain("IX_SalesInvoices_PaymentPlan"));
            Assert.That(text, Does.Contain("EXEC(N'UPDATE [dbo].[tbl_CustomerOrders]"));
            Assert.That(text, Does.Contain("EXEC(N'UPDATE [dbo].[tbl_SalesInvoices]"));
            Assert.That(text, Does.Contain("ALTER COLUMN [PrescriptionRevisionId] uniqueidentifier NULL"));
            Assert.That(text, Does.Contain("CK_PaymentAllocations_TypedSource"));
            Assert.That(text, Does.Contain("= 1"));
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
