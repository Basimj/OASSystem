using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Commands.CreateEmployee;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.ValueObjects;
using OAS.Domain.Identity.Entities;

namespace OAS.Tests.Features.Employees.Application;

[TestFixture]
public sealed class CreateEmployeeCommandHandlerTests
{
    [Test]
    public async Task CreateEmployee_ValidRequest_UsesReservedNumberAndAddsContactData()
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var employees = new FakeEmployeeRepository();

        var handler = CreateHandler(
            employees,
            title,
            sequenceStart: 700);

        var id = await handler.Handle(
            new CreateEmployeeCommand(
                new CreateEmployeeRequest(
                    "Ahmed",
                    "Ali",
                    "777123456",
                    "ahmed@example.com",
                    "Yemen",
                    "Sana'a",
                    "Sana'a",
                    "10001",
                    "Main Street",
                    title.Id,
                    new DateOnly(2026, 1, 1),
                    true,
                    true)),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(
                id,
                Is.Not.EqualTo(Guid.Empty));

            Assert.That(
                employees.AddedEmployee,
                Is.Not.Null);

            Assert.That(
                employees.AddedEmployee!.EmployeeCode,
                Is.EqualTo("EMP-00700"));

            Assert.That(
                employees.AddedEmployee.JobTitleId,
                Is.EqualTo(title.Id));

            Assert.That(
                employees.AddedEmployee.ContactInfo.Email,
                Is.EqualTo("ahmed@example.com"));

            Assert.That(
                employees.AddedEmployee.ContactInfo.Address.Country,
                Is.EqualTo("Yemen"));
        });
    }

    [Test]
    public async Task CreateEmployee_PreReservedNumber_DoesNotConsumeAnotherNumber()
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var employees = new FakeEmployeeRepository();

        var sequence =
            new FakeSequenceNumberGenerator(900);

        var handler =
            new CreateEmployeeCommandHandler(
                employees,
                new FakeJobTitleRepository(title),
                new FakeUserRepository(),
                sequence);

        await handler.Handle(
            new CreateEmployeeCommand(
                new CreateEmployeeRequest(
                    "A",
                    "B",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    title.Id,
                    null,
                    false,
                    true,
                    EmployeeCode: "EMP-00501")),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(
                employees.AddedEmployee,
                Is.Not.Null);

            Assert.That(
                employees.AddedEmployee!.EmployeeCode,
                Is.EqualTo("EMP-00501"));
        });

        Assert.That(
            await sequence.NextAsync(
                "EmployeeNumberSequence"),
            Is.EqualTo(900));
    }

    [Test]
    public void CreateEmployee_InactiveJobTitle_ThrowsConflict()
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "قديم",
            false);

        var handler =
            CreateHandler(
                new FakeEmployeeRepository(),
                title);

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateEmployeeCommand(
                    new CreateEmployeeRequest(
                        "A",
                        "B",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        title.Id,
                        null,
                        false,
                        true)),
                CancellationToken.None));

        Assert.That(
            ex!.Code,
            Is.EqualTo("job_title_inactive"));
    }

    [Test]
    public void CreateEmployee_UnknownUser_ThrowsConflict()
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var handler =
            CreateHandler(
                new FakeEmployeeRepository(),
                title);

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateEmployeeCommand(
                    new CreateEmployeeRequest(
                        "A",
                        "B",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        title.Id,
                        null,
                        false,
                        true,
                        Guid.NewGuid())),
                CancellationToken.None));

        Assert.That(
            ex!.Code,
            Is.EqualTo(
                "employee_user_account_not_found"));
    }

    [Test]
    public void CreateEmployee_UserAlreadyLinked_ThrowsConflict()
    {
        var title = JobTitle.Create(
            Guid.NewGuid(),
            "فني",
            true);

        var userId = Guid.NewGuid();

        var existing = Employee.Create(
            Guid.NewGuid(),
            "EMP-00501",
            "X",
            "Y",
            ContactInfo.Empty,
            title.Id,
            null,
            false,
            true,
            userId);

        var user = UserAccount.Create(
            userId,
            "user",
            "User",
            "One",
            null);

        var handler =
            new CreateEmployeeCommandHandler(
                new FakeEmployeeRepository(existing),
                new FakeJobTitleRepository(title),
                new FakeUserRepository(user),
                new FakeSequenceNumberGenerator());

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateEmployeeCommand(
                    new CreateEmployeeRequest(
                        "A",
                        "B",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        title.Id,
                        null,
                        false,
                        true,
                        userId)),
                CancellationToken.None));

        Assert.That(
            ex!.Code,
            Is.EqualTo(
                "employee_user_account_already_linked"));
    }

    private static CreateEmployeeCommandHandler CreateHandler(
        FakeEmployeeRepository employees,
        JobTitle title,
        long sequenceStart = 100) =>
        new(
            employees,
            new FakeJobTitleRepository(title),
            new FakeUserRepository(),
            new FakeSequenceNumberGenerator(sequenceStart));
}