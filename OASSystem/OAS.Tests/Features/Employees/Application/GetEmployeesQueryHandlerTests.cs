using NUnit.Framework;
using OAS.Application.Features.Employees.Queries.GetEmployees;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.ValueObjects;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class GetEmployeesQueryHandlerTests
{
    [Test]
    public async Task GetEmployees_SearchByNumericCode_ReturnsMatch()
    {
        var title = JobTitle.Create(Guid.NewGuid(), "فني", true);
        var first = CreateEmployee(1, "Ahmed", "Ali", "777111111", title.Id);
        var second = CreateEmployee(2, "Mohammed", "Hassan", "777222222", title.Id);
        var handler = new GetEmployeesQueryHandler(
            new FakeEmployeeRepository(first, second), new FakeJobTitleRepository(title));

        var result = await handler.Handle(new GetEmployeesQuery(new PageRequest
        {
            PageNumber = 1, PageSize = 20, Search = "2"
        }), CancellationToken.None);

        Assert.That(result.Items.Select(x => x.EmployeeNumber), Is.EquivalentTo(new[] { 2 }));
    }

    [Test]
    public async Task GetEmployees_SearchByContactField_ReturnsMatch()
    {
        var title = JobTitle.Create(Guid.NewGuid(), "فني", true);
        var first = CreateEmployee(1, "A", "One", null, title.Id, "a@example.com", "Sana'a");
        var second = CreateEmployee(2, "B", "Two", null, title.Id, "b@example.com", "Aden");
        var handler = new GetEmployeesQueryHandler(
            new FakeEmployeeRepository(first, second), new FakeJobTitleRepository(title));

        var result = await handler.Handle(new GetEmployeesQuery(new PageRequest
        {
            PageNumber = 1, PageSize = 20, Search = "Aden"
        }), CancellationToken.None);

        Assert.That(result.Items.Select(x => x.EmployeeNumber), Is.EquivalentTo(new[] { 2 }));
    }

    [Test]
    public async Task GetEmployees_SearchByJobTitle_ReturnsMatches()
    {
        var tech = JobTitle.Create(Guid.NewGuid(), "فني", true);
        var stock = JobTitle.Create(Guid.NewGuid(), "مخازن", true);
        var handler = new GetEmployeesQueryHandler(
            new FakeEmployeeRepository(
                CreateEmployee(1, "A", "One", null, tech.Id),
                CreateEmployee(2, "B", "Two", null, stock.Id)),
            new FakeJobTitleRepository(tech, stock));

        var result = await handler.Handle(new GetEmployeesQuery(new PageRequest
        {
            PageNumber = 1, PageSize = 20, Search = "مخازن"
        }), CancellationToken.None);

        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].JobTitleName, Is.EqualTo("مخازن"));
    }

    [Test]
    public async Task GetEmployees_Pagination_ReturnsCorrectTotal()
    {
        var title = JobTitle.Create(Guid.NewGuid(), "فني", true);
        var employees = Enumerable.Range(1, 25)
            .Select(i => CreateEmployee(i, $"First{i}", $"Last{i}", null, title.Id)).ToArray();
        var handler = new GetEmployeesQueryHandler(new FakeEmployeeRepository(employees), new FakeJobTitleRepository(title));

        var result = await handler.Handle(new GetEmployeesQuery(new PageRequest { PageNumber = 2, PageSize = 10 }), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(25));
            Assert.That(result.Items, Has.Count.EqualTo(10));
            Assert.That(result.TotalPages, Is.EqualTo(3));
        });
    }

    private static Employee CreateEmployee(
        int number,
        string first,
        string last,
        string? phone,
        Guid titleId,
        string? email = null,
        string? city = null) => Employee.Create(
        Guid.NewGuid(), number, first, last,
        ContactInfo.Create(phone, email, Address.Create(null, null, city, null, null)),
        titleId, null, true, true);
}
