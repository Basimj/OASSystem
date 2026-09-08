
using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Tests.Features.Employees.Domain;

[TestFixture]
public sealed class EmployeeTests
{
    [Test]
    public void Create_ValidEmployee_ReturnsEmployee()
    {
        var id = Guid.NewGuid();

        var employee = Employee.Create(
            id,
            "emp-001",
            "Ahmed",
            "Ali",
            "777123456",
            "Sales",
            new DateOnly(2026, 1, 10),
            "Test employee",
            isSalesperson: true,
            isTechnician: false,
            isCommissionEligible: true,
            isActive: true,
            userAccountId: null);

        Assert.That(employee.Id, Is.EqualTo(id));
        Assert.That(employee.EmployeeCode, Is.EqualTo("emp-001"));
        Assert.That(employee.FirstName, Is.EqualTo("Ahmed"));
        Assert.That(employee.LastName, Is.EqualTo("Ali"));
        Assert.That(employee.IsSalesperson, Is.True);
        Assert.That(employee.IsTechnician, Is.False);
        Assert.That(employee.IsCommissionEligible, Is.True);
        Assert.That(employee.IsActive, Is.True);
    }

    [Test]
    public void Create_EmptyId_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Employee.Create(
                Guid.Empty,
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
                null));

        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    public void Create_EmptyEmployeeCode_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Employee.Create(
                Guid.NewGuid(),
                "",
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
                null));

        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    public void Create_EmptyFirstName_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Employee.Create(
                Guid.NewGuid(),
                "EMP-001",
                "",
                "Ali",
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                true,
                null));

        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    public void Create_EmptyLastName_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Employee.Create(
                Guid.NewGuid(),
                "EMP-001",
                "Ahmed",
                "",
                null,
                null,
                null,
                null,
                false,
                false,
                false,
                true,
                null));

        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    public void Create_EmployeeCode_IsNormalized()
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            "  emp-001  ",
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

        Assert.That(employee.EmployeeCode, Is.EqualTo("emp-001"));
        Assert.That(employee.NormalizedEmployeeCode, Is.EqualTo("EMP-001"));
    }

    [Test]
    public void UpdateDetails_UpdatesEmployeeFields()
    {
        var employee = CreateEmployee();

        employee.UpdateDetails(
            "EMP-002",
            "Mohammed",
            "Hassan",
            "771111111",
            "Technician",
            new DateOnly(2026, 2, 1),
            "Updated notes");

        Assert.That(employee.EmployeeCode, Is.EqualTo("EMP-002"));
        Assert.That(employee.NormalizedEmployeeCode, Is.EqualTo("EMP-002"));
        Assert.That(employee.FirstName, Is.EqualTo("Mohammed"));
        Assert.That(employee.LastName, Is.EqualTo("Hassan"));
        Assert.That(employee.Phone, Is.EqualTo("771111111"));
        Assert.That(employee.JobTitle, Is.EqualTo("Technician"));
        Assert.That(employee.HireDate, Is.EqualTo(new DateOnly(2026, 2, 1)));
        Assert.That(employee.Notes, Is.EqualTo("Updated notes"));
    }

    [Test]
    public void SetCapabilities_UpdatesCapabilityFlags()
    {
        var employee = CreateEmployee();

        employee.SetCapabilities(
            isSalesperson: true,
            isTechnician: true,
            isCommissionEligible: true);

        Assert.That(employee.IsSalesperson, Is.True);
        Assert.That(employee.IsTechnician, Is.True);
        Assert.That(employee.IsCommissionEligible, Is.True);
    }

    [Test]
    public void SetUserAccount_WithGuid_SetsUserAccountId()
    {
        var employee = CreateEmployee();
        var userId = Guid.NewGuid();

        employee.SetUserAccount(userId);

        Assert.That(employee.UserAccountId, Is.EqualTo(userId));
    }

    [Test]
    public void SetUserAccount_WithNull_RemovesUserAccountLink()
    {
        var employee = Employee.Create(
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
            Guid.NewGuid());

        employee.SetUserAccount(null);

        Assert.That(employee.UserAccountId, Is.Null);
    }

    [Test]
    public void SetActive_False_DeactivatesEmployee()
    {
        var employee = CreateEmployee();

        employee.SetActive(false);

        Assert.That(employee.IsActive, Is.False);
    }

    [Test]
    public void SetActive_True_ReactivatesEmployee()
    {
        var employee = Employee.Create(
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
            false,
            null);

        employee.SetActive(true);

        Assert.That(employee.IsActive, Is.True);
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
            new DateOnly(2026, 1, 10),
            "Test",
            isSalesperson: true,
            isTechnician: false,
            isCommissionEligible: true,
            isActive: true,
            userAccountId: null);
    }
}

