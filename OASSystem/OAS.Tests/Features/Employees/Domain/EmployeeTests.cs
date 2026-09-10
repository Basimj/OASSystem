using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.ValueObjects;

namespace OAS.Tests.Features.Employees.Domain;

[TestFixture]
public sealed class EmployeeTests
{
    private static readonly Guid JobTitleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public void Create_ValidEmployee_SetsBusinessAndContactFields()
    {
        var id = Guid.NewGuid();
        var contact = ContactInfo.Create(
            " 777123456 ",
            " employee@example.com ",
            Address.Create(" Yemen ", " Sana'a ", " Sana'a ", " 10001 ", " Main Street "));

        var employee = Employee.Create(
            id,
            17,
            " Ahmed ",
            " Ali ",
            contact,
            JobTitleId,
            new DateOnly(2026, 1, 10),
            isCommissionEligible: true,
            isActive: true);

        Assert.Multiple(() =>
        {
            Assert.That(employee.Id, Is.EqualTo(id));
            Assert.That(employee.EmployeeNumber, Is.EqualTo(17));
            Assert.That(employee.EmployeeCode, Is.EqualTo("17"));
            Assert.That(employee.FirstName, Is.EqualTo("Ahmed"));
            Assert.That(employee.LastName, Is.EqualTo("Ali"));
            Assert.That(employee.ContactInfo.Phone, Is.EqualTo("777123456"));
            Assert.That(employee.ContactInfo.Email, Is.EqualTo("employee@example.com"));
            Assert.That(employee.ContactInfo.Address.Country, Is.EqualTo("Yemen"));
            Assert.That(employee.ContactInfo.Address.City, Is.EqualTo("Sana'a"));
            Assert.That(employee.JobTitleId, Is.EqualTo(JobTitleId));
            Assert.That(employee.IsCommissionEligible, Is.True);
            Assert.That(employee.IsActive, Is.True);
        });
    }

    [TestCase("", "Ali")]
    [TestCase("Ahmed", "")]
    public void Create_MissingRequiredName_Throws(string firstName, string lastName)
    {
        Assert.Throws<DomainException>(() => Employee.Create(
            Guid.NewGuid(), 1, firstName, lastName, ContactInfo.Empty, JobTitleId, null, false, true));
    }

    [Test]
    public void Create_InvalidEmployeeNumber_Throws()
    {
        Assert.Throws<DomainException>(() => Employee.Create(
            Guid.NewGuid(), 0, "Ahmed", "Ali", ContactInfo.Empty, JobTitleId, null, false, true));
    }

    [Test]
    public void Create_MissingJobTitle_Throws()
    {
        Assert.Throws<DomainException>(() => Employee.Create(
            Guid.NewGuid(), 1, "Ahmed", "Ali", ContactInfo.Empty, Guid.Empty, null, false, true));
    }

    [Test]
    public void UpdateDetails_LinkedEmployee_RemainsEditable()
    {
        var employee = CreateEmployee();
        var userId = Guid.NewGuid();
        var newTitleId = Guid.NewGuid();
        employee.LinkUserAccount(userId);

        employee.UpdateDetails(
            "Mohammed",
            "Hassan",
            ContactInfo.Create("771111111", "updated@example.com", Address.Create("Yemen", "Aden", "Aden", null, "Crater")),
            newTitleId,
            new DateOnly(2026, 2, 1),
            isCommissionEligible: false,
            isActive: false);
        employee.SetPhoto("Resources/Employees/Photos/test.jpg");

        Assert.Multiple(() =>
        {
            Assert.That(employee.FirstName, Is.EqualTo("Mohammed"));
            Assert.That(employee.ContactInfo.Phone, Is.EqualTo("771111111"));
            Assert.That(employee.JobTitleId, Is.EqualTo(newTitleId));
            Assert.That(employee.IsActive, Is.False);
            Assert.That(employee.Photo, Is.EqualTo("Resources/Employees/Photos/test.jpg"));
            Assert.That(employee.UserAccountId, Is.EqualTo(userId));
        });
    }

    [Test]
    public void SetUserAccountLink_AfterInitialLink_CannotChangeOrRemoveLink()
    {
        var employee = CreateEmployee();
        var userId = Guid.NewGuid();
        employee.SetUserAccountLink(userId);

        Assert.That(employee.UserAccountId, Is.EqualTo(userId));
        Assert.DoesNotThrow(() => employee.SetUserAccountLink(userId));
        Assert.Throws<DomainException>(() => employee.SetUserAccountLink(Guid.NewGuid()));
        Assert.Throws<DomainException>(() => employee.SetUserAccountLink(null));
    }

    [Test]
    public void EmployeeCodeFormatter_SupportsOptionalFuturePrefix()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EmployeeCodeFormatter.Format(42), Is.EqualTo("42"));
            Assert.That(EmployeeCodeFormatter.Format(42, "EMP-", 5), Is.EqualTo("EMP-00042"));
        });
    }

    private static Employee CreateEmployee() => Employee.Create(
        Guid.NewGuid(),
        1,
        "Ahmed",
        "Ali",
        ContactInfo.Create("777123456", null, Address.Empty),
        JobTitleId,
        new DateOnly(2026, 1, 10),
        isCommissionEligible: true,
        isActive: true);
}
