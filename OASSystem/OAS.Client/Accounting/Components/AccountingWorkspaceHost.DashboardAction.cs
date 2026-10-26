using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Workspace;
using OAS.Contracts.Accounting.Enums;

namespace OAS.Client.Accounting.Components;

// Small, additive extension. Keeps the 4,000-line host unchanged and ensures
// /accounting/journals?new=true creates a working new-journal editor tab.
public partial class AccountingWorkspaceHost
{
    [Parameter] public bool OpenNewJournalFromDashboard { get; set; }

    private bool _dashboardNewJournalOpened;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_dashboardNewJournalOpened ||
            !OpenNewJournalFromDashboard ||
            Section != AccountingEntityType.Journals ||
            _loadedSection != AccountingEntityType.Journals)
        {
            return;
        }

        // Set the flag before awaiting, avoiding creation of duplicate tabs.
        _dashboardNewJournalOpened = true;
        await OpenNewRecord(AccountingEntityType.Journals);
    }
}
