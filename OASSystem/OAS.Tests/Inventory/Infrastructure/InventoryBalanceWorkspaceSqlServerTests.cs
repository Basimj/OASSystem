using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Inventory.Balances.Specifications;
using OAS.Application.Inventory.Balances.Workspace;
using OAS.Application.Spreadsheets;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Stock;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Queries.Inventory;
using OAS.Infrastructure.Persistence.Repositories.Generic;
using OAS.Infrastructure.Spreadsheets;

namespace OAS.Tests.Inventory.Infrastructure;

[TestFixture, Category("InventorySqlServer"), NonParallelizable]
public sealed class InventoryBalanceWorkspaceSqlServerTests
{
    [Test]
    public async Task Workspace_PreservesInactiveStock_FiltersAndSortsOnServer_ExportsAllMatchingRows()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_INVENTORY_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured)) { Assert.Ignore("Set OAS_INVENTORY_TEST_SQLSERVER."); return; }
        var connection = new SqlConnectionStringBuilder(configured) { InitialCatalog = $"OAS_Balance_Test_{Guid.NewGuid():N}" }.ConnectionString;
        await using var db = new OasDbContext(new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(connection).Options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            var type = new ProductType("WSTYPE", "Workspace Type");
            var category = new ProductCategory("WSCAT", "Workspace Category", type.Id);
            var product = new Product("WSPROD", "Workspace Product", category.Id);
            var unit = new Unit("WSUNIT", "Piece");
            var w1 = new Warehouse("WSW1", "Workspace Warehouse 1");
            var w2 = new Warehouse("WSW2", "Workspace Warehouse 2");
            var v1 = new ProductVariant(product.Id, "WSSKU1", 10, 20, barcode: "WSBAR1", unitId: unit.Id);
            var v2 = new ProductVariant(product.Id, "WSSKU2", 10, 20, unitId: unit.Id);
            var v3 = new ProductVariant(product.Id, "WSSKU3", 10, 20, unitId: unit.Id);
            v2.UpdateDetails(v2.SKU, null, null, null, null, unit.Id, 10, 20, false);
            var b1 = new InventoryBalance(w1.Id, v1.Id, 100, 95, 0, 10, 1000);
            var b2 = new InventoryBalance(w1.Id, v2.Id, 20, 0, 3, 10, 200);
            var b3 = new InventoryBalance(w2.Id, v1.Id, 10, 0, 0, 10, 100);
            db.AddRange(type, category, product, unit, w1, w2, v1, v2, v3, b1, b2, b3);
            await db.SaveChangesAsync();
            var reader = new InventoryBalanceWorkspaceReader(db);
            var filters = new InventoryBalanceFilters { WarehouseId = w1.Id, CategoryId = category.Id };
            var pageRequest = new PageRequest { PageSize = 1, SortBy = "available", SortDirection = SortDirection.Descending };
            var page = await reader.ReadAsync(pageRequest, filters, default);
            Assert.That(page.Page.TotalCount, Is.EqualTo(3));
            Assert.That(page.Page.Items.Single().VariantId, Is.EqualTo(v2.Id));
            Assert.That(page.Page.Items.Single().IsActive, Is.False);
            Assert.That(page.Summary.InventoryValue, Is.EqualTo(1200));
            Assert.That(page.Summary.Units.Single().Available, Is.EqualTo(25));
            var second = await reader.ReadAsync(pageRequest with { PageNumber = 2 }, filters, default);
            Assert.That(second.Page.Items.Single().VariantId, Is.EqualTo(v1.Id));
            var all = await reader.ExportRowsAsync(pageRequest, filters with { WarehouseId = null }, default);
            Assert.That(all.Any(x => x.WarehouseId == w2.Id && x.VariantId == v1.Id && x.OnHand == 10), Is.True);
            var inactive = await reader.ReadAsync(pageRequest, filters with { ActivityStatus = "inactive", PurchaseStatus = "ordered" }, default);
            Assert.That(inactive.Page.Items.Single().VariantId, Is.EqualTo(v2.Id));
            var search = await reader.ReadAsync(pageRequest with { Search = "WSBAR1" }, filters, default);
            Assert.That(search.Page.Items.Single().VariantId, Is.EqualTo(v1.Id));
            var empty = await reader.ReadAsync(pageRequest with { Search = "NO-MATCH-XYZ" }, filters, default);
            Assert.That(empty.Summary.InventoryValue, Is.Zero);
            Assert.That(empty.Page.Items, Is.Empty);
            var workbook = new ClosedXmlSpreadsheetWorkbook();
            var bytes = await new ExportBalanceWorkspaceHandler(reader, workbook).Handle(new ExportBalanceWorkspaceQuery(pageRequest, filters), default);
            var exported = workbook.Read(bytes, [new SpreadsheetSheet("أرصدة المخزون", [new SpreadsheetColumn("SKU"), new SpreadsheetColumn("المتاح")])]);
            Assert.That(exported.Select(x=>x.Values["SKU"]), Is.EqualTo(new[] { v2.SKU, v1.SKU, v3.SKU }));
            var repository = new EfRepository<InventoryBalance, Guid>(db);
            var legacy = await repository.GetPageAsync(InventoryBalanceSpecification.Create(pageRequest, w1.Id));
            Assert.That(legacy.Items.Single().ProductVariantId, Is.EqualTo(v2.Id));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
