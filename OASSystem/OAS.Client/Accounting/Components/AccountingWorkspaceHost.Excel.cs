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
    [Inject]
    private AccountingSpreadsheetClient SpreadsheetClient { get; set; } = default!;

    [Inject]
    private BrowserFileDownloadService SpreadsheetDownload { get; set; } = default!;

    private OAS.UiLib.Components.Spreadsheets.UiSpreadsheetPreview? SpreadsheetUiPreview =>
        _spreadsheetPreview is { } preview
            ? new OAS.UiLib.Components.Spreadsheets.UiSpreadsheetPreview(
                preview.Rows
                    .Select(x => new OAS.UiLib.Components.Spreadsheets.UiSpreadsheetRow(
                        x.Sheet,
                        x.RowNumber,
                        x.Values,
                        x.Errors,
                        x.Warnings))
                    .ToArray(),
                preview.ValidRows,
                preview.InvalidRows,
                preview.WarningRows,
                preview.CanImport,
                preview.ImportedRecords)
            : null;

    private bool _spreadsheetBusy;

    private bool _spreadsheetOpen;

    // جلسة مستقلة لكل مرة يتم فيها فتح نافذة Excel.
    // تستخدم مع @key في AccountingWorkspaceHost.razor.
    private long _spreadsheetImportSession;

    private SpreadsheetPreview? _spreadsheetPreview;

    // يجب ألا تبقى Bytes الملف السابق عند فتح استيراد جديد.
    private byte[]? _spreadsheetBytes;

    private string _spreadsheetName = "";

    private string _spreadsheetSection = "";

    private string SpreadsheetTitle => _spreadsheetSection switch
    {
        "accounts" => "الحسابات",
        "cost-centers" => "مراكز التكلفة",
        "cash-accounts" => "الصناديق",
        "bank-accounts" => "الحسابات البنكية",
        "expense-types" => "أنواع المصروفات",
        "posting-profiles" => "ملفات الترحيل",
        "journals" => "القيود المحاسبية",
        _ => "Excel"
    };

    private static string? SpreadsheetSection(
        AccountingEntityType? section) => section switch
        {
            AccountingEntityType.Accounts => "accounts",

            AccountingEntityType.CostCenters => "cost-centers",

            AccountingEntityType.CashAccounts => "cash-accounts",

            AccountingEntityType.BankAccounts => "bank-accounts",

            AccountingEntityType.ExpenseTypes => "expense-types",

            AccountingEntityType.PostingProfiles => "posting-profiles",

            AccountingEntityType.Journals => "journals",

            AccountingEntityType.FiscalYears => "fiscal-years",

            AccountingEntityType.FiscalPeriods => "fiscal-periods",

            AccountingEntityType.ReceiptVouchers => "receipt-vouchers",

            AccountingEntityType.PaymentVouchers => "payment-vouchers",

            AccountingEntityType.PaymentAllocations => "payment-allocations",

            AccountingEntityType.Expenses => "expenses",

            AccountingEntityType.CashShifts => "cash-shifts",

            _ => null
        };

    private static bool SpreadsheetCanImport(
        AccountingEntityType? section) =>
        section is AccountingEntityType.Accounts
            or AccountingEntityType.CostCenters
            or AccountingEntityType.CashAccounts
            or AccountingEntityType.BankAccounts
            or AccountingEntityType.ExpenseTypes
            or AccountingEntityType.PostingProfiles
            or AccountingEntityType.Journals;

    private void OpenSpreadsheet(AccountingEntityType type)
    {
        _spreadsheetSection = SpreadsheetSection(type)!;

        // مهم:
        // أي استيراد جديد يجب أن يبدأ بحالة نظيفة تمامًا.
        _spreadsheetPreview = null;
        _spreadsheetBytes = null;
        _spreadsheetName = "";

        // يجعل Blazor ينشئ UiSpreadsheetImport جديدًا بالكامل.
        _spreadsheetImportSession++;

        _spreadsheetOpen = true;
    }

    private void CloseSpreadsheet()
    {
        if (_spreadsheetBusy)
        {
            return;
        }

        _spreadsheetOpen = false;

        // التخلص من كل بيانات الملف السابق.
        _spreadsheetPreview = null;
        _spreadsheetBytes = null;
        _spreadsheetName = "";
    }

    private void ClearSpreadsheetFile()
    {
        if (_spreadsheetBusy)
        {
            return;
        }

        // عند اختيار ملف جديد لا نسمح ببقاء أي شيء
        // مرتبط بالملف السابق.
        _spreadsheetPreview = null;
        _spreadsheetBytes = null;
        _spreadsheetName = "";
    }

    private async Task SpreadsheetRun(Func<Task> operation)
    {
        if (_spreadsheetBusy)
        {
            return;
        }

        _spreadsheetBusy = true;

        try
        {
            await operation();
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
        }
        catch
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _spreadsheetBusy = false;
        }
    }

    private Task DownloadSpreadsheet(
        AccountingEntityType type,
        bool template) =>
        SpreadsheetRun(async () =>
        {
            var section = SpreadsheetSection(type)!;

            var source = type == AccountingEntityType.PaymentAllocations
                ? Section switch
                {
                    AccountingEntityType.ReceiptVouchers =>
                        "ReceiptVoucher",

                    AccountingEntityType.PaymentVouchers =>
                        "PaymentVoucher",

                    _ => null
                }
                : null;

            var bytes = template
                ? await SpreadsheetClient.TemplateAsync(section)
                : await SpreadsheetClient.ExportAsync(
                    section,
                    GetSearch(type),
                    source);

            await SpreadsheetDownload.SaveAsync(
                bytes,
                $"{section}{(template ? "-template" : "")}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        });

    private Task PreviewSpreadsheet(IBrowserFile file) =>
        SpreadsheetRun(async () =>
        {
            // مهم جدًا:
            // تنظيف الملف السابق قبل حتى محاولة قراءة الجديد.
            _spreadsheetPreview = null;
            _spreadsheetBytes = null;
            _spreadsheetName = "";

            if (!file.Name.EndsWith(
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase) ||
                file.Size > 10 * 1024 * 1024)
            {
                _spreadsheetPreview = new SpreadsheetPreview(
                [
                    new SpreadsheetRowResult(
                        "الملف",
                        1,
                        new Dictionary<string, string>(),
                        [
                            "اختر ملف xlsx بحجم لا يتجاوز 10 MB."
                        ],
                        [])
                ]);

                return;
            }

            using var memory = new MemoryStream();

            await using var stream =
                file.OpenReadStream(10 * 1024 * 1024);

            await stream.CopyToAsync(memory);

            // هنا يتم تخزين Bytes الملف الجديد فقط.
            _spreadsheetBytes = memory.ToArray();

            _spreadsheetName = file.Name;

            _spreadsheetPreview =
                await SpreadsheetClient.UploadAsync(
                    _spreadsheetSection,
                    _spreadsheetBytes,
                    _spreadsheetName,
                    false);
        });

    private Task ConfirmSpreadsheet() =>
        SpreadsheetRun(async () =>
        {
            if (_spreadsheetBytes is null ||
                _spreadsheetPreview?.CanImport != true ||
                _spreadsheetPreview.ImportedRecords > 0)
            {
                return;
            }

            _spreadsheetPreview =
                await SpreadsheetClient.UploadAsync(
                    _spreadsheetSection,
                    _spreadsheetBytes,
                    _spreadsheetName,
                    true);

            if (_spreadsheetPreview.ImportedRecords > 0)
            {
                Snackbar.Success(
                    $"تم استيراد {_spreadsheetPreview.ImportedRecords} سجل بنجاح.");

                // بعد نجاح الاستيراد لا نحتفظ بالملف.
                _spreadsheetBytes = null;
                _spreadsheetName = "";

                if (Section.HasValue)
                {
                    await LoadSectionDataAsync(Section.Value);
                }
            }
        });
}