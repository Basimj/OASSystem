using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Inventory.Reports;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Infrastructure.Inventory.Queries;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Inventory.Infrastructure;

[TestFixture, Category("InventorySqlServer"), NonParallelizable]
public sealed class WarehouseReportSqlServerTests
{
    private string? _connection;
    [OneTimeSetUp]
    public async Task Initialize()
    {
        var configured=Environment.GetEnvironmentVariable("OAS_INVENTORY_TEST_SQLSERVER");
        if(string.IsNullOrWhiteSpace(configured)){Assert.Ignore("Set OAS_INVENTORY_TEST_SQLSERVER to opt in.");return;}
        _connection=new SqlConnectionStringBuilder(configured){InitialCatalog=$"OAS_WH_REPORT_TEST_{Guid.NewGuid():N}"}.ConnectionString;
        await using var db=Context();await db.Database.EnsureCreatedAsync();
    }
    [OneTimeTearDown]
    public async Task Cleanup()
    {
        if(_connection is null)return;
        if(!new SqlConnectionStringBuilder(_connection).InitialCatalog.StartsWith("OAS_WH_REPORT_TEST_",StringComparison.Ordinal))throw new InvalidOperationException("Not an owned test database.");
        await using var db=Context();await db.Database.EnsureDeletedAsync();
    }
    private OasDbContext Context()=>new(new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(_connection!).Options);
    [Test]
    public async Task WarehouseSnapshots_Transfers_AndUncountedLines_AreSeparated()
    {
        await using var db=Context();
        var type=new ProductType("WRT","Test type");var category=new ProductCategory("WRC","Test category",type.Id);
        var product=new Product("WRP","Test product",category.Id);var unit=new Unit("WRU","Piece");
        var variant=new ProductVariant(product.Id,"WRV",10m,20m,unitId:unit.Id);var a=new Warehouse("WRA","A");var b=new Warehouse("WRB","B");
        db.AddRange(type,category,product,unit,variant,a,b);
        var opening=new DateTimeOffset(2026,10,31,0,0,0,TimeSpan.Zero);var date=opening.AddDays(3);
        void Leg(long seq,InventoryTransaction tx,InventoryTransactionLine line,Warehouse w,decimal inbound,decimal outbound,decimal balance)
        {db.Add(new InventoryLedger(seq,tx.Id,line.Id,w.Id,variant.Id,inbound>0?InventoryMovementType.In:InventoryMovementType.Out,inbound,outbound,balance,10m,10m,balance*10m,tx.TransactionDate,tx.TransactionDate,"test"));}
        void Open(long seq,Warehouse w,decimal quantity)
        {var t=new InventoryTransaction($"WR-{seq}",InventoryTransactionType.Opening,opening,null,w.Id);t.Post(opening,"test");var l=new InventoryTransactionLine(t.Id,variant.Id,quantity,10m);db.AddRange(t,l);Leg(seq,t,l,w,quantity,0,quantity);}
        Open(1001,a,100);Open(1002,b,50);
        var transfer=new InventoryTransaction("WR-TRANSFER",InventoryTransactionType.Transfer,date,a.Id,b.Id);transfer.Post(date,"test");
        var line=new InventoryTransactionLine(transfer.Id,variant.Id,20m,10m);db.AddRange(transfer,line);Leg(1003,transfer,line,a,0,20,80);Leg(1004,transfer,line,b,20,0,70);
        var draft=new InventoryTransaction("WR-DRAFT",InventoryTransactionType.Transfer,date,a.Id,b.Id);db.AddRange(draft,new InventoryTransactionLine(draft.Id,variant.Id,5m,0m));
        var count=new StockCount("WR-COUNT",a.Id,new DateOnly(2026,11,4));db.AddRange(count,new StockCountLine(count.Id,variant.Id,80m,10m));
        await db.SaveChangesAsync();
        var service=new WarehouseReportService(db);
        var query=new GetWarehouseReportQuery("cost",new DateOnly(2026,11,1),new DateOnly(2026,11,30),null,variant.Id,product.Id,category.Id,false);
        var report=await service.GetAsync(query,CancellationToken.None);
        var source=report.Movements.Single(x=>x.WarehouseId==a.Id);var dest=report.Movements.Single(x=>x.WarehouseId==b.Id);
        var transfers=await service.GetAsync(query with {Mode="transfers"},CancellationToken.None);
        var counts=await service.GetAsync(query with {Mode="counts"},CancellationToken.None);
        var snapshot=await service.GetAsync(query with {Mode="details"},CancellationToken.None);
        Assert.Multiple(()=>{
            Assert.That(source.Quantity.Opening,Is.EqualTo(100m));Assert.That(dest.Quantity.Opening,Is.EqualTo(50m));
            Assert.That(source.Quantity.TransferOut,Is.EqualTo(20m));Assert.That(dest.Quantity.TransferIn,Is.EqualTo(20m));
            Assert.That(source.Cost.Closing,Is.EqualTo(800m));Assert.That(dest.Cost.Closing,Is.EqualTo(700m));
            Assert.That(transfers.Transfers.Count,Is.EqualTo(2));Assert.That(transfers.Transfers.Single(x=>x.Id==transfer.Id).Cost,Is.EqualTo(200m));
            Assert.That(transfers.Transfers.Single(x=>x.Id==draft.Id).Cost,Is.Null);
            Assert.That(counts.Counts.Single().CountedQuantity,Is.Null);Assert.That(counts.Counts.Single().Variance,Is.Null);
            Assert.That(snapshot.Stocks.Sum(x=>x.Value),Is.EqualTo(1500m));Assert.That(snapshot.Stocks.All(x=>x.Reserved is null),Is.True);
        });
    }
}
