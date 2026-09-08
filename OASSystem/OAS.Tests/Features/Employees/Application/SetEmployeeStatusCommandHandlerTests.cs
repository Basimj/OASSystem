using NUnit.Framework;
using OAS.Application.Features.Employees.Commands.SetEmployeeStatus;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class SetEmployeeStatusCommandHandlerTests
{
    [Test]
    public async Task SetStatus_False_DeactivatesEmployee()
    {
        var employee = CreateEmployee(true);

        var handler =
            new SetEmployeeStatusCommandHandler(
                new FakeEmployeeRepository(employee));

        var request = new SetEmployeeStatusRequest(
            false,
            Convert.ToBase64String(employee.RowVersion));

        await handler.Handle(
            new SetEmployeeStatusCommand(
                employee.Id,
                request),
            CancellationToken.None);

        Assert.That(employee.IsActive, Is.False);
    }

    [Test]
    public async Task SetStatus_True_ActivatesEmployee()
    {
        var employee = CreateEmployee(false);

        var handler =
            new SetEmployeeStatusCommandHandler(
                new FakeEmployeeRepository(employee));

        var request = new SetEmployeeStatusRequest(
            true,
            Convert.ToBase64String(employee.RowVersion));

        await handler.Handle(
            new SetEmployeeStatusCommand(
                employee.Id,
                request),
            CancellationToken.None);

        Assert.That(employee.IsActive, Is.True);
    }

    private static Employee CreateEmployee(bool active)
    {
        return Employee.Create(
            Guid.NewGuid(),
            "EMP-001",
            "Ahmed",
            "Ali",
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            active,
            null);
    }
}