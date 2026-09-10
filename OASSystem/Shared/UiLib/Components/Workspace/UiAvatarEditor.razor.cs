using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace OAS.UiLib.Components.Workspace;

public partial class UiAvatarEditor
{
    [Parameter] public string Initials { get; set; } = string.Empty;
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public string AltText { get; set; } = string.Empty;
    [Parameter] public int Size { get; set; } = 68;
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public bool ShowRemove { get; set; } = true;
    [Parameter] public string EditText { get; set; } = "تغيير الصورة";
    [Parameter] public string RemoveText { get; set; } = "حذف الصورة";
    [Parameter] public string Accept { get; set; } = "image/jpeg,image/png,image/webp";
    [Parameter] public EventCallback<IBrowserFile> OnImageSelected { get; set; }
    [Parameter] public EventCallback OnRemove { get; set; }
    private Task HandleFileAsync(InputFileChangeEventArgs args) => args.File is null ? Task.CompletedTask : OnImageSelected.InvokeAsync(args.File);
    private Task RemoveAsync() => OnRemove.InvokeAsync();
}
