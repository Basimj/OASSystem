using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Sales.Infrastructure;

[TestFixture]
public sealed class CommercialExtensionsEfConfigurationTests
{
    private DbContextOptions<OasDbContext> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=dummy_commercial_extension_tests;Trusted_Connection=True;")
            .Options;
    }

    [Test]
    public void ReturnQuantities_ArePersistedWithNonNegativeConstraints()
    {
        using var context = new OasDbContext(_options);

        var model = context.GetService<IDesignTimeModel>().Model;
        var salesLine = model.FindEntityType(typeof(SalesInvoiceLine));
        Assert.That(salesLine, Is.Not.Null);
        Assert.That(salesLine!.FindProperty(nameof(SalesInvoiceLine.ReturnedQuantity)), Is.Not.Null);
        Assert.That(salesLine.GetCheckConstraints().Any(x => x.Name == "CK_SalesInvoiceLines_ReturnedQuantity_Valid"), Is.True);

        var receiptLine = model.FindEntityType(typeof(PurchaseReceiptLine));
        Assert.That(receiptLine, Is.Not.Null);
        Assert.That(receiptLine!.FindProperty(nameof(PurchaseReceiptLine.ReturnedQuantity)), Is.Not.Null);
        Assert.That(receiptLine.GetCheckConstraints().Any(x => x.Name == "CK_PurchaseReceiptLines_ReturnedQuantity_Valid"), Is.True);
    }

    [Test]
    public void ReturnsAndCommissionSources_HaveUniqueBusinessKeys()
    {
        using var context = new OasDbContext(_options);

        AssertUniqueIndex(context, typeof(SalesReturn), "UX_SalesReturns_ReturnCode");
        AssertUniqueIndex(context, typeof(PurchaseReturn), "UX_PurchaseReturns_ReturnCode");
        AssertUniqueIndex(context, typeof(CommissionRule), "UX_CommissionRules_Code");
        AssertUniqueIndex(context, typeof(CommissionStatement), "UX_CommissionStatements_StatementCode");
        AssertUniqueIndex(context, typeof(CommissionEntry), "UX_CommissionEntries_SourceLine");
    }

    [Test]
    public void OpticalProduction_AllowsOnlyOneActiveJobPerSalesInvoiceLine()
    {
        using var context = new OasDbContext(_options);
        var entity = context.Model.FindEntityType(typeof(OpticalProductionJob));
        Assert.That(entity, Is.Not.Null);

        var index = entity!.GetIndexes()
            .SingleOrDefault(x => x.GetDatabaseName() == "UX_OpticalProductionJobs_ActiveInvoiceLine");

        Assert.That(index, Is.Not.Null);
        Assert.That(index!.IsUnique, Is.True);
        Assert.That(index.GetFilter(), Is.EqualTo("[IsActive] = 1"));
    }

    private static void AssertUniqueIndex(DbContext context, Type entityType, string databaseName)
    {
        var entity = context.Model.FindEntityType(entityType);
        Assert.That(entity, Is.Not.Null, $"{entityType.Name} is missing from the EF model.");

        var index = entity!.GetIndexes().SingleOrDefault(x => x.GetDatabaseName() == databaseName);
        Assert.That(index, Is.Not.Null, $"Index {databaseName} is missing.");
        Assert.That(index!.IsUnique, Is.True, $"Index {databaseName} must be unique.");
    }
}
