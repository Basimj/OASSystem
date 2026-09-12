using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Queries.GetEmployeeById;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.ValueObjects;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class GetEmployeeByIdQueryHandlerTests
{
    [Test]
    public async Task GetById_ExistingEmployee_ReturnsJobTitleContactAndEmployeeCode()
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var employee = Employee.Create(
            Guid.NewGuid(),
            "EMP-00007",
            "Ahmed",
            "Ali",
            ContactInfo.Create(
                "777123456",
                "a@example.com",
                Address.Create(
                    "Yemen",
                    "Sana'a",
                    "Sana'a",
                    null,
                    "Street")),
            title.Id,
            null,
            true,
            true);

        var handler =
            new GetEmployeeByIdQueryHandler(
                new FakeEmployeeRepository(employee),
                new FakeJobTitleRepository(title),
                new FakeUserRepository());

        var result =
            await handler.Handle(
                new GetEmployeeByIdQuery(employee.Id),
                CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.EmployeeCode,
                Is.EqualTo("EMP-00007"));

            Assert.That(
                result.JobTitleId,
                Is.EqualTo(title.Id));

            Assert.That(
                result.JobTitleName,
                Is.EqualTo("فني"));

            Assert.That(
                result.DisplayName,
                Is.EqualTo("Ahmed Ali"));

            Assert.That(
                result.Email,
                Is.EqualTo("a@example.com"));

            Assert.That(
                result.Country,
                Is.EqualTo("Yemen"));
        });
    }

    [Test]
    public void GetById_UnknownEmployee_ThrowsNotFound()
    {
        var handler =
            new GetEmployeeByIdQueryHandler(
                new FakeEmployeeRepository(),
                new FakeJobTitleRepository(),
                new FakeUserRepository());

        Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new GetEmployeeByIdQuery(Guid.NewGuid()),
                CancellationToken.None));
    }
}