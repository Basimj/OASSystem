using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Accounting.Infrastructure;

[TestFixture]
public class AccountingEfConfigurationTests
{
    private DbContextOptions<OasDbContext> _options = null!;

    [SetUp]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=dummy_tests;Trusted_Connection=True;")
            .Options;
    }

    [Test]
    public void ModelBuilder_ConfiguresAllAccountingEntitiesWithDboSchemaAndExpectedTables()
    {
        using var context = new OasDbContext(_options);
        var model = context.Model;

        var expected = new (Type EntityType, string TableName)[]
        {
            (typeof(Account), "tbl_Accounts"),
            (typeof(FiscalYear), "tbl_FiscalYears"),
            (typeof(FiscalPeriod), "tbl_FiscalPeriods"),
            (typeof(JournalEntry), "tbl_JournalEntries"),
            (typeof(JournalEntryLine), "tbl_JournalEntryLines"),
            (typeof(PostingProfile), "tbl_PostingProfiles"),
            (typeof(PostingProfileLine), "tbl_PostingProfileLines"),
            (typeof(CostCenter), "tbl_CostCenters"),
            (typeof(Customer), "tbl_Customers"),
            (typeof(Supplier), "tbl_Suppliers"),
            (typeof(ReceiptVoucher), "tbl_ReceiptVouchers"),
            (typeof(ReceiptVoucherLine), "tbl_ReceiptVoucherLines"),
            (typeof(PaymentVoucher), "tbl_PaymentVouchers"),
            (typeof(PaymentVoucherLine), "tbl_PaymentVoucherLines"),
            (typeof(PaymentAllocation), "tbl_PaymentAllocations"),
            (typeof(CashAccount), "tbl_CashAccounts"),
            (typeof(BankAccount), "tbl_BankAccounts"),
            (typeof(CashShift), "tbl_CashShifts"),
            (typeof(ExpenseType), "tbl_ExpenseTypes"),
            (typeof(Expense), "tbl_Expenses")
        };

        Assert.Multiple(() =>
        {
            foreach (var (entityType, tableName) in expected)
            {
                var entity = model.FindEntityType(entityType);
                Assert.That(entity, Is.Not.Null, $"{entityType.Name} must be configured in EF Core.");
                Assert.That(entity!.GetSchema(), Is.EqualTo("dbo"), $"{entityType.Name} schema mismatch.");
                Assert.That(entity.GetTableName(), Is.EqualTo(tableName), $"{entityType.Name} table mismatch.");
                Assert.That(entity.FindPrimaryKey(), Is.Not.Null, $"{entityType.Name} must have a primary key.");
            }
        });
    }

    [TestCase(typeof(Account))]
    [TestCase(typeof(FiscalYear))]
    [TestCase(typeof(FiscalPeriod))]
    [TestCase(typeof(JournalEntry))]
    [TestCase(typeof(PostingProfile))]
    [TestCase(typeof(CostCenter))]
    [TestCase(typeof(Customer))]
    [TestCase(typeof(Supplier))]
    [TestCase(typeof(ReceiptVoucher))]
    [TestCase(typeof(PaymentVoucher))]
    [TestCase(typeof(CashAccount))]
    [TestCase(typeof(BankAccount))]
    [TestCase(typeof(CashShift))]
    [TestCase(typeof(ExpenseType))]
    [TestCase(typeof(Expense))]
    public void MutableAggregateRoot_HasRowVersionConcurrencyToken(Type entityType)
    {
        using var context = new OasDbContext(_options);
        var entity = context.Model.FindEntityType(entityType);

        Assert.That(entity, Is.Not.Null);

        var rowVersion = entity!.FindProperty("RowVersion");
        Assert.That(rowVersion, Is.Not.Null, $"{entityType.Name} must expose RowVersion.");
        Assert.That(rowVersion!.IsConcurrencyToken, Is.True, $"{entityType.Name}.RowVersion must be a concurrency token.");
    }
    [Test]
    public void AllAccountingEntities_HaveStandardAuditColumns()
    {
        using var context = new OasDbContext(_options);
        var model = context.Model;

        var entityTypes = new[]
        {
            typeof(Account),
            typeof(FiscalYear),
            typeof(FiscalPeriod),
            typeof(JournalEntry),
            typeof(JournalEntryLine),
            typeof(PostingProfile),
            typeof(PostingProfileLine),
            typeof(CostCenter),
            typeof(Customer),
            typeof(Supplier),
            typeof(ReceiptVoucher),
            typeof(ReceiptVoucherLine),
            typeof(PaymentVoucher),
            typeof(PaymentVoucherLine),
            typeof(PaymentAllocation),
            typeof(CashAccount),
            typeof(BankAccount),
            typeof(CashShift),
            typeof(ExpenseType),
            typeof(Expense)
        };

        Assert.Multiple(() =>
        {
            foreach (var entityType in entityTypes)
            {
                var entity = model.FindEntityType(entityType);
                Assert.That(entity, Is.Not.Null, $"{entityType.Name} must be configured in EF Core.");

                var table = StoreObjectIdentifier.Table(
                    entity!.GetTableName()!,
                    entity.GetSchema());

                AssertAuditColumn(entity, table, "CreatedAtUtc", "CreatedAt");
                AssertAuditColumn(entity, table, "CreatedBy", "CreatedBy");
                AssertAuditColumn(entity, table, "LastModifiedAtUtc", "UpdatedAt");
                AssertAuditColumn(entity, table, "LastModifiedBy", "UpdatedBy");
                AssertAuditColumn(entity, table, "CreatedFromDevice", "CreatedFromDevice");
                AssertAuditColumn(entity, table, "UpdatedFromDevice", "UpdatedFromDevice");
            }
        });
    }

    private static void AssertAuditColumn(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entity,
        StoreObjectIdentifier table,
        string propertyName,
        string columnName)
    {
        var property = entity.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, $"{entity.ClrType.Name}.{propertyName} audit property is missing.");
        Assert.That(property!.GetColumnName(table), Is.EqualTo(columnName),
            $"{entity.ClrType.Name}.{propertyName} must map to {columnName}.");
    }

}
