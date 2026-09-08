using NUnit.Framework;
using OAS.Client.Identity.Users.Workspace;

namespace OAS.Tests.Identity.Users.Client;

[TestFixture]
public sealed class UsersWorkspaceStateTests
{
    [Test]
    public void AllUsersTab_IsPermanentDefaultTab()
    {
        var state = new UsersWorkspaceState();

        Assert.Multiple(() =>
        {
            Assert.That(state.AllUsersTabId, Is.EqualTo(Guid.Empty));
            Assert.That(state.ActiveTabId, Is.EqualTo(state.AllUsersTabId));
            Assert.That(state.ActiveUserTab, Is.Null);
            Assert.That(state.UserTabs, Is.Empty);
        });
    }

    [Test]
    public void OpenExisting_SameUser_DoesNotCreateDuplicateTab()
    {
        var state = new UsersWorkspaceState();
        var userId = Guid.NewGuid();

        var first = state.OpenExisting(userId, "User One");
        var second = state.OpenExisting(userId, "User One");

        Assert.Multiple(() =>
        {
            Assert.That(second.WorkspaceTabId, Is.EqualTo(first.WorkspaceTabId));
            Assert.That(state.UserTabs, Has.Count.EqualTo(1));
            Assert.That(state.ActiveTabId, Is.EqualTo(first.WorkspaceTabId));
        });
    }

    [Test]
    public void NewDrafts_AreIndependentAndPreserveDirtyState()
    {
        var state = new UsersWorkspaceState();

        var first = state.OpenNew("New User");
        first.Editor.Form.FirstName = "Ahmed";

        var second = state.OpenNew("New User");
        second.Editor.Form.FirstName = "Sara";

        state.Activate(first.WorkspaceTabId);

        Assert.Multiple(() =>
        {
            Assert.That(state.UserTabs, Has.Count.EqualTo(2));
            Assert.That(first.IsDirty, Is.True);
            Assert.That(second.IsDirty, Is.True);
            Assert.That(first.Editor.Form.FirstName, Is.EqualTo("Ahmed"));
            Assert.That(second.Editor.Form.FirstName, Is.EqualTo("Sara"));
            Assert.That(state.ActiveUserTab, Is.SameAs(first));
        });
    }

    [Test]
    public void CloseActiveTab_SelectsPreviousTabWithoutClosingAllUsers()
    {
        var state = new UsersWorkspaceState();
        var first = state.OpenNew("New User");
        var second = state.OpenNew("New User");

        state.Close(second.WorkspaceTabId);
        Assert.That(state.ActiveTabId, Is.EqualTo(first.WorkspaceTabId));

        state.Close(first.WorkspaceTabId);
        Assert.Multiple(() =>
        {
            Assert.That(state.ActiveTabId, Is.EqualTo(state.AllUsersTabId));
            Assert.That(state.UserTabs, Is.Empty);
        });
    }
}
