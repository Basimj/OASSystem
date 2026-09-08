using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiSecretValue
{
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public string CopyText { get; set; } = "Copy";
    [Parameter] public EventCallback OnCopy { get; set; }
    private Task CopyAsync() => OnCopy.InvokeAsync();
}
