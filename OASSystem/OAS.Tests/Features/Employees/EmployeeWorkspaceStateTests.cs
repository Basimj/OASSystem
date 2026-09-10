using NUnit.Framework;
using OAS.Client.Features.Employees.Workspace;
using OAS.Contracts.Features.Employees;

namespace OAS.Tests.Features.Employees;

[TestFixture]
public sealed class EmployeeWorkspaceStateTests
{
    private static readonly Guid JobTitleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public void ExistingEmployee_DirtyStateTracksContactChangesAndRevertRestoresBaseline()
    {
        var employee = CreateEmployee();
        var tab = new EmployeeWorkspaceTabState(employee.Id);
        tab.Load(employee);

        Assert.That(tab.IsDirty, Is.False);
        Assert.That(tab.IsEditMode, Is.False);

        tab.BeginEdit();
        tab.Form.Email = "changed@example.com";
        tab.Form.City = "Aden";
        Assert.That(tab.IsDirty, Is.True);

        tab.Revert();
        Assert.Multiple(() =>
        {
            Assert.That(tab.IsDirty, Is.False);
            Assert.That(tab.IsEditMode, Is.False);
            Assert.That(tab.Form.Email, Is.EqualTo(employee.Email));
            Assert.That(tab.Form.City, Is.EqualTo(employee.City));
        });
    }

    [Test]
    public void NewEmployee_StartsWithReservedNumberAndTracksDirtyState()
    {
        var tab = new EmployeeWorkspaceTabState(null);
        tab.InitializeNew(501);

        Assert.Multiple(() =>
        {
            Assert.That(tab.IsDirty, Is.False);
            Assert.That(tab.IsEditMode, Is.True);
            Assert.That(tab.Form.EmployeeNumber, Is.EqualTo(501));
            Assert.That(tab.Form.EmployeeCode, Is.EqualTo("501"));
        });

        tab.Form.FirstName = "Ali";
        Assert.That(tab.IsDirty, Is.True);
    }

    [Test]
    public void EmployeeImageDraft_ParticipatesInDirtyStateAndRevertClearsIt()
    {
        var employee = CreateEmployee();
        var tab = new EmployeeWorkspaceTabState(employee.Id);
        tab.Load(employee);
        tab.BeginEdit();
        tab.StageImage([1, 2, 3], "image/jpeg", "photo.jpg", "data:image/jpeg;base64,AQID");

        Assert.That(tab.IsDirty, Is.True);
        tab.Revert();
        Assert.That(tab.IsDirty, Is.False);
        Assert.That(tab.PendingImageContent, Is.Null);
    }

    [Test]
    public void LinkedEmployee_RemainsEditableButUserLookupIsLocked()
    {
        var employee = CreateEmployee(userAccountId: Guid.NewGuid());
        var tab = new EmployeeWorkspaceTabState(employee.Id);
        tab.Load(employee);
        tab.BeginEdit();

        Assert.Multiple(() =>
        {
            Assert.That(tab.IsUserAccountLinkLocked, Is.True);
            Assert.That(tab.IsEditMode, Is.True);
        });

        tab.Form.FirstName = "Updated";
        Assert.That(tab.IsDirty, Is.True);
    }

    private static EmployeeDto CreateEmployee(Guid? userAccountId = null) => new(
        Guid.NewGuid(),
        1,
        "1",
        "Ahmed",
        "Ali",
        "Ahmed Ali",
        "777000000",
        "employee@example.com",
        "Yemen",
        "Sana'a",
        "Sana'a",
        "10001",
        "Main Street",
        JobTitleId,
        "فني",
        new DateOnly(2026, 1, 1),
        true,
        true,
        userAccountId,
        userAccountId.HasValue ? "Linked User" : null,
        userAccountId.HasValue ? "linked" : null,
        userAccountId.HasValue ? "linked@example.com" : null,
        null,
        Convert.ToBase64String([1, 2, 3]),
        DateTimeOffset.UtcNow,
        null);
}
