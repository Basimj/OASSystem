using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.ValueObjects;

namespace OAS.Tests.Features.Employees.Domain;

[TestFixture]
public sealed class HRFoundationDomainTests
{
    [Test]
    public void Employee_OperationalCapabilities_AreIndependentFromJobTitle()
    {
        var employee = Employee.Create(Guid.NewGuid(), "EMP-01000", "Ahmed", "Ali", ContactInfo.Empty, Guid.NewGuid(), null, null, new DateOnly(2026, 1, 1), true, false, true, true);
        Assert.Multiple(() =>
        {
            Assert.That(employee.IsSalesperson, Is.True);
            Assert.That(employee.IsTechnician, Is.False);
            Assert.That(employee.IsCommissionEligible, Is.True);
        });
        employee.SetOperationalCapabilities(false, true, false);
        Assert.Multiple(() =>
        {
            Assert.That(employee.IsSalesperson, Is.False);
            Assert.That(employee.IsTechnician, Is.True);
            Assert.That(employee.IsCommissionEligible, Is.False);
        });
    }

    [Test]
    public void Employee_CannotBeOwnManager()
    {
        var id = Guid.NewGuid();
        Assert.Throws<DomainException>(() => Employee.Create(id, "EMP-01001", "A", "B", ContactInfo.Empty, Guid.NewGuid(), null, id, null, false, false, false, true));
    }

    [Test]
    public void Department_CannotBeOwnParent()
    {
        var id = Guid.NewGuid();
        Assert.Throws<DomainException>(() => Department.Create(id, "DEP-000001", "الإدارة", parentDepartmentId: id));
    }

    [Test]
    public void FixedTermContract_RequiresEndDate_AndBecomesImmutableAfterActivation()
    {
        var employeeId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        Assert.Throws<DomainException>(() => EmployeeContract.Create(Guid.NewGuid(), "CTR-2026-000001", employeeId, EmploymentContractType.FixedTerm, new DateOnly(2026, 1, 1), null, null, 8, 6, currencyId, null));

        var contract = EmployeeContract.Create(Guid.NewGuid(), "CTR-2026-000002", employeeId, EmploymentContractType.FixedTerm, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null, 8, 6, currencyId, null);
        contract.Activate("admin", DateTimeOffset.UtcNow);
        Assert.That(contract.Status, Is.EqualTo(EmploymentContractStatus.Active));
        Assert.Throws<DomainException>(() => contract.UpdateDraft(EmploymentContractType.Permanent, contract.StartDate, null, null, 8, 6, currencyId, null));
    }

    [Test]
    public void SalaryComponent_BasicSalary_MustBeEarning()
    {
        Assert.Throws<DomainException>(() => SalaryComponent.Create(Guid.NewGuid(), "SAL-000001", "راتب أساسي", null, SalaryComponentType.Deduction, SalaryCalculationMethod.FixedAmount, true, true, false, true, "SalaryExpense", "SalariesPayable", 1, null));
    }

    [Test]
    public void SalaryStructure_RequiresExactlyOneBasicSalary_AndSnapshotsComponent()
    {
        var structure = EmployeeSalaryStructure.Create(Guid.NewGuid(), "SST-2026-000001", Guid.NewGuid(), null, Guid.NewGuid(), new DateOnly(2026, 1, 1), null, null);
        var basic = SalaryComponent.Create(Guid.NewGuid(), "SAL-000001", "الراتب الأساسي", null, SalaryComponentType.Earning, SalaryCalculationMethod.FixedAmount, true, true, false, true, "SalaryExpense", "SalariesPayable", 1, null);
        var allowance = SalaryComponent.Create(Guid.NewGuid(), "SAL-000002", "بدل سكن", null, SalaryComponentType.Earning, SalaryCalculationMethod.FixedAmount, false, true, false, true, "AllowanceExpense", "SalariesPayable", 2, null);

        Assert.Throws<DomainException>(() => structure.ReplaceLines([EmployeeSalaryStructureLine.Create(Guid.NewGuid(), structure.Id, allowance, 10_000, null)]));

        structure.ReplaceLines([
            EmployeeSalaryStructureLine.Create(Guid.NewGuid(), structure.Id, basic, 100_000, null),
            EmployeeSalaryStructureLine.Create(Guid.NewGuid(), structure.Id, allowance, 10_000, null)
        ]);

        var basicLine = structure.Lines.Single(x => x.IsBasicSalarySnapshot);
        Assert.Multiple(() =>
        {
            Assert.That(basicLine.ComponentCodeSnapshot, Is.EqualTo("SAL-000001"));
            Assert.That(basicLine.ComponentNameSnapshot, Is.EqualTo("الراتب الأساسي"));
            Assert.That(basicLine.DebitPostingRoleSnapshot, Is.EqualTo("SalaryExpense"));
        });
    }

    [Test]
    public void EmployeeDocument_RejectsExpiryBeforeIssueDate()
    {
        Assert.Throws<DomainException>(() => EmployeeDocument.Create(Guid.NewGuid(), "EDOC-2026-000001", Guid.NewGuid(), EmployeeDocumentType.Identity, "هوية", "Resources/Employees/Documents/a/file.pdf", "id.pdf", "application/pdf", 100, new string('a', 64), new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1), null));
    }
}
