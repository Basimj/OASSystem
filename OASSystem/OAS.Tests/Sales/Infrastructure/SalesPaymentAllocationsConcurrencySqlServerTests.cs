using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Sales.Services;

namespace OAS.Tests.Sales.Infrastructure;

[TestFixture, Category("SalesSqlServer"), NonParallelizable]
public sealed class SalesPaymentAllocationsConcurrencySqlServerTests
{
    private const string AllocationSafetyMigrationId =
        "20261017163000_AddPaymentAllocationConcurrencyAndReversal";
    private string _connection = null!;

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_SALES_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_SALES_TEST_SQLSERVER to run Sales AR SQL Server tests.");
            return;
        }

        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_SA02_{Guid.NewGuid():N}"
        };
        _connection = builder.ConnectionString;

        await using var context = Context();
        await context.Database.MigrateAsync(AllocationSafetyMigrationId);
    }

    [OneTimeTearDown]
    public async Task DropOwnedDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connection)) return;
        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task SameReceiptLineAndInvoice_SecondAllocatorBlocksUntilFirstTransactionEnds()
    {
        var receiptLineId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        await using var first = Context();
        await using var second = Context();
        await first.Database.BeginTransactionAsync();
        await second.Database.BeginTransactionAsync();

        var firstPort = new SalesPaymentAllocationConcurrencyPort(first);
        var secondPort = new SalesPaymentAllocationConcurrencyPort(second);

        await firstPort.LockReceiptLineAndInvoiceAsync(receiptLineId, invoiceId);
        await second.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 500;");

        var exception = Assert.ThrowsAsync<SqlException>(async () =>
            await secondPort.LockReceiptLineAndInvoiceAsync(receiptLineId, invoiceId));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Number, Is.EqualTo(1222));

        await first.Database.RollbackTransactionAsync();
        await second.Database.RollbackTransactionAsync();
    }

    private OasDbContext Context() => new(
        new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer(_connection)
            .Options);
}
