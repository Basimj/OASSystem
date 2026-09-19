using NUnit.Framework;
using OAS.Client.Accounting.Workspace;

namespace OAS.Tests.Accounting.Client;

[TestFixture]
public sealed class AccountingWorkspaceStateTests
{
    [Test]
    public void Workspace_InitializesWithDefaultAccountsTab()
    {
        var state = new AccountingWorkspaceState();

        Assert.That(state.Tabs, Has.Count.EqualTo(1));
        var defaultTab = state.Tabs[0];
        Assert.Multiple(() =>
        {
            Assert.That(defaultTab.EntityType, Is.EqualTo(AccountingEntityType.Accounts));
            Assert.That(defaultTab.IsListTab, Is.True);
            Assert.That(state.ActiveTabId, Is.EqualTo(defaultTab.TabId));
        });
    }

    [Test]
    public void OpenOrActivateEntityTab_WhenTabAlreadyExists_DoesNotCreateDuplicateAndActivatesIt()
    {
        var state = new AccountingWorkspaceState();
        var entityId = Guid.NewGuid();

        var firstTab = state.OpenOrActivateEntityTab(AccountingEntityType.Accounts, entityId, "1100 - النقدية");
        var initialCount = state.Tabs.Count;

        // Try opening the same entity record again
        var secondTab = state.OpenOrActivateEntityTab(AccountingEntityType.Accounts, entityId, "1100 - النقدية");

        Assert.Multiple(() =>
        {
            Assert.That(state.Tabs, Has.Count.EqualTo(initialCount), "Tabs count must not increase when activating an already open entity.");
            Assert.That(secondTab.TabId, Is.EqualTo(firstTab.TabId), "Existing tab must be reused.");
            Assert.That(state.ActiveTabId, Is.EqualTo(firstTab.TabId), "Existing tab must be activated.");
        });
    }

    [Test]
    public void OpenOrActivateEntityTab_WhenTabDoesNotExist_CreatesNewTabAndActivatesIt()
    {
        var state = new AccountingWorkspaceState();
        var entityId = Guid.NewGuid();

        var tab = state.OpenOrActivateEntityTab(AccountingEntityType.Journals, entityId, "قيد #10025");

        Assert.Multiple(() =>
        {
            Assert.That(tab.EntityId, Is.EqualTo(entityId));
            Assert.That(tab.Title, Is.EqualTo("قيد #10025"));
            Assert.That(tab.IsListTab, Is.False);
            Assert.That(state.ActiveTabId, Is.EqualTo(tab.TabId));
        });
    }

    [Test]
    public void OpenNewEntityTab_CreatesNewTabInEditMode()
    {
        var state = new AccountingWorkspaceState();

        var newTab = state.OpenNewEntityTab(AccountingEntityType.ReceiptVouchers);

        Assert.Multiple(() =>
        {
            Assert.That(newTab.IsNew, Is.True);
            Assert.That(newTab.IsEditMode, Is.True);
            Assert.That(newTab.Title, Is.EqualTo("سند قبض جديد"));
            Assert.That(state.ActiveTabId, Is.EqualTo(newTab.TabId));
        });
    }

    [Test]
    public void RemoveTab_WhenClosingActiveTab_SelectsAdjacentTab()
    {
        var state = new AccountingWorkspaceState();
        var entity1 = Guid.NewGuid();
        var entity2 = Guid.NewGuid();

        var tab1 = state.OpenOrActivateEntityTab(AccountingEntityType.Accounts, entity1, "حساب 1");
        var tab2 = state.OpenOrActivateEntityTab(AccountingEntityType.Accounts, entity2, "حساب 2");

        Assert.That(state.ActiveTabId, Is.EqualTo(tab2.TabId));

        var removed = state.RemoveTab(tab2.TabId);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(state.ActiveTabId, Is.EqualTo(tab1.TabId));
        });
    }

    [Test]
    public void AccountingTabState_DirtyTracking_DetectsChanges()
    {
        var tab = new AccountingTabState(AccountingEntityType.Accounts, Guid.NewGuid(), isListTab: false, title: "حساب");
        tab.CaptureBaseline("{\"Code\":\"1100\",\"Name\":\"نقدية\"}");

        Assert.That(tab.IsDirty, Is.False);

        tab.CheckDirty("{\"Code\":\"1100\",\"Name\":\"نقدية معدلة\"}");
        Assert.That(tab.IsDirty, Is.True);

        tab.CheckDirty("{\"Code\":\"1100\",\"Name\":\"نقدية\"}");
        Assert.That(tab.IsDirty, Is.False);
    }

    [Test]
    public void AccountingTabState_CompleteSave_TransitionsToViewMode()
    {
        var tab = new AccountingTabState(AccountingEntityType.Accounts, entityId: null, isListTab: false, title: "إضافة حساب");
        Assert.Multiple(() =>
        {
            Assert.That(tab.IsNew, Is.True);
            Assert.That(tab.IsEditMode, Is.True);
        });

        var newId = Guid.NewGuid();
        tab.CompleteSave(newId, "1100 - الصندوق", new object(), "{}");

        Assert.Multiple(() =>
        {
            Assert.That(tab.IsNew, Is.False);
            Assert.That(tab.IsEditMode, Is.False);
            Assert.That(tab.EntityId, Is.EqualTo(newId));
            Assert.That(tab.Title, Is.EqualTo("1100 - الصندوق"));
            Assert.That(tab.IsDirty, Is.False);
        });
    }
}
