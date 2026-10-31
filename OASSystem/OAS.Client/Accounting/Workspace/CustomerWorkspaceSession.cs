using OAS.UiLib.Core.Models.Accounting;

namespace OAS.Client.Accounting.Workspace;

/// <summary>
/// Keeps customer draft tabs and list context while navigating to the financial
/// workspace and customer credit settlement pages in the same browser session.
/// </summary>
public sealed class CustomerWorkspaceSession
{
    public Guid ListTabId { get; } = Guid.NewGuid();
    public Guid ActiveTabId { get; set; }
    public List<CustomerEditorTab> EditorTabs { get; } = [];
    public string Search { get; set; } = string.Empty;
    public string Filter { get; set; } = "all";
    public int PageNumber { get; set; } = 1;

    public CustomerWorkspaceSession() => ActiveTabId = ListTabId;

    public bool Activate(Guid tabId)
    {
        if (tabId != ListTabId && !EditorTabs.Any(t => t.TabId == tabId))
            return false;

        ActiveTabId = tabId;
        return true;
    }
}

public sealed class CustomerEditorTab
{
    public CustomerEditorTab(Guid? entityId, string title, UiAccountingPartyFormModel form)
    {
        EntityId = entityId;
        Title = title;
        Form = form;
    }

    public Guid TabId { get; } = Guid.NewGuid();
    public Guid? EntityId { get; set; }
    public string Title { get; set; }
    public UiAccountingPartyFormModel Form { get; set; }
    public bool IsDirty { get; set; }
    public bool IsNew => !EntityId.HasValue;
}
