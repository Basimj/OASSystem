using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Domain.Features.Employees.Adjustments;
using OAS.Domain.Features.Employees.Leave;
using OAS.Domain.Features.Employees.Loans;
using OAS.Domain.Features.Employees.Overtime;
using OAS.Domain.Features.Employees.Settings;
using OAS.Domain.Features.Employees.Time;
using OAS.Infrastructure.Features.Employees.Services;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Features.Employees.Integration;

[TestFixture]
public sealed class HrOperationsEfConfigurationTests
{
    private static DbContextOptions<OasDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=dummy_hr_operations_tests;Trusted_Connection=True;")
            .Options;

    [Test]
    public void Phase2Entities_AreMappedToHrSchema_WithConcurrencyTokens()
    {
        using var context = new OasDbContext(CreateOptions());
        var expected = new (Type Type, string Table)[]
        {
            (typeof(HrSettings), "HrSettings"),
            (typeof(WorkShift), "WorkShifts"),
            (typeof(EmployeeShiftAssignment), "EmployeeShiftAssignments"),
            (typeof(Holiday), "Holidays"),
            (typeof(AttendanceRecord), "AttendanceRecords"),
            (typeof(LeaveType), "LeaveTypes"),
            (typeof(EmployeeLeaveBalance), "EmployeeLeaveBalances"),
            (typeof(LeaveRequest), "LeaveRequests"),
            (typeof(OvertimeRecord), "OvertimeRecords"),
            (typeof(EmployeeLoan), "EmployeeLoans"),
            (typeof(EmployeeLoanInstallment), "EmployeeLoanInstallments"),
            (typeof(EmployeeAdjustment), "EmployeeAdjustments")
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
    public void Phase2Model_ContainsCriticalUniqueIndexes()
    {
        using var context = new OasDbContext(CreateOptions());

        Assert.Multiple(() =>
        {
            AssertUniqueIndex(context, typeof(AttendanceRecord), "UX_Attendance_Employee_Date");
            AssertUniqueIndex(context, typeof(EmployeeLeaveBalance), "UX_LeaveBalances_Employee_Type_Year");
            AssertUniqueIndex(context, typeof(LeaveRequest), "UX_LeaveRequests_Code");
            AssertUniqueIndex(context, typeof(OvertimeRecord), "UX_Overtime_AttendanceRecord");
            AssertUniqueIndex(context, typeof(EmployeeLoan), "UX_EmployeeLoans_PaymentVoucher");
            AssertUniqueIndex(context, typeof(EmployeeLoanInstallment), "UX_LoanInstallments_Loan_Sequence");
            AssertUniqueIndex(context, typeof(EmployeeAdjustment), "UX_EmployeeAdjustments_Code");
        });
    }

    [Test]
    public void HrTimeZoneService_ConvertsAsiaAdenLocalTimeToUtc()
    {
        var service = new HRTimeZoneService();

        var utc = service.ToUtc(
            new DateOnly(2026, 10, 2),
            new TimeOnly(8, 0),
            "Asia/Aden");

        Assert.That(utc, Is.EqualTo(new DateTimeOffset(2026, 10, 2, 5, 0, 0, TimeSpan.Zero)));
        Assert.That(service.ToLocalDate(utc, "Asia/Aden"), Is.EqualTo(new DateOnly(2026, 10, 2)));
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
