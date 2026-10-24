namespace OAS.Client.Sales.Workspace;

public partial class SalesWorkspaceHost
{
    // Close inside the current Blazor circuit; never perform a full page reload.
    // CloseTabAsync already asks before discarding a dirty document or revision.
    private async Task ReturnToSalesListAsync()
    {
        if (ActiveTab is not { IsListTab: false } active)
            return;

        if (active.IsSaving || _isSavingRevision || _isCollectingInvoicePayment || _isPrintingInvoice)
            return;

        var tabId = active.TabId;
        await CloseTabAsync(tabId);

        // A declined unsaved-changes dialog keeps the tab alive.
        if (Workspace.FindTab(tabId) is not null)
            return;

        Workspace.OpenOrActivateListTab(Section);
        await LoadListAsync(Section);
    }
}
