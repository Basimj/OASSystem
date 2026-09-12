using NUnit.Framework;
using OAS.Client.Features.Employees.Workspace;

namespace OAS.Tests.Features.Employees.Client;

[TestFixture]
public sealed class EmployeesWorkspaceStateTests
{
    [Test]
    public void ListState_PreservesSearchFilterAndPage()
    {
        var state = new EmployeesWorkspaceState
        {
            SearchText = "Ahmed",
            AppliedSearch = "Ahmed",
            PageNumber = 3,
            Filter = EmployeeListFilter.Commission,
            HasLoadedPage = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                state.SearchText,
                Is.EqualTo("Ahmed"));

            Assert.That(
                state.PageNumber,
                Is.EqualTo(3));

            Assert.That(
                state.Filter,
                Is.EqualTo(
                    EmployeeListFilter.Commission));

            Assert.That(
                state.HasLoadedPage,
                Is.True);
        });
    }

    [Test]
    public void CreateNewTab_EachReservationCreatesIndependentEditableTab()
    {
        var state =
            new EmployeesWorkspaceState();

        var first =
            state.CreateNewTab("EMP-00501");

        var second =
            state.CreateNewTab("EMP-00502");

        Assert.Multiple(() =>
        {
            Assert.That(
                first.TabId,
                Is.Not.EqualTo(second.TabId));

            Assert.That(
                first.Form.EmployeeCode,
                Is.EqualTo("EMP-00501"));

            Assert.That(
                second.Form.EmployeeCode,
                Is.EqualTo("EMP-00502"));

            Assert.That(
                first.IsEditMode,
                Is.True);

            Assert.That(
                second.IsEditMode,
                Is.True);

            Assert.That(
                first.IsDirty,
                Is.False);

            Assert.That(
                second.IsDirty,
                Is.False);
        });

        first.Form.FirstName =
            "Basim";

        Assert.That(
            first.IsDirty,
            Is.True);

        Assert.That(
            second.IsDirty,
            Is.False);

        first.Revert();

        Assert.Multiple(() =>
        {
            Assert.That(
                first.Form.FirstName,
                Is.Empty);

            Assert.That(
                first.IsDirty,
                Is.False);

            Assert.That(
                first.Form.EmployeeCode,
                Is.EqualTo("EMP-00501"));
        });
    }

    [Test]
    public void SameEmployee_ReusesExistingTab()
    {
        var state =
            new EmployeesWorkspaceState();

        var employeeId =
            Guid.NewGuid();

        var first =
            state.GetOrCreateEmployeeTab(
                employeeId);

        var second =
            state.GetOrCreateEmployeeTab(
                employeeId);

        Assert.That(
            second.TabId,
            Is.EqualTo(first.TabId));
    }
}