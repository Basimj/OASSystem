using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Commands.UpdateEmployee;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class UpdateEmployeeCommandHandlerTests
{
    [Test]
    public async Task UpdateEmployee_UpdatesEmployee()
    {
        var employee = CreateEmployee();

        var repository =
            new FakeEmployeeRepository(employee);

        var handler =
            new UpdateEmployeeCommandHandler(
                repository,
                new FakeUserRepository());

        var request = new UpdateEmployeeRequest(
            "EMP-002",
            "Mohammed",
            "Hassan",
            "771111111",
            "Technician",
            new DateOnly(2026, 2, 1),
            "Updated",
            false,
            true,
            true,
            null,
            Convert.ToBase64String(employee.RowVersion));

        var id = await handler.Handle(
            new UpdateEmployeeCommand(
                employee.Id,
                request),
            CancellationToken.None);

        Assert.That(id, Is.EqualTo(employee.Id));
        Assert.That(employee.EmployeeCode, Is.EqualTo("EMP-002"));
        Assert.That(employee.FirstName, Is.EqualTo("Mohammed"));
        Assert.That(employee.IsTechnician, Is.True);
    }

    [Test]
    public async Task UpdateEmployee_StaleRowVersion_ThrowsConcurrency()
    {
        var employee = CreateEmployee();

        var handler =
            new UpdateEmployeeCommandHandler(
                new FakeEmployeeRepository(employee),
                new FakeUserRepository());

        var staleVersion =
            Convert.ToBase64String([1, 2, 3]);

        var request = new UpdateEmployeeRequest(
            "EMP-002",
            "Mohammed",
            "Hassan",
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            null,
            staleVersion);

        var exception =
            Assert.ThrowsAsync<ConcurrencyException>(
                async () => await handler.Handle(
                    new UpdateEmployeeCommand(
                        employee.Id,
                        request),
                    CancellationToken.None));

        Assert.That(exception, Is.Not.Null);
    }

    private static Employee CreateEmployee()
    {
        return Employee.Create(
            Guid.NewGuid(),
            "EMP-001",
            "Ahmed",
            "Ali",
            "777123456",
            "Sales",
            new DateOnly(2026, 1, 1),
            "Test",
            true,
            false,
            true,
            true,
            null);
    }
}