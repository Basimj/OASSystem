using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiRecordListHeader
{
    [Parameter] public string Primary { get; set; } = string.Empty;
    [Parameter] public string Column1 { get; set; } = string.Empty;
    [Parameter] public string Column2 { get; set; } = string.Empty;
    [Parameter] public string Column3 { get; set; } = string.Empty;
    [Parameter] public string Trailing { get; set; } = string.Empty;
}
