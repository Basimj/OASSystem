using NUnit.Framework;
using OAS.Application.Features.Employees.Queries.GetEmployees;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class GetEmployeesQueryHandlerTests
{
    [Test]
    public async Task GetEmployees_SearchByCode_ReturnsMatchingEmployee()
    {
        var employees = new[]
        {
            CreateEmployee("EMP-001", "Ahmed", "Ali", "777111111"),
            CreateEmployee("EMP-002", "Mohammed", "Hassan", "777222222")
        };

        var handler =
            new GetEmployeesQueryHandler(
                new FakeEmployeeRepository(employees));

        var request = new PageRequest
        {
            PageNumber = 1,
            PageSize = 20,
            Search = "EMP-002"
        };

        var result = await handler.Handle(
            new GetEmployeesQuery(request),
            CancellationToken.None);

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(
            result.Items[0].EmployeeCode,
            Is.EqualTo("EMP-002"));
    }

    [Test]
    public async Task GetEmployees_SearchByName_ReturnsMatchingEmployee()
    {
        var employees = new[]
        {
            CreateEmployee("EMP-001", "Ahmed", "Ali", null),
            CreateEmployee("EMP-002", "Mohammed", "Hassan", null)
        };

        var handler =
            new GetEmployeesQueryHandler(
                new FakeEmployeeRepository(employees));

        var request = new PageRequest
        {
            PageNumber = 1,
            PageSize = 20,
            Search = "Mohammed"
        };

        var result = await handler.Handle(
            new GetEmployeesQuery(request),
            CancellationToken.None);

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(
            result.Items[0].FirstName,
            Is.EqualTo("Mohammed"));
    }

    [Test]
    public async Task GetEmployees_SearchByPhone_ReturnsMatchingEmployee()
    {
        var employees = new[]
        {
            CreateEmployee("EMP-001", "Ahmed", "Ali", "777111111"),
            CreateEmployee("EMP-002", "Mohammed", "Hassan", "777222222")
        };

        var handler =
            new GetEmployeesQueryHandler(
                new FakeEmployeeRepository(employees));

        var request = new PageRequest
        {
            PageNumber = 1,
            PageSize = 20,
            Search = "777222222"
        };

        var result = await handler.Handle(
            new GetEmployeesQuery(request),
            CancellationToken.None);

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(
            result.Items[0].Phone,
            Is.EqualTo("777222222"));
    }

    [Test]
    public async Task GetEmployees_Pagination_ReturnsCorrectTotal()
    {
        var employees = Enumerable.Range(1, 25)
            .Select(i =>
                CreateEmployee(
                    $"EMP-{i:000}",
                    $"First{i}",
                    $"Last{i}",
                    null))
            .ToArray();

        var handler =
            new GetEmployeesQueryHandler(
                new FakeEmployeeRepository(employees));

        var request = new PageRequest
        {
            PageNumber = 2,
            PageSize = 10
        };

        var result = await handler.Handle(
            new GetEmployeesQuery(request),
            CancellationToken.None);

        Assert.That(result.TotalCount, Is.EqualTo(25));
        Assert.That(result.PageNumber, Is.EqualTo(2));
        Assert.That(result.PageSize, Is.EqualTo(10));
        Assert.That(result.Items, Has.Count.EqualTo(10));
        Assert.That(result.TotalPages, Is.EqualTo(3));
    }

    private static Employee CreateEmployee(
        string code,
        string firstName,
        string lastName,
        string? phone)
    {
        return Employee.Create(
            Guid.NewGuid(),
            code,
            firstName,
            lastName,
            phone,
            "Sales",
            null,
            null,
            true,
            false,
            true,
            true,
            null);
    }
}