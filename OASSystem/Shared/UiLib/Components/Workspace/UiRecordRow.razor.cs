using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Workspace;

public partial class UiRecordRow
{
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public bool Selected { get; set; }
    [Parameter] public RenderFragment? Leading { get; set; }
    [Parameter] public RenderFragment? Content { get; set; }
    [Parameter] public RenderFragment? Trailing { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    private Task HandleClickAsync() => OnClick.InvokeAsync();
}
