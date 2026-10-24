using System;
using System.Threading.Tasks;
using OAS.Contracts.Accounting.Settings;

namespace OAS.Client.Sales.Workspace;

public partial class SalesWorkspaceHost
{
    private Guid? _walkInCustomerId;
    private string? _walkInCustomerLabel;

    private Task OnWalkInSettingsLoadedAsync(WalkInCashCustomerSettingsDto? value)
    {
        _walkInCustomerId = value?.CustomerId;
        _walkInCustomerLabel = value?.CustomerId.HasValue == true
            ? $"{value.CustomerCode} - {value.CustomerName}"
            : null;
        return InvokeAsync(StateHasChanged);
    }
}
