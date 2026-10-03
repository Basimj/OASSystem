using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Domain.Features.Employees.EndOfService;
using OAS.Domain.Features.Employees.Payroll;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Features.Employees.Integration;

[TestFixture]
public sealed class PayrollEfConfigurationTests
{
    private static DbContextOptions<OasDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=dummy_hr_payroll_tests;Trusted_Connection=True;")
            .Options;

    [Test]
    public void Phase3Entities_AreMappedToHrSchema_WithConcurrencyTokens()
    {
        using var context = new OasDbContext(CreateOptions());
        var expected = new (Type Type, string Table)[]
        {
            (typeof(PayrollPolicy), "PayrollPolicies"),
            (typeof(PayrollPeriod), "PayrollPeriods"),
            (typeof(PayrollRun), "PayrollRuns"),
            (typeof(EmployeePayroll), "EmployeePayrolls"),
            (typeof(EmployeePayrollSalarySegment), "EmployeePayrollSalarySegments"),
            (typeof(EmployeePayrollLine), "EmployeePayrollLines"),
            (typeof(EndOfServiceSettlement), "EndOfServiceSettlements"),
            (typeof(EndOfServiceSettlementLine), "EndOfServiceSettlementLines")
        };

        Assert.Multiple(() =>
        {
            foreach (var (type, table) in expected)
            {
                var entity = context.Model.FindEntityType(type);
                Assert.That(entity, Is.Not.Null, $"{type.Name} must be present in the EF model.");
                Assert.That(entity!.GetSchema(), Is.EqualTo("hr"));
                Assert.That(entity.GetTableName(), Is.EqualTo(table));
                var rowVersion = entity.FindProperty("RowVersion");
                Assert.That(rowVersion, Is.Not.Null, $"{type.Name}.RowVersion is required.");
                Assert.That(rowVersion!.IsConcurrencyToken, Is.True, $"{type.Name}.RowVersion must be a concurrency token.");
            }
        });
    }

    [Test]
    public void Phase3Model_ContainsCriticalUniqueIndexes()
    {
        using var context = new OasDbContext(CreateOptions());

        Assert.Multiple(() =>
        {
            AssertUniqueIndex(context, typeof(PayrollPolicy), "UX_PayrollPolicies_Code");
            AssertUniqueIndex(context, typeof(PayrollPeriod), "UX_PayrollPeriods_Year_Month");
            AssertUniqueIndex(context, typeof(PayrollPeriod), "UX_PayrollPeriods_Code");
            AssertUniqueIndex(context, typeof(PayrollRun), "UX_PayrollRuns_Code");
            AssertUniqueIndex(context, typeof(EmployeePayroll), "UX_EmployeePayroll_Run_Employee");
            AssertUniqueIndex(context, typeof(EmployeePayroll), "UX_EmployeePayroll_Period_Employee");
            AssertUniqueIndex(context, typeof(EmployeePayrollLine), "UX_EmployeePayrollLines_Payroll_Sequence");
            AssertUniqueIndex(context, typeof(EndOfServiceSettlement), "UX_EndOfService_SettlementCode");
        });
    }

    [Test]
    public void Phase3Sequences_AreRegisteredInModel()
    {
        using var context = new OasDbContext(CreateOptions());
        Assert.Multiple(() =>
        {
            Assert.That(context.Model.FindSequence("PayrollPolicyCodeSequence", "hr"), Is.Not.Null);
            Assert.That(context.Model.FindSequence("PayrollRunCodeSequence", "hr"), Is.Not.Null);
            Assert.That(context.Model.FindSequence("EndOfServiceCodeSequence", "hr"), Is.Not.Null);
        });
    }

    private static void AssertUniqueIndex(OasDbContext context, Type entityType, string databaseName)
    {
        var entity = context.Model.FindEntityType(entityType);
        Assert.That(entity, Is.Not.Null);
        var index = entity!.GetIndexes().SingleOrDefault(x => x.GetDatabaseName() == databaseName);
        Assert.That(index, Is.Not.Null, $"Index {databaseName} is missing from {entityType.Name}.");
        Assert.That(index!.IsUnique, Is.True, $"Index {databaseName} must be unique.");
    }
}
