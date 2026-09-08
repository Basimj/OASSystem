using Microsoft.JSInterop;

namespace OAS.Client.Services.Browser;

public sealed class BrowserFileDownloadService(
    IJSRuntime jsRuntime)
{
    public async Task DownloadAsync(
        byte[] content,
        string fileName,
        string contentType)
    {
        await jsRuntime.InvokeVoidAsync(
            "oasDownloadFile",
            fileName,
            contentType,
            content);
    }

    public async Task<bool> SaveAsync(
        byte[] content,
        string fileName,
        string contentType)
    {
        return await jsRuntime.InvokeAsync<bool>(
            "oasSaveFile",
            fileName,
            contentType,
            content);
    }
}