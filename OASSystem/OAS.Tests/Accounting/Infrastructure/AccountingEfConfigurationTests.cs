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
    public void ModelBuilder_ConfiguresAccountingEntitiesWithCorrectSchemaAndKeys()
    {
        using var context = new OasDbContext(_options);
        var model = context.Model;

        // Verify Account configuration
        var accountEntity = model.FindEntityType(typeof(Account));
        Assert.That(accountEntity, Is.Not.Null);
        Assert.That(accountEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(accountEntity.GetTableName(), Is.EqualTo("Accounts"));
        Assert.That(accountEntity.FindPrimaryKey()?.Properties[0].Name, Is.EqualTo(nameof(Account.Id)));

        // Verify JournalEntry configuration
        var journalEntity = model.FindEntityType(typeof(JournalEntry));
        Assert.That(journalEntity, Is.Not.Null);
        Assert.That(journalEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(journalEntity.GetTableName(), Is.EqualTo("JournalEntries"));

        // Verify JournalEntryLine configuration
        var lineEntity = model.FindEntityType(typeof(JournalEntryLine));
        Assert.That(lineEntity, Is.Not.Null);
        Assert.That(lineEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(lineEntity.GetTableName(), Is.EqualTo("JournalEntryLines"));

        // Verify CashShift configuration
        var shiftEntity = model.FindEntityType(typeof(CashShift));
        Assert.That(shiftEntity, Is.Not.Null);
        Assert.That(shiftEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(shiftEntity.GetTableName(), Is.EqualTo("CashShifts"));

        // Verify FiscalYear configuration
        var yearEntity = model.FindEntityType(typeof(FiscalYear));
        Assert.That(yearEntity, Is.Not.Null);
        Assert.That(yearEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(yearEntity.GetTableName(), Is.EqualTo("FiscalYears"));

        // Verify FiscalPeriod configuration
        var periodEntity = model.FindEntityType(typeof(FiscalPeriod));
        Assert.That(periodEntity, Is.Not.Null);
        Assert.That(periodEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(periodEntity.GetTableName(), Is.EqualTo("FiscalPeriods"));

        // Verify ReceiptVoucher configuration
        var rvEntity = model.FindEntityType(typeof(ReceiptVoucher));
        Assert.That(rvEntity, Is.Not.Null);
        Assert.That(rvEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(rvEntity.GetTableName(), Is.EqualTo("ReceiptVouchers"));

        // Verify PaymentVoucher configuration
        var pvEntity = model.FindEntityType(typeof(PaymentVoucher));
        Assert.That(pvEntity, Is.Not.Null);
        Assert.That(pvEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(pvEntity.GetTableName(), Is.EqualTo("PaymentVouchers"));

        // Verify Expense configuration
        var expEntity = model.FindEntityType(typeof(Expense));
        Assert.That(expEntity, Is.Not.Null);
        Assert.That(expEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(expEntity.GetTableName(), Is.EqualTo("Expenses"));

        // Verify CostCenter configuration
        var ccEntity = model.FindEntityType(typeof(CostCenter));
        Assert.That(ccEntity, Is.Not.Null);
        Assert.That(ccEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(ccEntity.GetTableName(), Is.EqualTo("CostCenters"));

        // Verify PostingProfile configuration
        var ppEntity = model.FindEntityType(typeof(PostingProfile));
        Assert.That(ppEntity, Is.Not.Null);
        Assert.That(ppEntity!.GetSchema(), Is.EqualTo("accounting"));
        Assert.That(ppEntity.GetTableName(), Is.EqualTo("PostingProfiles"));
    }
}
