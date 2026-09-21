using Microsoft.EntityFrameworkCore;
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
    public void ModelBuilder_ConfiguresAllAccountingEntitiesWithAccountingSchemaAndExpectedTables()
    {
        using var context = new OasDbContext(_options);
        var model = context.Model;

        var expected = new (Type EntityType, string TableName)[]
        {
            (typeof(Account), "Accounts"),
            (typeof(FiscalYear), "FiscalYears"),
            (typeof(FiscalPeriod), "FiscalPeriods"),
            (typeof(JournalEntry), "JournalEntries"),
            (typeof(JournalEntryLine), "JournalEntryLines"),
            (typeof(PostingProfile), "PostingProfiles"),
            (typeof(PostingProfileLine), "PostingProfileLines"),
            (typeof(CostCenter), "CostCenters"),
            (typeof(CustomerAccount), "CustomerAccounts"),
            (typeof(SupplierAccount), "SupplierAccounts"),
            (typeof(ReceiptVoucher), "ReceiptVouchers"),
            (typeof(ReceiptVoucherLine), "ReceiptVoucherLines"),
            (typeof(PaymentVoucher), "PaymentVouchers"),
            (typeof(PaymentVoucherLine), "PaymentVoucherLines"),
            (typeof(PaymentAllocation), "PaymentAllocations"),
            (typeof(CashAccount), "CashAccounts"),
            (typeof(BankAccount), "BankAccounts"),
            (typeof(CashShift), "CashShifts"),
            (typeof(ExpenseType), "ExpenseTypes"),
            (typeof(Expense), "Expenses")
        };

        Assert.Multiple(() =>
        {
            foreach (var (entityType, tableName) in expected)
            {
                var entity = model.FindEntityType(entityType);
                Assert.That(entity, Is.Not.Null, $"{entityType.Name} must be configured in EF Core.");
                Assert.That(entity!.GetSchema(), Is.EqualTo("accounting"), $"{entityType.Name} schema mismatch.");
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
    [TestCase(typeof(CustomerAccount))]
    [TestCase(typeof(SupplierAccount))]
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
}
