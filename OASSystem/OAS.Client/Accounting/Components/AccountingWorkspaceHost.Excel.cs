using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.Client.Accounting.Services;
using OAS.Client.Accounting.Workspace;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.Contracts.Spreadsheets;
namespace OAS.Client.Accounting.Components;
public partial class AccountingWorkspaceHost
{
    [Inject] private AccountingSpreadsheetClient SpreadsheetClient { get; set; } = default!;
    [Inject] private BrowserFileDownloadService SpreadsheetDownload { get; set; } = default!;
    private OAS.UiLib.Components.Spreadsheets.UiSpreadsheetPreview? SpreadsheetUiPreview => _spreadsheetPreview is { } p ? new(p.Rows.Select(x=>new OAS.UiLib.Components.Spreadsheets.UiSpreadsheetRow(x.Sheet,x.RowNumber,x.Values,x.Errors,x.Warnings)).ToArray(),p.ValidRows,p.InvalidRows,p.WarningRows,p.CanImport,p.ImportedRecords) : null;
    private bool _spreadsheetBusy, _spreadsheetOpen;
    private SpreadsheetPreview? _spreadsheetPreview;
    private byte[]? _spreadsheetBytes;
    private string _spreadsheetName="",_spreadsheetSection="";
    private static string? SpreadsheetSection(AccountingEntityType? section)=>section switch
    {
        AccountingEntityType.Accounts=>"accounts",AccountingEntityType.CostCenters=>"cost-centers",
        AccountingEntityType.CashAccounts=>"cash-accounts",AccountingEntityType.BankAccounts=>"bank-accounts",
        AccountingEntityType.ExpenseTypes=>"expense-types",AccountingEntityType.PostingProfiles=>"posting-profiles",
        AccountingEntityType.Journals=>"journals",AccountingEntityType.FiscalYears=>"fiscal-years",
        AccountingEntityType.FiscalPeriods=>"fiscal-periods",AccountingEntityType.ReceiptVouchers=>"receipt-vouchers",
        AccountingEntityType.PaymentVouchers=>"payment-vouchers",AccountingEntityType.PaymentAllocations=>"payment-allocations",
        AccountingEntityType.Expenses=>"expenses",AccountingEntityType.CashShifts=>"cash-shifts",_=>null
    };
    private static bool SpreadsheetCanImport(AccountingEntityType? section)=>section is AccountingEntityType.Accounts or AccountingEntityType.CostCenters or AccountingEntityType.CashAccounts or AccountingEntityType.BankAccounts or AccountingEntityType.ExpenseTypes or AccountingEntityType.PostingProfiles or AccountingEntityType.Journals;
    private void OpenSpreadsheet(AccountingEntityType type)
    { _spreadsheetSection=SpreadsheetSection(type)!;_spreadsheetPreview=null;_spreadsheetBytes=null;_spreadsheetOpen=true; }
    private void CloseSpreadsheet() { if(!_spreadsheetBusy) _spreadsheetOpen=false; }
    private async Task SpreadsheetRun(Func<Task> operation)
    {
        if(_spreadsheetBusy) return;
        _spreadsheetBusy=true;
        try { await operation(); }
        catch(ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _spreadsheetBusy=false; }
    }
    private Task DownloadSpreadsheet(AccountingEntityType type,bool template)=>SpreadsheetRun(async()=>
    {
        var section=SpreadsheetSection(type)!;
        var source=type==AccountingEntityType.PaymentAllocations?Section switch { AccountingEntityType.ReceiptVouchers=>"ReceiptVoucher",AccountingEntityType.PaymentVouchers=>"PaymentVoucher",_=>null }:null;
        var bytes=template?await SpreadsheetClient.TemplateAsync(section):await SpreadsheetClient.ExportAsync(section,GetSearch(type),source);
        await SpreadsheetDownload.SaveAsync(bytes,$"{section}{(template?"-template":"")}.xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    });
    private Task PreviewSpreadsheet(InputFileChangeEventArgs args)=>SpreadsheetRun(async()=>
    {
        _spreadsheetPreview=null;_spreadsheetBytes=null;
        var file=args.File;
        if(!file.Name.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase) || file.Size>10*1024*1024) { Snackbar.Error("اختر ملف xlsx بحجم لا يتجاوز 10 MB.");return; }
        using var memory=new MemoryStream();await using var stream=file.OpenReadStream(10*1024*1024);
        await stream.CopyToAsync(memory);_spreadsheetBytes=memory.ToArray();_spreadsheetName=file.Name;
        _spreadsheetPreview=await SpreadsheetClient.UploadAsync(_spreadsheetSection,_spreadsheetBytes,_spreadsheetName,false);
    });
    private Task ConfirmSpreadsheet()=>SpreadsheetRun(async()=>
    {
        if(_spreadsheetBytes is null || _spreadsheetPreview?.CanImport!=true || _spreadsheetPreview.ImportedRecords>0) return;
        _spreadsheetPreview=await SpreadsheetClient.UploadAsync(_spreadsheetSection,_spreadsheetBytes,_spreadsheetName,true);
        if(_spreadsheetPreview.ImportedRecords>0) { Snackbar.Success($"تم استيراد {_spreadsheetPreview.ImportedRecords} سجل بنجاح.");_spreadsheetBytes=null;if(Section.HasValue) await LoadSectionDataAsync(Section.Value); }
    });
}
