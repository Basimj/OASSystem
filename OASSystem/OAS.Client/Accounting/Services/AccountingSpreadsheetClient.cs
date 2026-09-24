using OAS.Client.Services.Http;
using OAS.Contracts.Spreadsheets;
namespace OAS.Client.Accounting.Services;
public sealed class AccountingSpreadsheetClient(OasApiClient api)
{
    private static string Url(string section)=>$"api/accounting/spreadsheets/{section}";
    public Task<byte[]> TemplateAsync(string section)=>api.GetFileAsync($"{Url(section)}/template");
    public Task<byte[]> ExportAsync(string section,string? search,string? sourceType=null)=>api.GetFileAsync($"{Url(section)}/export?search={Uri.EscapeDataString(search??"")}&sourceType={Uri.EscapeDataString(sourceType??"")}");
    public async Task<SpreadsheetPreview> UploadAsync(string section,byte[] bytes,string name,bool confirm)
    {
        using var stream=new MemoryStream(bytes,false);
        return await api.UploadFileAsync<SpreadsheetPreview>($"{Url(section)}/{(confirm?"import":"preview")}",stream,name) ?? new SpreadsheetPreview([]);
    }
}
