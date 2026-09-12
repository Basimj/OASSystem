using DocumentFormat.OpenXml.Presentation;
using Microsoft.VisualBasic;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Commands.UpdateEmployee;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.ValueObjects;
using OAS.Domain.Identity.Entities;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class UpdateEmployeeCommandHandlerTests
{
    [Test]
    public async Task UpdateEmployee_ValidRequest_UpdatesContactAndBusinessFields()
    {
        var oldTitle = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var newTitle = JobTitle.Create(
            Guid.NewGuid(),
            "مخازن",
            true);

        var employee = CreateEmployee(oldTitle.Id);

        var handler = new UpdateEmployeeCommandHandler(
            new FakeEmployeeRepository(employee),
            new FakeJobTitleRepository(oldTitle, newTitle),
            new FakeUserRepository());

        var request = Request(
            employee,
            newTitle.Id,
            firstName: "Mohammed",
            phone: "771111111",
            email: "mohammed@example.com",
            city: "Aden",
            isCommissionEligible: true,
            isActive: false);

        var id = await handler.Handle(
            new UpdateEmployeeCommand(employee.Id, request),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.EqualTo(employee.Id));
            Assert.That(employee.FirstName, Is.EqualTo("Mohammed"));
            Assert.That(employee.ContactInfo.Phone, Is.EqualTo("771111111"));
            Assert.That(employee.ContactInfo.Email, Is.EqualTo("mohammed@example.com"));
            Assert.That(employee.ContactInfo.Address.City, Is.EqualTo("Aden"));
            Assert.That(employee.JobTitleId, Is.EqualTo(newTitle.Id));
            Assert.That(employee.IsCommissionEligible, Is.True);
            Assert.That(employee.IsActive, Is.False);
        });
    }

    [Test]
    public async Task UpdateEmployee_LinkedEmployee_CanEditOtherFieldsWhenLinkIsUnchanged()
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var userId = Guid.NewGuid();

        var employee = CreateEmployee(
            title.Id,
            userId);

        var handler = new UpdateEmployeeCommandHandler(
            new FakeEmployeeRepository(employee),
            new FakeJobTitleRepository(title),
            new FakeUserRepository());

        await handler.Handle(
            new UpdateEmployeeCommand(
                employee.Id,
                Request(
                    employee,
                    title.Id,
                    firstName: "Updated",
                    userAccountId: userId)),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(employee.FirstName, Is.EqualTo("Updated"));
            Assert.That(employee.UserAccountId, Is.EqualTo(userId));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void UpdateEmployee_LinkedEmployee_CannotChangeOrRemoveUserLink(
        bool remove)
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var userId = Guid.NewGuid();

        var employee = CreateEmployee(
            title.Id,
            userId);

        var handler = new UpdateEmployeeCommandHandler(
            new FakeEmployeeRepository(employee),
            new FakeJobTitleRepository(title),
            new FakeUserRepository());

        // مهم:
        // يجب أن يكون النوع Guid? لأن القيمة قد تكون null.
        Guid? requestedUser = remove
            ? null
            : Guid.NewGuid();

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new UpdateEmployeeCommand(
                    employee.Id,
                    Request(
                        employee,
                        title.Id,
                        userAccountId: requestedUser)),
                CancellationToken.None));

        Assert.That(
            ex!.Code,
            Is.EqualTo("employee_user_account_immutable"));
    }

    [Test]
    public async Task UpdateEmployee_UnlinkedEmployee_CanLinkAvailableUser()
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var userId = Guid.NewGuid();

        var employee = CreateEmployee(title.Id);

        var user = UserAccount.Create(
            userId,
            "linked-user",
            "Linked",
            "User",
            null);

        var handler = new UpdateEmployeeCommandHandler(
            new FakeEmployeeRepository(employee),
            new FakeJobTitleRepository(title),
            new FakeUserRepository(user));

        await handler.Handle(
            new UpdateEmployeeCommand(
                employee.Id,
                Request(
                    employee,
                    title.Id,
                    userAccountId: userId)),
            CancellationToken.None);

        Assert.That(
            employee.UserAccountId,
            Is.EqualTo(userId));
    }

    [Test]
    public void UpdateEmployee_StaleRowVersion_ThrowsConcurrency()
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var employee = CreateEmployee(title.Id);

        var handler = new UpdateEmployeeCommandHandler(
            new FakeEmployeeRepository(employee),
            new FakeJobTitleRepository(title),
            new FakeUserRepository());

        var request = Request(employee, title.Id) with
        {
            RowVersion = Convert.ToBase64String([1, 2, 3])
        };

        Assert.ThrowsAsync<ConcurrencyException>(() =>
            handler.Handle(
                new UpdateEmployeeCommand(
                    employee.Id,
                    request),
                CancellationToken.None));
    }

    private static Employee CreateEmployee(
        Guid titleId,
        Guid? userAccountId = null)
    {
        return Employee.Create(
            Guid.NewGuid(),
            "EMP-00501",
            "Ahmed",
            "Ali",
            ContactInfo.Create(
                "777000000",
                null,
                Address.Empty),
            titleId,
            null,
            false,
            true,
            userAccountId);
    }

    private static UpdateEmployeeRequest Request(
        Employee employee,
        Guid titleId,
        string firstName = "Ahmed",
        string? phone = "777000000",
        string? email = null,
        string? city = null,
        bool isCommissionEligible = false,
        bool isActive = true,
        Guid? userAccountId = null)
    {
        return new UpdateEmployeeRequest(
            firstName,
            "Ali",
            phone,
            email,
            "Yemen",
            "Sana'a",
            city,
            null,
            null,
            titleId,
            null,
            isCommissionEligible,
            isActive,
            userAccountId,
            Convert.ToBase64String(employee.RowVersion));
    }
}
