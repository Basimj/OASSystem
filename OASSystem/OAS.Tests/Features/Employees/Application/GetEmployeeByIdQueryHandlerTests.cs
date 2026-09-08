using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Queries.GetEmployeeById;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class GetEmployeeByIdQueryHandlerTests
{
    [Test]
    public async Task GetById_ExistingEmployee_ReturnsDto()
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            "EMP-001",
            "Ahmed",
            "Ali",
            "777123456",
            "Sales",
            null,
            null,
            true,
            false,
            true,
            true,
            null);

        var handler =
            new GetEmployeeByIdQueryHandler(
                new FakeEmployeeRepository(employee));

        var result = await handler.Handle(
            new GetEmployeeByIdQuery(employee.Id),
            CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(employee.Id));
        Assert.That(result.EmployeeCode,
            Is.EqualTo("EMP-001"));
        Assert.That(result.DisplayName,
            Is.EqualTo("Ahmed Ali"));
    }

    [Test]
    public void GetById_UnknownEmployee_ThrowsNotFound()
    {
        var handler =
            new GetEmployeeByIdQueryHandler(
                new FakeEmployeeRepository());

        var id = Guid.NewGuid();

        Assert.ThrowsAsync<NotFoundException>(
            async () => await handler.Handle(
                new GetEmployeeByIdQuery(id),
                CancellationToken.None));
    }
}