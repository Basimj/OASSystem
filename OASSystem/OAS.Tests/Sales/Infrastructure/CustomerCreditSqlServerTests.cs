using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Sales.Infrastructure;

// Set OAS_SALES_TEST_SQLSERVER to a local SQL Server connection that may create databases.
[TestFixture, Category("SalesSqlServer"),NonParallelizable]
public sealed class CustomerCreditSqlServerTests
{
    private string? _connection;
    [OneTimeSetUp]
    public async Task Initialize()
    {
        var env=Environment.GetEnvironmentVariable("OAS_SALES_TEST_SQLSERVER");
        if(string.IsNullOrWhiteSpace(env))Assert.Ignore("Set OAS_SALES_TEST_SQLSERVER to run SA-03 migration and lock tests.");
        _connection=new SqlConnectionStringBuilder(env){InitialCatalog=$"OAS_SA03_{Guid.NewGuid():N}"}.ConnectionString;
        await using var db=Context();
        await db.Database.MigrateAsync("20261018170000_AddCustomerCreditApplications");
    }
    [OneTimeTearDown]
    public async Task Cleanup()
    {
        if(string.IsNullOrWhiteSpace(_connection))return;
        await using var db=Context();
        await db.Database.EnsureDeletedAsync();
    }
    [Test]
    public async Task MigrationCreatesTypedCreditApplicationRowVersionAndIdempotencyIndexes()
    {
        await using var connection=new SqlConnection(_connection!);
        await connection.OpenAsync();
        foreach(var field in new[]{"AppliedSourceBaseAmount","TargetInvoiceCurrencyAmount","RefundSettlementBaseAmount","ReversalReceiptVoucherLineId","RowVersion"})
        {
            await using var command=connection.CreateCommand();
            command.CommandText="SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.tbl_CustomerCreditApplications') AND name=@name";
            command.Parameters.AddWithValue("@name",field);
            Assert.That(Convert.ToInt32(await command.ExecuteScalarAsync()),Is.EqualTo(1),field);
        }
        await using var index=connection.CreateCommand();
        index.CommandText="SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.tbl_CustomerCreditApplications') AND name=N'UX_CustomerCreditApplications_IdempotencyKey' AND is_unique=1";
        Assert.That(Convert.ToInt32(await index.ExecuteScalarAsync()),Is.EqualTo(1));
    }
    [Test]
    public async Task SerializableReturnKeyRangeLock_BlocksConcurrentSecondDecision()
    {
        var id=Guid.NewGuid();
        await using var first=Context();
        await using var second=Context();
        await first.Database.BeginTransactionAsync();
        await second.Database.BeginTransactionAsync();
        await first.Database.ExecuteSqlInterpolatedAsync($"SELECT [Id] FROM [dbo].[tbl_SalesReturns] WITH(UPDLOCK,HOLDLOCK) WHERE [Id]={id}");
        await second.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 500;");
        var ex=Assert.ThrowsAsync<SqlException>(async()=>await second.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT [Id] FROM [dbo].[tbl_SalesReturns] WITH(UPDLOCK,HOLDLOCK) WHERE [Id]={id}"));
        Assert.That(ex!.Number,Is.EqualTo(1222));
        await first.Database.RollbackTransactionAsync();
        await second.Database.RollbackTransactionAsync();
    }
    private OasDbContext Context()=>new(new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(_connection).Options);
}
