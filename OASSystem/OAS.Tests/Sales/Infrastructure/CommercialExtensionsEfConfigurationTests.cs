using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
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

    [Test]
    public void SalesReturnHardening_HasPostingRoleQualityAndNonSellableWarehouseMetadata()
    {
        using var context = new OasDbContext(_options);
        var model = context.GetService<IDesignTimeModel>().Model;

        var journalLine = model.FindEntityType(typeof(JournalEntryLine));
        Assert.That(journalLine, Is.Not.Null);
        Assert.That(journalLine!.FindProperty(nameof(JournalEntryLine.PostingRole)), Is.Not.Null);
        Assert.That(journalLine.GetIndexes().Any(x => x.GetDatabaseName() == "IX_JournalEntryLines_Journal_Role"), Is.True);

        var returnLine = model.FindEntityType(typeof(SalesReturnLine));
        Assert.That(returnLine, Is.Not.Null);
        Assert.That(returnLine!.FindProperty(nameof(SalesReturnLine.ReturnCondition)), Is.Not.Null);
        Assert.That(returnLine.FindProperty(nameof(SalesReturnLine.StockDisposition)), Is.Not.Null);
        Assert.That(returnLine.FindProperty(nameof(SalesReturnLine.InspectedAtUtc)), Is.Not.Null);
        Assert.That(returnLine.GetCheckConstraints().Any(x => x.Name == "CK_SalesReturnLines_ReturnCondition"), Is.True);
        Assert.That(returnLine.GetCheckConstraints().Any(x => x.Name == "CK_SalesReturnLines_StockDisposition"), Is.True);

        var salesReturn = model.FindEntityType(typeof(SalesReturn));
        Assert.That(salesReturn, Is.Not.Null);
        Assert.That(salesReturn!.FindProperty(nameof(SalesReturn.ReturnPolicyReferenceDate)), Is.Not.Null);
        Assert.That(salesReturn.FindProperty(nameof(SalesReturn.ReturnWindowDaysSnapshot)), Is.Not.Null);
        Assert.That(salesReturn.FindProperty(nameof(SalesReturn.ReturnWindowOverridden)), Is.Not.Null);

        var warehouse = model.FindEntityType(typeof(Warehouse));
        Assert.That(warehouse, Is.Not.Null);
        Assert.That(warehouse!.FindProperty(nameof(Warehouse.AllowsSalesAllocation)), Is.Not.Null);
        Assert.That(warehouse.GetIndexes().Any(x => x.GetDatabaseName() == "IX_Warehouses_SalesAllocation"), Is.True);
    }

    [Test]
    public void SalesReturnHardening_HasDatabaseConcurrencyAndIdempotencyGuards()
    {
        using var context = new OasDbContext(_options);
        var model = context.GetService<IDesignTimeModel>().Model;

        var journal = model.FindEntityType(typeof(JournalEntry));
        Assert.That(journal, Is.Not.Null);
        var sourceIndex = journal!.GetIndexes()
            .SingleOrDefault(x => x.GetDatabaseName() == "UX_JournalEntries_SourceDocument");
        Assert.That(sourceIndex, Is.Not.Null);
        Assert.That(sourceIndex!.IsUnique, Is.True);
        Assert.That(sourceIndex.GetFilter(), Does.Contain("[SourceDocumentId] IS NOT NULL"));
        AssertRowVersion(journal, nameof(JournalEntry.RowVersion));

        var salesReturn = model.FindEntityType(typeof(SalesReturn));
        Assert.That(salesReturn, Is.Not.Null);
        AssertRowVersion(salesReturn!, nameof(SalesReturn.RowVersion));

        var returnLine = model.FindEntityType(typeof(SalesReturnLine));
        Assert.That(returnLine, Is.Not.Null);
        AssertRowVersion(returnLine!, nameof(SalesReturnLine.RowVersion));
    }

    private static void AssertUniqueIndex(DbContext context, Type entityType, string databaseName)
    {
        var entity = context.Model.FindEntityType(entityType);
        Assert.That(entity, Is.Not.Null, $"{entityType.Name} is missing from the EF model.");

        var index = entity!.GetIndexes().SingleOrDefault(x => x.GetDatabaseName() == databaseName);
        Assert.That(index, Is.Not.Null, $"Index {databaseName} is missing.");
        Assert.That(index!.IsUnique, Is.True, $"Index {databaseName} must be unique.");
    }

    private static void AssertRowVersion(IReadOnlyEntityType entity, string propertyName)
    {
        var property = entity.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, $"{entity.ClrType.Name}.{propertyName} is missing.");
        Assert.That(property!.IsConcurrencyToken, Is.True, $"{entity.ClrType.Name}.{propertyName} must be a concurrency token.");
        Assert.That(property.ValueGenerated, Is.EqualTo(ValueGenerated.OnAddOrUpdate));
    }
}
