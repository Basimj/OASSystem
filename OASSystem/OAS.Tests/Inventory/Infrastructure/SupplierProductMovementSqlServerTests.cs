using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Inventory.Reports;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Infrastructure.Inventory.Queries;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Inventory.Infrastructure;

// Executes the real grouped SQL queries in an isolated database, not production.
[TestFixture, Category("InventorySqlServer"), NonParallelizable]
public sealed class SupplierProductMovementSqlServerTests
{
    private string? _connection;
    [OneTimeSetUp]
    public async Task Initialize()
    {
        var configured=Environment.GetEnvironmentVariable("OAS_INVENTORY_TEST_SQLSERVER");
        if(string.IsNullOrWhiteSpace(configured)){Assert.Ignore("Set OAS_INVENTORY_TEST_SQLSERVER to opt in.");return;}
        _connection=new SqlConnectionStringBuilder(configured){InitialCatalog=$"OAS_MOVEMENT_TEST_{Guid.NewGuid():N}"}.ConnectionString;
        await using var db=Context();await db.Database.EnsureCreatedAsync();
    }
    [OneTimeTearDown]
    public async Task Cleanup()
    {
        if(_connection is null)return;
        if(!new SqlConnectionStringBuilder(_connection).InitialCatalog.StartsWith("OAS_MOVEMENT_TEST_",StringComparison.Ordinal))throw new InvalidOperationException("Not an owned test database.");
        await using var db=Context();await db.Database.EnsureDeletedAsync();
    }
    private OasDbContext Context()=>new(new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(_connection!).Options);

    [Test]
    public async Task ProductBalances_AndUnattributedSales_DoNotInventSupplierBalances()
    {
        await using var db=Context();
        var type=new ProductType("MVT","Test type");var category=new ProductCategory("MVC","Test category",type.Id);
        var product=new Product("MVP","Test product",category.Id);var unit=new Unit("MVU","Piece");
        var variant=new ProductVariant(product.Id,"MVV",10m,20m,unitId:unit.Id);var warehouse=new Warehouse("MVW","Test warehouse");
        db.AddRange(type,category,product,unit,variant,warehouse);
        var opening=new DateTimeOffset(2026,10,31,0,0,0,TimeSpan.Zero);var sale=opening.AddDays(3);
        void Add(long sequence,InventoryTransactionType kind,DateTimeOffset date,decimal inbound,decimal outbound,decimal balance,bool posted=true)
        {
            var tx=new InventoryTransaction($"MV-{sequence}",kind,date,outbound>0?warehouse.Id:null,inbound>0?warehouse.Id:null);
            if(posted)tx.Post(date,"test");
            var line=new InventoryTransactionLine(tx.Id,variant.Id,inbound+outbound,10m);
            db.AddRange(tx,line,new InventoryLedger(sequence,tx.Id,line.Id,warehouse.Id,variant.Id,inbound>0?InventoryMovementType.In:InventoryMovementType.Out,inbound,outbound,balance,10m,10m,balance*10m,date,date,"test"));
        }
        Add(1,InventoryTransactionType.Opening,opening,100,0,100);
        Add(2,InventoryTransactionType.Sale,sale,0,20,80);
        Add(3,InventoryTransactionType.Receipt,sale,10,0,90);
        Add(4,InventoryTransactionType.Issue,sale.AddDays(1),0,5,85,false);
        Add(5,InventoryTransactionType.Issue,sale.AddMonths(1),0,10,80);
        await db.SaveChangesAsync();
        var q=new GetSupplierProductMovementReportQuery(new DateOnly(2026,11,1),new DateOnly(2026,11,30),variant.Id,product.Id,category.Id,warehouse.Id);
        var result=await new SupplierProductMovementReportService(db).GetAsync(q,CancellationToken.None);
        var item=result.ProductRows.Single();var unknown=result.SupplierRows.Single();
        Assert.Multiple(()=>{
            Assert.That(item.Quantity.Opening,Is.EqualTo(100));Assert.That(item.Quantity.Sales,Is.EqualTo(20));
            Assert.That(item.Quantity.OtherIn,Is.EqualTo(10));Assert.That(item.Quantity.Purchases,Is.Zero);
            Assert.That(item.Quantity.Closing,Is.EqualTo(90));Assert.That(item.Cost.Closing,Is.EqualTo(900));
            Assert.That(unknown.SupplierId,Is.Null);Assert.That(unknown.Quantity.Sales,Is.EqualTo(20));
            Assert.That(unknown.Quantity.Opening,Is.Null);Assert.That(unknown.Quantity.Closing,Is.Null);
        });
    }
}
