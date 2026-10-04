using NUnit.Framework;
using OAS.Client.Features.Employees.Workspace;
using OAS.Contracts.Features.Employees;

namespace OAS.Tests.Features.Employees;

[TestFixture]
public sealed class EmployeeWorkspaceStateTests
{
    private static readonly Guid JobTitleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public void ExistingEmployee_DirtyStateTracksContactChangesAndRevertRestoresBaseline()
    {
        var employee = CreateEmployee();

        var tab =
            new EmployeeWorkspaceTabState(employee.Id);

        tab.Load(employee);

        Assert.Multiple(() =>
        {
            Assert.That(
                tab.IsDirty,
                Is.False);

            Assert.That(
                tab.IsEditMode,
                Is.False);
        });

        tab.BeginEdit();

        tab.Form.Email =
            "changed@example.com";

        tab.Form.City =
            "Aden";

        Assert.That(
            tab.IsDirty,
            Is.True);

        tab.Revert();

        Assert.Multiple(() =>
        {
            Assert.That(
                tab.IsDirty,
                Is.False);

            Assert.That(
                tab.IsEditMode,
                Is.False);

            Assert.That(
                tab.Form.Email,
                Is.EqualTo(employee.Email));

            Assert.That(
                tab.Form.City,
                Is.EqualTo(employee.City));
        });
    }

    [Test]
    public void NewEmployee_StartsWithReservedEmployeeCodeAndTracksDirtyState()
    {
        var tab =
            new EmployeeWorkspaceTabState(null);

        tab.InitializeNew("EMP-00501");

        Assert.Multiple(() =>
        {
            Assert.That(
                tab.IsDirty,
                Is.False);

            Assert.That(
                tab.IsEditMode,
                Is.True);

            Assert.That(
                tab.Form.EmployeeCode,
                Is.EqualTo("EMP-00501"));
        });

        tab.Form.FirstName = "Ali";

        Assert.That(
            tab.IsDirty,
            Is.True);
    }

    [Test]
    public void EmployeeImageDraft_ParticipatesInDirtyStateAndRevertClearsIt()
    {
        var employee = CreateEmployee();

        var tab =
            new EmployeeWorkspaceTabState(employee.Id);

        tab.Load(employee);

        tab.BeginEdit();

        tab.StageImage(
            [1, 2, 3],
            "image/jpeg",
            "photo.jpg",
            "data:image/jpeg;base64,AQID");

        Assert.That(
            tab.IsDirty,
            Is.True);

        tab.Revert();

        Assert.Multiple(() =>
        {
            Assert.That(
                tab.IsDirty,
                Is.False);

            Assert.That(
                tab.PendingImageContent,
                Is.Null);
        });
    }

    [Test]
    public void LinkedEmployee_RemainsEditableButUserLookupIsLocked()
    {
        var employee =
            CreateEmployee(
                userAccountId: Guid.NewGuid());

        var tab =
            new EmployeeWorkspaceTabState(employee.Id);

        tab.Load(employee);
        tab.BeginEdit();

        Assert.Multiple(() =>
        {
            Assert.That(
                tab.IsUserAccountLinkLocked,
                Is.True);

            Assert.That(
                tab.IsEditMode,
                Is.True);
        });

        tab.Form.FirstName =
            "Updated";

        Assert.That(
            tab.IsDirty,
            Is.True);
    }

    private static EmployeeDto CreateEmployee(
        Guid? userAccountId = null) =>
        new(
            Id: Guid.NewGuid(),
            EmployeeCode: "EMP-00501",
            FirstName: "Ahmed",
            LastName: "Ali",
            DisplayName: "Ahmed Ali",
            Phone: "777000000",
            Email: "employee@example.com",
            Country: "Yemen",
            Governorate: "Sana'a",
            City: "Sana'a",
            PostalCode: "10001",
            ResidentialAddress: "Main Street",
            JobTitleId: JobTitleId,
            JobTitleName: "فني",
            DepartmentId: null,
            DepartmentCode: null,
            DepartmentName: null,
            ManagerEmployeeId: null,
            ManagerEmployeeCode: null,
            ManagerEmployeeName: null,
            HireDate: new DateOnly(2026, 1, 1),
            IsSalesperson: true,
            IsTechnician: true,
            IsCommissionEligible: false,
            IsActive: true,
            UserAccountId: userAccountId,
            UserAccountDisplayName: userAccountId.HasValue ? "Linked User" : null,
            UserAccountUserName: userAccountId.HasValue ? "linked" : null,
            UserAccountEmail: userAccountId.HasValue ? "linked@example.com" : null,
            Photo: null,
            RowVersion: Convert.ToBase64String([1, 2, 3]),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            LastModifiedAtUtc: null);

}