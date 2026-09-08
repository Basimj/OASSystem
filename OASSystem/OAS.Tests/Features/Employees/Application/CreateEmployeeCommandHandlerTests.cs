using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Commands.CreateEmployee;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Identity.Entities;
using OAS.Contracts.Features.Employees;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class CreateEmployeeCommandHandlerTests
{
    [Test]
    public async Task CreateEmployee_AddsEmployee()
    {
        var employeeRepository = new FakeEmployeeRepository();
        var userRepository = new FakeUserRepository();

        var handler = new CreateEmployeeCommandHandler(
            employeeRepository,
            userRepository);

        var request = new CreateEmployeeRequest(
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

        var id = await handler.Handle(
            new CreateEmployeeCommand(request),
            CancellationToken.None);

        Assert.That(id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(employeeRepository.AddedEmployee, Is.Not.Null);
        Assert.That(
            employeeRepository.AddedEmployee!.EmployeeCode,
            Is.EqualTo("EMP-001"));
    }

    [Test]
    public async Task CreateEmployee_DuplicateCode_ThrowsConflict()
    {
        var existing = CreateEmployee("EMP-001");

        var handler = new CreateEmployeeCommandHandler(
            new FakeEmployeeRepository(existing),
            new FakeUserRepository());

        var request = new CreateEmployeeRequest(
            " emp-001 ",
            "Mohammed",
            "Hassan",
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            true,
            null);

        var exception =
            Assert.ThrowsAsync<ConflictException>(
                async () => await handler.Handle(
                    new CreateEmployeeCommand(request),
                    CancellationToken.None));

        Assert.That(exception!.Code,
            Is.EqualTo("employee_code_exists"));
    }

    [Test]
    public async Task CreateEmployee_UnknownUser_ThrowsConflict()
    {
        var userId = Guid.NewGuid();

        var handler = new CreateEmployeeCommandHandler(
            new FakeEmployeeRepository(),
            new FakeUserRepository());

        var request = new CreateEmployeeRequest(
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
            true,
            userId);

        var exception =
            Assert.ThrowsAsync<ConflictException>(
                async () => await handler.Handle(
                    new CreateEmployeeCommand(request),
                    CancellationToken.None));

        Assert.That(exception!.Code,
            Is.EqualTo("employee_user_account_not_found"));
    }

    [Test]
    public async Task CreateEmployee_DuplicateUserLink_ThrowsConflict()
    {
        var userId = Guid.NewGuid();

        var existingEmployee = Employee.Create(
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
            true,
            userId);

        var user = UserAccount.Create(
            userId,
            "ahmed",
            "Ahmed",
            "Ali",
            null);

        var handler = new CreateEmployeeCommandHandler(
            new FakeEmployeeRepository(existingEmployee),
            new FakeUserRepository(user));

        var request = new CreateEmployeeRequest(
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
            true,
            userId);

        var exception =
            Assert.ThrowsAsync<ConflictException>(
                async () => await handler.Handle(
                    new CreateEmployeeCommand(request),
                    CancellationToken.None));

        Assert.That(exception!.Code,
            Is.EqualTo("employee_user_account_already_linked"));
    }

    private static Employee CreateEmployee(string code)
    {
        return Employee.Create(
            Guid.NewGuid(),
            code,
            "Ahmed",
            "Ali",
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            true,
            null);
    }
}