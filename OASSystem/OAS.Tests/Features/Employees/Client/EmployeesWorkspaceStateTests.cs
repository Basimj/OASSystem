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
            Filter = EmployeeListFilter.Technician,
            HasLoadedPage = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(state.SearchText, Is.EqualTo("Ahmed"));
            Assert.That(state.AppliedSearch, Is.EqualTo("Ahmed"));
            Assert.That(state.PageNumber, Is.EqualTo(3));
            Assert.That(state.Filter, Is.EqualTo(EmployeeListFilter.Technician));
            Assert.That(state.HasLoadedPage, Is.True);
        });
    }

    [Test]
    public void EditorState_MatchesSameRouteAndKeepsDraftValues()
    {
        var state = new EmployeesWorkspaceState();
        var employeeId = Guid.NewGuid();

        state.Editor.Begin(employeeId);
        state.Editor.Form.FirstName = "Basim";
        state.Editor.Form.LastName = "Employee";
        state.Editor.RowVersion = "AQID";
        state.Editor.ActiveSection = "contact";
        state.Editor.MarkInitialized();

        Assert.Multiple(() =>
        {
            Assert.That(state.Editor.Matches(employeeId), Is.True);
            Assert.That(state.Editor.Form.FirstName, Is.EqualTo("Basim"));
            Assert.That(state.Editor.Form.LastName, Is.EqualTo("Employee"));
            Assert.That(state.Editor.RowVersion, Is.EqualTo("AQID"));
            Assert.That(state.Editor.ActiveSection, Is.EqualTo("contact"));
        });
    }

    [Test]
    public void BeginDifferentEditor_ClearsPreviousDraft()
    {
        var state = new EmployeesWorkspaceState();
        state.Editor.Begin(Guid.NewGuid());
        state.Editor.Form.FirstName = "Old";
        state.Editor.MarkInitialized();

        var nextId = Guid.NewGuid();
        state.Editor.Begin(nextId);

        Assert.Multiple(() =>
        {
            Assert.That(state.Editor.Matches(nextId), Is.False);
            Assert.That(state.Editor.Form.FirstName, Is.Empty);
            Assert.That(state.Editor.RowVersion, Is.Null);
            Assert.That(state.Editor.ActiveSection, Is.EqualTo("basic"));
        });
    }
}
