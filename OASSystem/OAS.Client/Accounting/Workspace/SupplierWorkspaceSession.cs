using OAS.UiLib.Core.Models.Accounting;

namespace OAS.Client.Accounting.Workspace;

/// <summary>Retains supplier drafts and list state when navigating to financial pages.</summary>
public sealed class SupplierWorkspaceSession
{
    public Guid ListTabId { get; } = Guid.NewGuid();
    public Guid ActiveTabId { get; set; }
    public List<SupplierEditorTab> EditorTabs { get; } = [];
    public string Search { get; set; } = string.Empty;
    public string Filter { get; set; } = "all";
    public int PageNumber { get; set; } = 1;

    public SupplierWorkspaceSession() => ActiveTabId = ListTabId;

    public bool Activate(Guid tabId)
    {
        if (tabId != ListTabId && !EditorTabs.Any(t => t.TabId == tabId))
            return false;
        ActiveTabId = tabId;
        return true;
    }
}

public sealed class SupplierEditorTab(Guid? entityId, string title, UiAccountingPartyFormModel form)
{
    public Guid TabId { get; } = Guid.NewGuid();
    public Guid? EntityId { get; set; } = entityId;
    public string Title { get; set; } = title;
    public UiAccountingPartyFormModel Form { get; set; } = form;
    public bool IsDirty { get; set; }
    public bool IsNew => !EntityId.HasValue;
}
