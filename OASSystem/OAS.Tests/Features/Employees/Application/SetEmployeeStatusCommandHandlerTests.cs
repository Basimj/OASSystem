using NUnit.Framework;
using OAS.Application.Features.Employees.Commands.SetEmployeeStatus;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.ValueObjects;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class SetEmployeeStatusCommandHandlerTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task SetStatus_WorksRegardlessOfUserAccountLink(bool linked)
    {
        var employee = Employee.Create(
            Guid.NewGuid(), 1, "A", "B", ContactInfo.Empty, Guid.NewGuid(), null, false, true,
            linked ? Guid.NewGuid() : null);
        var handler = new SetEmployeeStatusCommandHandler(new FakeEmployeeRepository(employee));

        await handler.Handle(new SetEmployeeStatusCommand(employee.Id,
            new SetEmployeeStatusRequest(false, Convert.ToBase64String(employee.RowVersion))), CancellationToken.None);

        Assert.That(employee.IsActive, Is.False);
    }
}
