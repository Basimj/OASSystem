using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Sales.Common;
using OAS.Client.Accounting.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Sales.Mapping;
using OAS.Client.Sales.Services;
using OAS.Client.Printing.Services;
using OAS.Client.Sales.Workspace;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Features.Employees;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.Lookups;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Contracts.Sales.PriceOverrides;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Sales;
using OAS.UiLib.Components.Sales.Invoices;
using OAS.UiLib.Services.Dialogs;

namespace OAS.Client.Sales.Workspace;

public partial class SalesWorkspaceHost : IDisposable
{
    [Parameter] public SalesEntityType Section { get; set; }
    [Inject] private ISalesClientService Sales { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IEmployeeClientService Employees { get; set; } = default!;
    [Inject] private ISalesWorkspaceState Workspace { get; set; } = default!;
    [Inject] private IUiDialogService Dialogs { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IPrintingClientService PrintingService { get; set; } = default!;

    private readonly List<UiSalesDocumentListItem> _listItems = [];
    private readonly Dictionary<Guid, SalesCustomerLookupDto> _customers = [];
    private readonly Dictionary<Guid, EmployeeDto> _employees = [];
    private readonly Dictionary<Guid, SalesProductTypeLookupDto> _productTypes = [];
    private readonly Dictionary<Guid, SalesProductCategoryLookupDto> _categories = [];
    private readonly Dictionary<Guid, SalesProductVariantLookupDto> _products = [];
    private readonly Dictionary<Guid, SalesWarehouseLookupDto> _warehouses = [];
    private readonly Dictionary<Guid, SalesCurrencyLookupDto> _currencies = [];
    private readonly Dictionary<Guid, SalesCashAccountLookupDto> _cashAccounts = [];
    private readonly Dictionary<Guid, SalesBankAccountLookupDto> _bankAccounts = [];
    private readonly Dictionary<Guid, SalesPrescriptionRevisionLookupDto> _revisions = [];
    private string? _search;
    private bool _isLoading;
    private int _pageNumber = 1;
    private int _totalPages;
    private long _totalCount;
    private string? _error;
    private string? _success;
    private readonly Dictionary<Guid, UiPrescriptionRevisionModel> _revisionDrafts = [];
    private readonly HashSet<Guid> _dirtyRevisionDraftTabs = [];
    private bool _isSavingRevision;
    private bool _isPrintingInvoice;
    private SalesEntityType? _lastSection;

    private SalesWorkspaceTabState? ActiveTab => Workspace.ActiveTab;
    private UiPrescriptionRevisionModel? ActiveRevisionDraft =>
        ActiveTab is not null && _revisionDrafts.TryGetValue(ActiveTab.TabId, out var draft)
            ? draft
            : null;
    private SalesEntityType ActiveEntityType => ActiveTab?.EntityType ?? Section;
    private IReadOnlyList<UiSalesDocumentListItem> ListItems => _listItems;
    private string ListTitle => ActiveEntityType switch { SalesEntityType.Prescriptions => "الوصفات", SalesEntityType.CustomerOrders => "طلبات العملاء", _ => "فواتير المبيعات" };
    private string ListSubtitle => ActiveEntityType switch { SalesEntityType.Prescriptions => "إدارة الوصفات الطبية وإصدارات القياس", SalesEntityType.CustomerOrders => "طلبات العملاء وحالة توفر المخزون", _ => "الفواتير والتأكيد والترحيل المحاسبي والمخزني" };
    private string EmptyTitle => ActiveEntityType switch { SalesEntityType.Prescriptions => "لا توجد وصفات", SalesEntityType.CustomerOrders => "لا توجد طلبات عملاء", _ => "لا توجد فواتير مبيعات" };
    private string EmptyText => "اضغط «جديد» لإنشاء أول مستند.";
    private string ListIcon => ActiveEntityType switch { SalesEntityType.Prescriptions => "fa-solid fa-glasses", SalesEntityType.CustomerOrders => "fa-solid fa-clipboard-list", _ => "fa-solid fa-file-invoice-dollar" };
    private IReadOnlyList<UiApplicationTabItem> TabItems => Workspace.Tabs.Select(x => new UiApplicationTabItem(x.TabId, x.Title, RouteFor(x.EntityType), x.IconCss(), x.TabId == Workspace.ActiveTabId, x.CanClose)).ToArray();

    protected override void OnInitialized() => Workspace.OnChange += WorkspaceChanged;

    protected override async Task OnParametersSetAsync()
    {
        if (_lastSection == Section) return;
        _lastSection = Section;
        if (Workspace.ActiveTab?.EntityType != Section)
            Workspace.OpenOrActivateListTab(Section);
        _pageNumber = 1;
        _search = null;
        await LoadListAsync(Section);
    }

    private void WorkspaceChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task LoadListAsync(SalesEntityType type)
    {
        _isLoading = true; _error = null; _listItems.Clear();
        try
        {
            var request = new PageRequest { PageNumber = _pageNumber, PageSize = 20, Search = _search };
            switch (type)
            {
                case SalesEntityType.Prescriptions:
                {
                    var page = await Sales.GetPrescriptionsPageAsync(request);
                    _totalCount = page.TotalCount; _totalPages = page.TotalPages;
                    _listItems.AddRange(page.Items.Select(x => { var current = x.Revisions.FirstOrDefault(r => r.IsCurrent); return new UiSalesDocumentListItem(x.Id, x.PrescriptionCode, x.CustomerName ?? x.CustomerCode ?? "عميل", x.PrescribedBy, SalesArabicPresenter.PrescriptionStatusText(x.Status), SalesArabicPresenter.StatusCss(x.Status.ToString()), DateText: x.PrescriptionDate.ToString("yyyy-MM-dd"), MetaText: current is not null ? $"Revision {current.RevisionNumber}" : null); }));
                    break;
                }
                case SalesEntityType.CustomerOrders:
                {
                    var page = await Sales.GetCustomerOrdersPageAsync(request);
                    _totalCount = page.TotalCount; _totalPages = page.TotalPages;
                    _listItems.AddRange(page.Items.Select(x => new UiSalesDocumentListItem(x.Id, x.OrderCode, x.CustomerName ?? x.CustomerCode ?? "عميل", x.CustomerCode, SalesArabicPresenter.OrderStatusText(x.Status), SalesArabicPresenter.StatusCss(x.Status.ToString()), $"{x.TotalAmount:N2} {x.CurrencyCodeSnapshot}", x.OrderDate.ToString("yyyy-MM-dd"), x.RequiredDate.HasValue ? $"مطلوب: {x.RequiredDate.Value:yyyy-MM-dd}" : null)));
                    break;
                }
                default:
                {
                    var page = await Sales.GetSalesInvoicesPageAsync(request);
                    _totalCount = page.TotalCount; _totalPages = page.TotalPages;
                    _listItems.AddRange(page.Items.Select(x => new UiSalesDocumentListItem(x.Id, x.InvoiceCode, x.CustomerName ?? x.CustomerCode ?? "عميل", x.CustomerCode, SalesArabicPresenter.InvoiceStatusText(x.Status), SalesArabicPresenter.StatusCss(x.Status.ToString()), $"{x.TotalAmount:N2} {x.CurrencyCodeSnapshot}", x.InvoiceDate.ToString("yyyy-MM-dd"), x.PaymentSummary.OutstandingAmount > 0 ? $"متبقي {x.PaymentSummary.OutstandingAmount:N2}" : "مسددة")));
                    break;
                }
            }
        }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
        finally { _isLoading = false; await InvokeAsync(StateHasChanged); }
    }

    private async Task NewAsync(MouseEventArgs _)
    {
        ClearMessages();
        var type = ActiveEntityType;
        var tab = Workspace.OpenNewEntityTab(type);
        tab.IsLoading = true;
        try
        {
            switch (type)
            {
                case SalesEntityType.Prescriptions:
                    var rx = await Sales.ReservePrescriptionCodeAsync();
                    tab.Model = new UiPrescriptionFormModel
                    {
                        PrescriptionCode = rx?.Code ?? string.Empty,
                        Status = PrescriptionStatus.Draft.ToString(),
                        StatusText = "جديدة - ستصبح مفعلة بعد الحفظ"
                    };
                    _revisionDrafts[tab.TabId] = CreateRevisionDraft(1);
                    break;
                case SalesEntityType.CustomerOrders:
                    var orderCode = await Sales.ReserveCustomerOrderCodeAsync(DateOnly.FromDateTime(DateTime.Today));
                    tab.Model = new UiCustomerOrderFormModel { OrderCode = orderCode?.Code ?? string.Empty };
                    break;
                default:
                    var invoiceCode = await Sales.ReserveSalesInvoiceCodeAsync(DateOnly.FromDateTime(DateTime.Today));
                    tab.Model = new UiSalesInvoiceFormModel { InvoiceCode = invoiceCode?.Code ?? string.Empty };
                    break;
            }
            tab.IsInitialized = true;
        }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); Workspace.RemoveTab(tab.TabId); }
        finally { tab.IsLoading = false; Workspace.NotifyStateChanged(); }
    }

    private Task RefreshAsync(MouseEventArgs _) => ActiveTab?.IsListTab == true ? LoadListAsync(ActiveEntityType) : ReloadActiveEntityAsync();
    private Task SearchValueChanged(string? value) { _search = value; return Task.CompletedTask; }
    private async Task SearchAsync(string? value) { _search = value; _pageNumber = 1; await LoadListAsync(ActiveEntityType); }
    private async Task PreviousPageAsync() { if (_pageNumber > 1) { _pageNumber--; await LoadListAsync(ActiveEntityType); } }
    private async Task NextPageAsync() { if (_pageNumber < _totalPages) { _pageNumber++; await LoadListAsync(ActiveEntityType); } }

    private async Task OpenEntityAsync(Guid id)
    {
        ClearMessages();
        var type = ActiveEntityType;
        try
        {
            switch (type)
            {
                case SalesEntityType.Prescriptions:
                    var prescription = await Sales.GetPrescriptionByIdAsync(id); if (prescription is null) return;
                    var rxTab = Workspace.OpenOrActivateEntityTab(type, id, prescription.PrescriptionCode); rxTab.CompleteSave(prescription.Id, prescription.PrescriptionCode, SalesUiMapper.ToUi(prescription));
                    break;
                case SalesEntityType.CustomerOrders:
                    var order = await Sales.GetCustomerOrderByIdAsync(id); if (order is null) return;
                    var orderTab = Workspace.OpenOrActivateEntityTab(type, id, order.OrderCode); orderTab.CompleteSave(order.Id, order.OrderCode, SalesUiMapper.ToUi(order));
                    break;
                default:
                    var invoice = await Sales.GetSalesInvoiceByIdAsync(id); if (invoice is null) return;
                    var invoiceTab = Workspace.OpenOrActivateEntityTab(type, id, invoice.InvoiceCode); invoiceTab.CompleteSave(invoice.Id, invoice.InvoiceCode, SalesUiMapper.ToUi(invoice));
                    break;
            }
            Workspace.NotifyStateChanged();
        }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
    }

    private async Task SaveAsync(MouseEventArgs _)
    {
        if (ActiveTab is null || ActiveTab.Model is null) return;
        ClearMessages(); ActiveTab.IsSaving = true;
        try
        {
            switch (ActiveTab.Model)
            {
                case UiPrescriptionFormModel model:
                {
                    RequireGuid(model.CustomerId, "يجب تحديد العميل.");

                    PrescriptionDto? prescription;

                    if (model.Id.HasValue)
                    {
                        prescription = await Sales.UpdatePrescriptionAsync(
                            model.Id.Value,
                            SalesUiMapper.ToUpdate(model));
                    }
                    else
                    {
                        var initialRevision = ActiveRevisionDraft
                            ?? throw new InvalidOperationException(
                                "لا يمكن حفظ الوصفة بدون الإصدار الأول.");

                        ValidateRevisionDraft(initialRevision);

                        prescription = await Sales.CreatePrescriptionAsync(
                            SalesUiMapper.ToCreate(model, initialRevision));
                    }

                    if (prescription is not null)
                    {
                        _revisionDrafts.Remove(ActiveTab.TabId);
                        _dirtyRevisionDraftTabs.Remove(ActiveTab.TabId);
                        CompleteSave(
                            ActiveTab,
                            prescription.Id,
                            prescription.PrescriptionCode,
                            SalesUiMapper.ToUi(prescription));
                    }

                    break;
                }
                case UiCustomerOrderFormModel model:
                    RequireGuid(model.CustomerId, "يجب تحديد العميل.");
                    RequireGuid(model.CurrencyId, "يجب تحديد العملة.");

                    if (!model.OrderDate.HasValue)
                        throw new InvalidOperationException("يجب تحديد تاريخ الطلب.");

                    if (model.RequiredDate.HasValue && model.RequiredDate.Value < model.OrderDate.Value)
                        throw new InvalidOperationException("تاريخ التسليم المطلوب لا يمكن أن يكون قبل تاريخ الطلب.");

                    if (model.Lines.Count == 0)
                        throw new InvalidOperationException("يجب إضافة سطر واحد على الأقل.");

                    var o = model.Id.HasValue
                        ? await Sales.UpdateCustomerOrderAsync(model.Id.Value, SalesUiMapper.ToUpdate(model))
                        : await Sales.CreateCustomerOrderAsync(SalesUiMapper.ToCreate(model));

                    if (o is not null)
                        CompleteSave(ActiveTab, o.Id, o.OrderCode, SalesUiMapper.ToUi(o));
                    break;
                case UiSalesInvoiceFormModel model:
                    if (!model.Id.HasValue && !string.IsNullOrWhiteSpace(model.CustomerOrderId))
                        throw new InvalidOperationException("طلبات العملاء تُفوتر من شاشة بيع جديد / Checkout. الفاتورة اليدوية مخصصة للبيع المباشر فقط.");

                    RequireGuid(model.CustomerId, "يجب تحديد العميل.");
                    RequireGuid(model.CurrencyId, "يجب تحديد العملة.");
                    model.PaymentTermType = string.Equals(model.PaymentPlan, "AccountCredit", StringComparison.OrdinalIgnoreCase) ? "Credit" : "Immediate";
                    if (!ValidateCreditSale(model.CustomerId, model.PaymentTermType))
                        return;
                    if (model.Lines.Count == 0) throw new InvalidOperationException("يجب إضافة سطر واحد على الأقل.");
                    var i = model.Id.HasValue
                        ? await Sales.UpdateSalesInvoiceAsync(model.Id.Value, SalesUiMapper.ToUpdate(model))
                        : await Sales.CreateSalesInvoiceAsync(SalesUiMapper.ToCreate(model));
                    if (i is not null) CompleteSave(ActiveTab, i.Id, i.InvoiceCode, SalesUiMapper.ToUi(i));
                    break;
            }
            _success = "تم حفظ البيانات بنجاح.";
            await LoadListAsync(ActiveTab.EntityType);
        }
        catch (InvalidOperationException ex) { _error = ex.Message; }
        catch (FormatException) { _error = "توجد قيمة مرجعية غير صالحة. أعد اختيار العميل/العملة/المخزن من قائمة البحث."; }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
        finally { ActiveTab.IsSaving = false; Workspace.NotifyStateChanged(); }
    }

    private static void CompleteSave(SalesWorkspaceTabState tab, Guid id, string title, object model) => tab.CompleteSave(id, title, model);
    private static void RequireGuid(string value, string message) { if (!Guid.TryParse(value, out var id) || id == Guid.Empty) throw new InvalidOperationException(message); }
    private Task MarkDirtyAsync() { if (ActiveTab is not null) { ActiveTab.IsDirty = true; Workspace.NotifyStateChanged(); } return Task.CompletedTask; }
    private Task BeginEditAsync(MouseEventArgs _) { ActiveTab?.BeginEdit(); ClearMessages(); Workspace.NotifyStateChanged(); return Task.CompletedTask; }

    private async Task CancelEditAsync(MouseEventArgs _)
    {
        if (ActiveTab is null) return;
        if (ActiveTab.IsDirty && !await Dialogs.ConfirmAsync("إلغاء التعديلات", "ستفقد التعديلات غير المحفوظة. هل تريد المتابعة؟", AlertTone.Warning, "متابعة", "رجوع")) return;
        await ReloadActiveEntityAsync();
    }

    private async Task ReloadActiveEntityAsync()
    {
        if (ActiveTab is null || !ActiveTab.EntityId.HasValue) return;
        var id = ActiveTab.EntityId.Value;
        try
        {
            switch (ActiveTab.EntityType)
            {
                case SalesEntityType.Prescriptions: var p = await Sales.GetPrescriptionByIdAsync(id); if (p is not null) ActiveTab.Model = SalesUiMapper.ToUi(p); break;
                case SalesEntityType.CustomerOrders: var o = await Sales.GetCustomerOrderByIdAsync(id); if (o is not null) ActiveTab.Model = SalesUiMapper.ToUi(o); break;
                case SalesEntityType.SalesInvoices: var i = await Sales.GetSalesInvoiceByIdAsync(id); if (i is not null) ActiveTab.Model = SalesUiMapper.ToUi(i); break;
            }
            ActiveTab.CancelEdit(); ActiveTab.IsInitialized = true; ClearMessages(); Workspace.NotifyStateChanged();
        }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
    }

    private Task OpenCheckoutAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiCustomerOrderFormModel order || !order.Id.HasValue)
            return Task.CompletedTask;
        if (!EnsureSaved(ActiveTab))
            return Task.CompletedTask;

        Navigation.NavigateTo($"/sales/checkout?orderId={order.Id.Value:D}");
        return Task.CompletedTask;
    }

    private async Task CancelOrderAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiCustomerOrderFormModel m || !m.Id.HasValue) return;
        if (!await Dialogs.ConfirmAsync("إلغاء الطلب", "سيتم تحرير الحجوزات النشطة المرتبطة بالطلب. هل تريد المتابعة؟", AlertTone.Warning, "إلغاء الطلب", "رجوع")) return;
        try { var dto = await Sales.CancelCustomerOrderAsync(m.Id.Value, new CancelCustomerOrderRequest(m.RowVersion)); if (dto is not null) ActiveTab.Model = SalesUiMapper.ToUi(dto); _success = "تم إلغاء الطلب."; Workspace.NotifyStateChanged(); await LoadListAsync(SalesEntityType.CustomerOrders); }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
    }

    private async Task ConfirmInvoiceAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiSalesInvoiceFormModel m || !m.Id.HasValue || !EnsureSaved(ActiveTab))
            return;

        ClearMessages();
        try
        {
            // Expected credit-limit failures are checked without throwing a business exception.
            // The POST confirm request is sent only when the invoice is currently eligible.
            var validation = await Sales.PreValidateSalesInvoiceConfirmationAsync(m.Id.Value);
            if (validation is null)
            {
                _error = "تعذر فحص جاهزية تأكيد الفاتورة.";
                Workspace.NotifyStateChanged();
                return;
            }

            if (!validation.CanConfirm)
            {
                var issue = validation.Issues.FirstOrDefault();
                _error = issue is null
                    ? "الفاتورة غير جاهزة للتأكيد."
                    : SalesApiErrorPresenter.GetMessage(
                        new OAS.Contracts.Common.Errors.ApiError
                        {
                            Code = issue.Code,
                            Message = issue.Message,
                            Status = 409
                        },
                        issue.Message);
                Workspace.NotifyStateChanged();
                return;
            }

            var dto = await Sales.ConfirmSalesInvoiceAsync(
                m.Id.Value,
                new ConfirmSalesInvoiceRequest(m.RowVersion));

            if (dto is not null)
                ActiveTab.Model = SalesUiMapper.ToUi(dto);

            _success = "تم تأكيد الفاتورة. أصبحت الحقول المالية الحرجة للقراءة فقط.";
            Workspace.NotifyStateChanged();
            await LoadListAsync(SalesEntityType.SalesInvoices);
        }
        catch (ApiClientException ex)
        {
            // Backend validation remains the final guard for concurrency and external API callers.
            _error = SalesApiErrorPresenter.GetMessage(ex.Error);
            Workspace.NotifyStateChanged();
        }
    }

    private async Task CancelInvoiceAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiSalesInvoiceFormModel m || !m.Id.HasValue) return;
        if (!await Dialogs.ConfirmAsync("إلغاء الفاتورة", "سيتم إلغاء الفاتورة قبل الترحيل وتحرير الحجوزات النشطة. هل تريد المتابعة؟", AlertTone.Warning, "إلغاء الفاتورة", "رجوع")) return;
        try { var dto = await Sales.CancelSalesInvoiceAsync(m.Id.Value, new CancelSalesInvoiceRequest(m.RowVersion)); if (dto is not null) ActiveTab.Model = SalesUiMapper.ToUi(dto); _success = "تم إلغاء الفاتورة."; Workspace.NotifyStateChanged(); await LoadListAsync(SalesEntityType.SalesInvoices); }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
    }

    private async Task PostInvoiceAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiSalesInvoiceFormModel m || !m.Id.HasValue || !EnsureSaved(ActiveTab)) return;
        ClearMessages();
        try
        {
            var validation = await Sales.PreValidateSalesInvoicePostingAsync(m.Id.Value);
            if (validation is null) { _error = "تعذر فحص جاهزية الترحيل."; return; }
            m.PostingIssues = validation.Issues.Select(x => SalesApiErrorPresenter.GetMessage(new OAS.Contracts.Common.Errors.ApiError { Code = x.Code, Message = x.Message, Status = 409 }, x.Message)).ToList();
            if (!validation.CanPost) { _error = m.PostingIssues.FirstOrDefault() ?? "الفاتورة غير جاهزة للترحيل."; Workspace.NotifyStateChanged(); return; }

            var paymentLines = BuildDirectInvoicePayments(m);
            var confirmationText = paymentLines.Count > 0
                ? "سيتم ترحيل فاتورة البيع المباشر وإنشاء الأثر المخزني والقيد المحاسبي، ثم إنشاء سند قبض واحد بخطوط الدفع المدخلة وتخصيصه على الفاتورة. هل تريد المتابعة؟"
                : "سيتم ترحيل فاتورة البيع المباشر وإنشاء أثر مخزني وقيد محاسبي رسمي بدون تحصيل الآن. هل تريد المتابعة؟";

            if (!await Dialogs.ConfirmAsync("ترحيل فاتورة المبيعات", confirmationText, AlertTone.Warning, "ترحيل", "رجوع")) return;
            var postingResult = await Sales.PostSalesInvoiceAsync(
                m.Id.Value,
                new PostSalesInvoiceRequest(m.RowVersion) { PaymentLines = paymentLines });
            var dto = await Sales.GetSalesInvoiceByIdAsync(m.Id.Value);
            if (dto is not null) ActiveTab.Model = SalesUiMapper.ToUi(dto);
            _success = !string.IsNullOrWhiteSpace(postingResult?.ReceiptVoucherNumber)
                ? $"تم ترحيل الفاتورة وإنشاء سند القبض {postingResult.ReceiptVoucherNumber} وتخصيص الدفعات."
                : "تم ترحيل الفاتورة بنجاح.";
            Workspace.NotifyStateChanged(); await LoadListAsync(SalesEntityType.SalesInvoices);
        }
        catch (InvalidOperationException ex) { _error = ex.Message; Workspace.NotifyStateChanged(); }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
    }

    private static IReadOnlyList<CheckoutPaymentLineRequest> BuildDirectInvoicePayments(UiSalesInvoiceFormModel invoice)
    {
        var result = new List<CheckoutPaymentLineRequest>();
        foreach (var line in invoice.PaymentLines.Where(x => x.Amount > 0m))
        {
            if (!Enum.TryParse<PaymentMethod>(line.PaymentMethod, true, out var method))
                throw new InvalidOperationException("طريقة دفع غير صحيحة.");
            if (!Guid.TryParse(line.CurrencyId, out var currencyId))
                throw new InvalidOperationException("يجب تحديد عملة كل دفعة.");
            result.Add(new CheckoutPaymentLineRequest(
                method, currencyId, line.Amount, TryGuid(line.CashAccountId), TryGuid(line.BankAccountId),
                line.ReferenceNumber, line.ReferenceDate, line.Description)
            { SettlementAccountId = TryGuid(line.SettlementAccountId) });
        }
        return result;
    }

    private async Task CancelPrescriptionAsync(MouseEventArgs _)
    {
        if (!await Dialogs.ConfirmAsync(
                "إلغاء الوصفة",
                "هل تريد تغيير حالة الوصفة إلى ملغاة؟",
                AlertTone.Warning,
                "إلغاء الوصفة",
                "رجوع"))
        {
            return;
        }

        await SetPrescriptionStatusAsync(PrescriptionStatus.Cancelled);
    }

    private async Task SetPrescriptionStatusAsync(PrescriptionStatus status)
    {
        if (ActiveTab?.Model is not UiPrescriptionFormModel model ||
            !model.Id.HasValue ||
            !EnsureSaved(ActiveTab))
        {
            return;
        }

        try
        {
            var dto = await Sales.SetPrescriptionStatusAsync(
                model.Id.Value,
                new SetPrescriptionStatusRequest(status, model.RowVersion));

            if (dto is not null)
                ActiveTab.Model = SalesUiMapper.ToUi(dto);

            Workspace.NotifyStateChanged();
            await LoadListAsync(SalesEntityType.Prescriptions);
        }
        catch (ApiClientException ex)
        {
            _error = SalesApiErrorPresenter.GetMessage(ex.Error);
        }
    }

    private Task BeginRevisionAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiPrescriptionFormModel model ||
            !model.Id.HasValue)
        {
            return Task.CompletedTask;
        }

        ClearMessages();

        var nextRevisionNumber = model.Revisions.Count == 0
            ? 1
            : model.Revisions.Max(x => x.RevisionNumber) + 1;

        _revisionDrafts[ActiveTab.TabId] = CreateRevisionDraft(nextRevisionNumber);
        _dirtyRevisionDraftTabs.Remove(ActiveTab.TabId);
        Workspace.NotifyStateChanged();
        return Task.CompletedTask;
    }

    private async Task CancelRevisionAsync(MouseEventArgs _)
    {
        if (ActiveTab is null)
            return;

        if (_dirtyRevisionDraftTabs.Contains(ActiveTab.TabId))
        {
            var confirmed = await Dialogs.ConfirmAsync(
                "إلغاء الإصدار الجديد",
                "توجد قياسات غير محفوظة في الإصدار الجديد. هل تريد إلغاءها؟",
                AlertTone.Warning,
                "إلغاء الإصدار",
                "رجوع");

            if (!confirmed)
                return;
        }

        _revisionDrafts.Remove(ActiveTab.TabId);
        _dirtyRevisionDraftTabs.Remove(ActiveTab.TabId);
        ClearMessages();
        Workspace.NotifyStateChanged();
    }

    private Task RevisionDraftChangedAsync()
    {
        if (ActiveTab is null)
            return Task.CompletedTask;

        // في الوصفة الجديدة الإصدار الأول جزء من عملية إنشاء الوصفة نفسها.
        if (ActiveTab.IsNew)
            return MarkDirtyAsync();

        // في الوصفة الموجودة نحفظ الإصدار بعملية مستقلة، فلا نوسّخ بيانات الوصفة الرئيسية.
        _dirtyRevisionDraftTabs.Add(ActiveTab.TabId);
        Workspace.NotifyStateChanged();
        return Task.CompletedTask;
    }

    private async Task SaveRevisionAsync(MouseEventArgs _)
    {
        if (_isSavingRevision ||
            ActiveTab?.Model is not UiPrescriptionFormModel prescription ||
            !prescription.Id.HasValue ||
            ActiveRevisionDraft is not { } revisionDraft)
        {
            return;
        }

        ClearMessages();
        _isSavingRevision = true;
        Workspace.NotifyStateChanged();

        try
        {
            ValidateRevisionDraft(revisionDraft);

            var dto = await Sales.CreatePrescriptionRevisionAsync(
                prescription.Id.Value,
                SalesUiMapper.ToRevisionRequest(
                    revisionDraft,
                    prescription.RowVersion));

            if (dto is not null)
                ActiveTab.Model = SalesUiMapper.ToUi(dto);

            _revisionDrafts.Remove(ActiveTab.TabId);
            _dirtyRevisionDraftTabs.Remove(ActiveTab.TabId);
            _success = "تم حفظ الإصدار الجديد وأصبح هو الإصدار الحالي للوصفة.";
            Workspace.NotifyStateChanged();
            await LoadListAsync(SalesEntityType.Prescriptions);
        }
        catch (InvalidOperationException ex)
        {
            _error = ex.Message;
        }
        catch (ApiClientException ex)
        {
            _error = SalesApiErrorPresenter.GetMessage(ex.Error);
        }
        finally
        {
            _isSavingRevision = false;
            Workspace.NotifyStateChanged();
        }
    }

    private static UiPrescriptionRevisionModel CreateRevisionDraft(int revisionNumber) => new()
    {
        RevisionNumber = revisionNumber,
        EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
        Eyes =
        [
            new UiPrescriptionEyeModel
            {
                Eye = EyeSide.RightOD.ToString(),
                EyeText = SalesArabicPresenter.EyeText(EyeSide.RightOD)
            },
            new UiPrescriptionEyeModel
            {
                Eye = EyeSide.LeftOS.ToString(),
                EyeText = SalesArabicPresenter.EyeText(EyeSide.LeftOS)
            }
        ]
    };

    private static void ValidateRevisionDraft(UiPrescriptionRevisionModel revision)
    {
        if (!revision.EffectiveDate.HasValue)
            throw new InvalidOperationException("يجب تحديد تاريخ سريان الإصدار.");

        if (revision.Eyes.Count == 0)
            throw new InvalidOperationException("يجب إضافة بيانات عين واحدة على الأقل.");

        if (revision.Eyes
            .GroupBy(x => x.Eye, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("لا يمكن تكرار نفس العين داخل الإصدار.");
        }

        if (revision.Eyes.Any(x => x.Axis is < 0 or > 180))
            throw new InvalidOperationException("قيمة Axis يجب أن تكون بين 0 و180.");

        if (revision.Eyes.Any(x => x.ADD < 0 || x.Prism < 0))
            throw new InvalidOperationException("قيم ADD وPrism لا يمكن أن تكون سالبة.");

        if (revision.Eyes.Any(x => x.PD is <= 0 || x.MonocularPD is <= 0 || x.FittingHeight is <= 0))
            throw new InvalidOperationException("قيم PD وMono PD وFitting Height يجب أن تكون أكبر من صفر عند إدخالها.");
    }

    private bool EnsureSaved(SalesWorkspaceTabState tab)
    {
        if (!tab.IsDirty) return true;
        _error = "توجد تعديلات غير محفوظة. احفظ المستند قبل تنفيذ الإجراء.";
        return false;
    }

    private async Task SelectTabAsync(Guid tabId)
    {
        var tab = Workspace.FindTab(tabId); if (tab is null) return;
        Workspace.ActiveTabId = tabId; Workspace.NotifyStateChanged();
        if (tab.EntityType != Section) Navigation.NavigateTo(RouteFor(tab.EntityType));
    }

    private async Task CloseTabAsync(Guid tabId)
    {
        var tab = Workspace.FindTab(tabId); if (tab is null || !tab.CanClose) return;
        var hasUnsavedRevision = _dirtyRevisionDraftTabs.Contains(tabId);
        if ((tab.IsDirty || hasUnsavedRevision) &&
            !await Dialogs.ConfirmAsync(
                "إغلاق التبويب",
                hasUnsavedRevision
                    ? "يوجد إصدار جديد غير محفوظ في هذا التبويب. هل تريد إغلاقه؟"
                    : "يوجد تغييرات غير محفوظة في هذا التبويب. هل تريد إغلاقه؟",
                AlertTone.Warning,
                "إغلاق",
                "رجوع"))
        {
            return;
        }

        _revisionDrafts.Remove(tabId);
        _dirtyRevisionDraftTabs.Remove(tabId);
        Workspace.RemoveTab(tabId);
    }

    private async Task CloseOtherTabsAsync(Guid keepId)
    {
        foreach (var tab in Workspace.Tabs.Where(x => x.TabId != keepId && x.CanClose).ToArray()) await CloseTabAsync(tab.TabId);
    }
    private async Task CloseAllTabsAsync() { foreach (var tab in Workspace.Tabs.Where(x => x.CanClose).ToArray()) await CloseTabAsync(tab.TabId); }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCustomerItemsAsync(string q, CancellationToken ct)
    {
        var rows = await Sales.SearchCustomersAsync(q, 20, ct); foreach (var x in rows) _customers[x.Id] = x;
        return rows.Select(x => new UiLookupItem(x.Id.ToString(), $"{x.CustomerCode} - {x.NameAr}", string.Join(" • ", new[] { x.AccountCode, x.Mobile }.Where(s => !string.IsNullOrWhiteSpace(s))), "fa-regular fa-user", !x.IsActive)).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchEmployeeItemsAsync(string q, CancellationToken ct)
    {
        var page = await Employees.GetPageAsync(new PageRequest { PageNumber = 1, PageSize = 50, Search = q }, ct);
        foreach (var employee in page.Items) _employees[employee.Id] = employee;
        return page.Items.Select(employee => new UiLookupItem(
            employee.Id.ToString(),
            $"{employee.EmployeeCode} - {employee.DisplayName}",
            string.Join(" • ", new[] { employee.JobTitleName, employee.IsCommissionEligible ? "مؤهل للعمولة" : null }.Where(value => !string.IsNullOrWhiteSpace(value))),
            "fa-solid fa-user-tie",
            !employee.IsActive)).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchPrescriptionRevisionItemsAsync(string q, CancellationToken ct)
    {
        Guid? customerId = ActiveTab?.Model switch
        {
            UiCustomerOrderFormModel o => TryGuid(o.CustomerId),
            UiSalesInvoiceFormModel i => TryGuid(i.CustomerId),
            _ => null
        };
        var rows = await Sales.SearchPrescriptionsAsync(customerId, q, 20, ct);
        var result = new List<UiLookupItem>();
        foreach (var x in rows.Where(x => x.CurrentRevisionId.HasValue))
        {
            var rev = new SalesPrescriptionRevisionLookupDto(x.CurrentRevisionId!.Value, x.Id, x.PrescriptionCode, x.CurrentRevisionNumber ?? 0, x.PrescriptionDate, true, x.IsActive);
            _revisions[rev.Id] = rev; result.Add(new UiLookupItem(rev.Id.ToString(), $"{x.PrescriptionCode} / Revision {rev.RevisionNumber}", x.PrescriptionDate.ToString("yyyy-MM-dd"), "fa-solid fa-glasses", !x.IsActive));
        }
        return result;
    }
    private Task<IReadOnlyList<UiLookupItem>> SearchLinePrescriptionItemsAsync(UiSalesLineModel _, string q, CancellationToken ct) => SearchPrescriptionRevisionItemsAsync(q, ct);
    private async Task<IReadOnlyList<UiLookupItem>> SearchCurrencyItemsAsync(
        string q,
        CancellationToken ct)
    {
        var date = ActiveTab?.Model switch
        {
            UiCustomerOrderFormModel order => order.OrderDate,
            UiSalesInvoiceFormModel invoice => invoice.InvoiceDate,
            _ => null
        } ?? DateOnly.FromDateTime(DateTime.Today);

        var rows = await Sales.SearchCurrenciesAsync(date, q, 20, ct);

        foreach (var currency in rows)
            _currencies[currency.Id] = currency;

        return rows.Select(currency =>
        {
            var disabled =
                !currency.IsActive ||
                !currency.HasEffectiveExchangeRate;

            string secondaryText;

            if (!currency.IsActive)
            {
                secondaryText = "العملة غير نشطة";
            }
            else if (!currency.HasEffectiveExchangeRate)
            {
                secondaryText =
                    currency.AvailabilityMessage
                    ?? $"لا يوجد سعر صرف فعال حتى {date:yyyy-MM-dd}";
            }
            else if (currency.IsBaseCurrency)
            {
                secondaryText =
                    string.IsNullOrWhiteSpace(currency.Symbol)
                        ? "العملة الأساسية • سعر الصرف: 1"
                        : $"{currency.Symbol} • العملة الأساسية • سعر الصرف: 1";
            }
            else
            {
                secondaryText =
                    string.IsNullOrWhiteSpace(currency.Symbol)
                        ? $"السعر: {currency.EffectiveExchangeRate:N4} • تاريخ السعر: {currency.EffectiveRateDate:yyyy-MM-dd}"
                        : $"{currency.Symbol} • السعر: {currency.EffectiveExchangeRate:N4} • تاريخ السعر: {currency.EffectiveRateDate:yyyy-MM-dd}";
            }

            return new UiLookupItem(
                currency.Id.ToString(),
                $"{currency.Code} - {currency.NameAr}",
                secondaryText,
                currency.HasEffectiveExchangeRate
                    ? "fa-solid fa-coins"
                    : "fa-solid fa-triangle-exclamation",
                disabled);
        }).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchProductTypeItemsAsync(string q, CancellationToken ct)
    {
        var rows = await Sales.SearchProductTypesAsync(q, 30, ct);
        foreach (var x in rows)
            _productTypes[x.Id] = x;

        return rows.Select(x => new UiLookupItem(
            x.Id.ToString(),
            $"{x.Code} - {x.NameAr}",
            ProductTypeHint(x),
            ProductTypeIcon(x.SalesLineType),
            !x.IsActive)).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCategoryItemsAsync(UiSalesLineModel line, string q, CancellationToken ct)
    {
        var productTypeId = TryGuid(line.ProductTypeId);
        if (!productTypeId.HasValue)
            return [];

        var rows = await Sales.SearchProductCategoriesAsync(productTypeId, q, 50, ct);
        foreach (var x in rows)
            _categories[x.Id] = x;

        return rows.Select(x => new UiLookupItem(
            x.Id.ToString(),
            $"{x.Code} - {x.NameAr}",
            null,
            "fa-solid fa-layer-group",
            !x.IsActive)).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCashAccountItemsAsync(string q, CancellationToken ct)
    {
        if (ActiveTab?.Model is not UiSalesInvoiceFormModel invoice || !Guid.TryParse(invoice.CurrencyId, out var currencyId))
            return [];
        var rows = await Sales.SearchCashAccountsAsync(currencyId, q, 20, ct);
        foreach (var x in rows) _cashAccounts[x.Id] = x;
        return rows.Select(x => new UiLookupItem(x.Id.ToString(), $"{x.Code} - {x.Name}", x.IsDefault ? "الصندوق الافتراضي" : null, "fa-solid fa-cash-register", !x.IsActive)).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchBankAccountItemsAsync(string q, CancellationToken ct)
    {
        if (ActiveTab?.Model is not UiSalesInvoiceFormModel invoice || !Guid.TryParse(invoice.CurrencyId, out var currencyId))
            return [];
        var rows = await Sales.SearchBankAccountsAsync(currencyId, q, 20, ct);
        foreach (var x in rows) _bankAccounts[x.Id] = x;
        return rows.Select(x => new UiLookupItem(x.Id.ToString(), $"{x.Code} - {x.BankName} - {x.AccountName}", x.AccountNumber, "fa-solid fa-building-columns", !x.IsActive)).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchProductItemsAsync(UiSalesLineModel line, string q, CancellationToken ct)
    {
        var productTypeId = TryGuid(line.ProductTypeId);
        var categoryId = TryGuid(line.ProductCategoryId);
        if (!productTypeId.HasValue || !categoryId.HasValue)
            return [];

        var rows = await Sales.SearchProductVariantsAsync(productTypeId, categoryId, q, 20, ct);
        foreach (var x in rows)
            _products[x.Id] = x;

        return rows.Select(x => new UiLookupItem(
            x.Id.ToString(),
            $"{x.ProductCode} - {x.ProductNameAr}",
            BuildProductLookupHint(x),
            ProductTypeIcon(x.SalesLineType),
            !x.IsActive)).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchWarehouseItemsAsync(UiSalesLineModel line, string q, CancellationToken ct)
    {
        Guid? variant = Guid.TryParse(line.ProductVariantId, out var id) ? id : null;
        var rows = await Sales.SearchWarehousesAsync(variant, q, 20, ct); foreach (var x in rows) _warehouses[x.Id] = x;
        return rows.Select(x => new UiLookupItem(x.Id.ToString(), $"{x.Code} - {x.NameAr}", x.AvailableQuantity.HasValue ? $"المتاح: {x.AvailableQuantity:N2}" : null, "fa-solid fa-warehouse", !x.IsActive)).ToArray();
    }
    private Task PrescriptionCustomerChangedAsync(string? value)
    {
        if (ActiveTab?.Model is UiPrescriptionFormModel prescription &&
            Guid.TryParse(value, out var customerId) &&
            _customers.TryGetValue(customerId, out var customer))
        {
            prescription.CustomerDisplay = $"{customer.CustomerCode} - {customer.NameAr}";
        }
        return Task.CompletedTask;
    }

    private async Task SelectionChangedAsync(UiSalesSelectionChange change)
    {
        if (ActiveTab?.Model is UiCustomerOrderFormModel order)
        {
            if (change.Field == "Customer" && Guid.TryParse(change.Value, out var cid) && _customers.TryGetValue(cid, out var c))
            {
                order.CustomerCode = c.CustomerCode;
                order.CustomerDisplay = $"{c.CustomerCode} - {c.NameAr}";
            }

            if (change.Field == "Currency" && Guid.TryParse(change.Value, out var curId) && _currencies.TryGetValue(curId, out var cur))
            {
                var previousRate = order.ExchangeRate;
                RepriceUiForCurrencyChange(
                    order.Lines,
                    previousRate,
                    cur.EffectiveExchangeRate,
                    cur.DecimalPlaces);
                order.Subtotal = ConvertUiCurrency(order.Subtotal, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                order.DiscountAmount = ConvertUiCurrency(order.DiscountAmount, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                order.TaxAmount = ConvertUiCurrency(order.TaxAmount, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                order.TotalAmount = ConvertUiCurrency(order.TotalAmount, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                order.CurrencyDisplay = $"{cur.Code} - {cur.NameAr}";
                order.CurrencyCode = cur.Code;
                order.CurrencyDecimalPlaces = cur.DecimalPlaces;
                order.ExchangeRate = cur.EffectiveExchangeRate;
            }

            if (change.Field == "PrescriptionRevision")
            {
                if (Guid.TryParse(change.Value, out var rid) && _revisions.TryGetValue(rid, out var rev))
                {
                    order.PrescriptionCode = rev.PrescriptionCode;
                    order.PrescriptionDisplay = $"{rev.PrescriptionCode} / إصدار {rev.RevisionNumber}";
                }
                else
                {
                    order.PrescriptionCode = string.Empty;
                    order.PrescriptionDisplay = null;
                }

                // عند تغيير وصفة رأس الطلب لا نحتفظ بمرجع Revision قديم داخل أسطر العدسات.
                // الـApplication سيربط Revision الرأس فقط بالأسطر التي تحتاج وصفة فعليًا.
                foreach (var line in order.Lines.Where(x => x.PrescriptionRequired))
                {
                    line.PrescriptionRevisionId = string.Empty;
                    line.PrescriptionRevisionDisplay = null;
                }
            }
        }
        else if (ActiveTab?.Model is UiSalesInvoiceFormModel invoice)
        {
            if (change.Field == "Customer" && Guid.TryParse(change.Value, out var cid) && _customers.TryGetValue(cid, out var c))
            {
                invoice.CustomerCode = c.CustomerCode;
                invoice.CustomerDisplay = $"{c.CustomerCode} - {c.NameAr}";
                if (invoice.PaymentPlan == "AccountCredit" && !ValidateCreditSale(invoice.CustomerId, "Credit"))
                {
                    invoice.PaymentPlan = "FullNow";
                    invoice.PaymentTermType = "Immediate";
                }
            }

            if (change.Field == "SalesEmployee")
            {
                if (Guid.TryParse(change.Value, out var employeeId) && _employees.TryGetValue(employeeId, out var employee))
                    invoice.SalesEmployeeDisplay = $"{employee.EmployeeCode} - {employee.DisplayName}";
                else
                    invoice.SalesEmployeeDisplay = null;
            }

            if (change.Field == "PaymentPlan")
            {
                invoice.PaymentTermType = invoice.PaymentPlan == "AccountCredit" ? "Credit" : "Immediate";
                if (invoice.PaymentPlan == "AccountCredit" && !ValidateCreditSale(invoice.CustomerId, "Credit"))
                {
                    invoice.PaymentPlan = "FullNow";
                    invoice.PaymentTermType = "Immediate";
                }
            }

            if (change.Field == "Currency" && Guid.TryParse(change.Value, out var curId) && _currencies.TryGetValue(curId, out var cur))
            {
                var previousRate = invoice.ExchangeRate;
                RepriceUiForCurrencyChange(
                    invoice.Lines,
                    previousRate,
                    cur.EffectiveExchangeRate,
                    cur.DecimalPlaces);
                invoice.Subtotal = ConvertUiCurrency(invoice.Subtotal, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                invoice.DiscountAmount = ConvertUiCurrency(invoice.DiscountAmount, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                invoice.TaxAmount = ConvertUiCurrency(invoice.TaxAmount, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                invoice.TotalAmount = ConvertUiCurrency(invoice.TotalAmount, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                invoice.PaidAmount = ConvertUiCurrency(invoice.PaidAmount, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                invoice.OutstandingAmount = ConvertUiCurrency(invoice.OutstandingAmount, previousRate, cur.EffectiveExchangeRate, cur.DecimalPlaces);
                invoice.CurrencyDisplay = $"{cur.Code} - {cur.NameAr}";
                invoice.CurrencyCode = cur.Code;
                invoice.CurrencyDecimalPlaces = cur.DecimalPlaces;
                invoice.ExchangeRate = cur.EffectiveExchangeRate;
            }

            if (change.Field == "PrescriptionRevision")
            {
                if (Guid.TryParse(change.Value, out var rid) && _revisions.TryGetValue(rid, out var rev))
                {
                    invoice.PrescriptionCode = rev.PrescriptionCode;
                    invoice.PrescriptionDisplay = $"{rev.PrescriptionCode} / إصدار {rev.RevisionNumber}";
                }
                else
                {
                    invoice.PrescriptionCode = string.Empty;
                    invoice.PrescriptionDisplay = null;
                }
            }
        }

        await MarkDirtyAsync();
    }

    private bool ValidateCreditSale(string? customerIdValue, string? paymentTermType)
    {
        if (!string.Equals(paymentTermType, "Credit", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!Guid.TryParse(customerIdValue, out var customerId) ||
            !_customers.TryGetValue(customerId, out var customer))
        {
            // يبقى التحقق النهائي في الـApplication إذا لم تكن بيانات العميل محملة في الـClient.
            return true;
        }

        if (customer.IsCreditAllowed)
            return true;

        _error = "البيع الآجل غير مسموح لهذا العميل. اختر السداد الفوري.";
        _success = null;
        Workspace.NotifyStateChanged();
        return false;
    }

    private Task ProductTypeSelectedAsync(UiSalesLineSelectionChange change)
    {
        if (Guid.TryParse(change.Value, out var id) && _productTypes.TryGetValue(id, out var productType))
        {
            change.Line.ProductTypeDisplay = $"{productType.Code} - {productType.NameAr}";
            change.Line.ProductTypeSystemKey = productType.SystemKey;
            change.Line.LineType = productType.SalesLineType.ToString();
        }
        else
        {
            change.Line.ProductTypeDisplay = null;
            change.Line.ProductTypeSystemKey = null;
            change.Line.LineType = SalesLineType.Other.ToString();
        }

        return Task.CompletedTask;
    }

    private Task CategorySelectedAsync(UiSalesLineSelectionChange change)
    {
        if (Guid.TryParse(change.Value, out var id) && _categories.TryGetValue(id, out var category))
            change.Line.ProductCategoryDisplay = $"{category.Code} - {category.NameAr}";
        else
            change.Line.ProductCategoryDisplay = null;

        return Task.CompletedTask;
    }

    private Task ProductSelectedAsync(UiSalesLineSelectionChange change)
    {
        if (Guid.TryParse(change.Value, out var id) && _products.TryGetValue(id, out var product))
        {
            change.Line.ProductTypeId = product.ProductTypeId.ToString();
            change.Line.ProductTypeDisplay = $"{product.ProductTypeCode} - {product.ProductTypeNameAr}";
            change.Line.ProductTypeSystemKey = product.ProductTypeSystemKey;
            change.Line.LineType = product.SalesLineType.ToString();
            change.Line.ProductIsStockItem = product.IsStockItem;

            change.Line.ProductCategoryId = product.CategoryId.ToString();
            if (_categories.TryGetValue(product.CategoryId, out var category))
                change.Line.ProductCategoryDisplay = $"{category.Code} - {category.NameAr}";

            change.Line.ProductDisplay = string.IsNullOrWhiteSpace(product.VariantName)
                ? product.ProductNameAr
                : $"{product.ProductNameAr} - {product.VariantName}";

            change.Line.ProductCodeSnapshot = product.ProductCode;
            change.Line.ProductNameSnapshot = change.Line.ProductDisplay;
            change.Line.UnitSnapshot = product.UnitName;
            change.Line.Description = change.Line.ProductDisplay;
            var (exchangeRate, currencyDecimals) = ActiveCurrencyContext();
            var transactionPrice = ConvertFromBaseForUi(
                product.SellingPrice,
                exchangeRate,
                currencyDecimals);
            change.Line.BaseUnitPrice = transactionPrice;
            change.Line.ActualUnitPrice = transactionPrice;
            change.Line.PrescriptionRequired = product.IsPrescriptionLens;

            if (change.Line.LineType == SalesLineType.Lens.ToString())
            {
                if (product.IsPrescriptionLens && ActiveTab?.Model is UiCustomerOrderFormModel)
                {
                    // في طلب العميل نستخدم وصفة الرأس، ويحدد المستخدم العين على مستوى البند.
                    change.Line.PrescriptionRevisionId = string.Empty;
                    change.Line.PrescriptionRevisionDisplay = null;
                }
                else if (!product.IsPrescriptionLens)
                {
                    change.Line.PrescriptionRevisionId = string.Empty;
                    change.Line.PrescriptionRevisionDisplay = null;
                    change.Line.PrescriptionEye = string.Empty;
                }
            }
            else
            {
                change.Line.PrescriptionRequired = false;
                change.Line.PrescriptionRevisionId = string.Empty;
                change.Line.PrescriptionRevisionDisplay = null;
                change.Line.PrescriptionEye = string.Empty;
            }
        }
        else
        {
            change.Line.ProductDisplay = null;
            change.Line.ProductCodeSnapshot = null;
            change.Line.ProductNameSnapshot = null;
            change.Line.UnitSnapshot = null;
            change.Line.ProductIsStockItem = null;
            change.Line.PrescriptionRequired = false;
        }

        return Task.CompletedTask;
    }

    private static string BuildProductLookupHint(SalesProductVariantLookupDto product)
    {
        var parts = new List<string>();
        foreach (var value in new[] { product.SKU, product.Barcode, product.VariantName })
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add(value);
        }

        if (product.IsPrescriptionLens)
        {
            if (product.SphereMin.HasValue && product.SphereMax.HasValue)
                parts.Add($"SPH {product.SphereMin.Value:0.##}..{product.SphereMax.Value:0.##}");
            if (product.CylinderMin.HasValue && product.CylinderMax.HasValue)
                parts.Add($"CYL {product.CylinderMin.Value:0.##}..{product.CylinderMax.Value:0.##}");
            if (product.AddMin.HasValue && product.AddMax.HasValue)
                parts.Add($"ADD {product.AddMin.Value:0.##}..{product.AddMax.Value:0.##}");
        }

        return string.Join(" • ", parts);
    }

    private static string ProductTypeHint(SalesProductTypeLookupDto productType) => productType.SalesLineType switch
    {
        SalesLineType.Frame when string.Equals(productType.SystemKey, "SUNGLASSES", StringComparison.OrdinalIgnoreCase) => "نظارة شمسية • تعامل كسطر إطار",
        SalesLineType.Frame => "إطار",
        SalesLineType.Lens => "عدسة",
        SalesLineType.Accessory => "إكسسوار",
        SalesLineType.Service => "خدمة / غير مخزني عادةً",
        _ => "نوع آخر"
    };

    private static string ProductTypeIcon(SalesLineType lineType) => lineType switch
    {
        SalesLineType.Frame => "fa-solid fa-glasses",
        SalesLineType.Lens => "fa-solid fa-circle-dot",
        SalesLineType.Accessory => "fa-solid fa-box-open",
        SalesLineType.Service => "fa-solid fa-screwdriver-wrench",
        _ => "fa-solid fa-box"
    };

    private (decimal ExchangeRate, byte DecimalPlaces) ActiveCurrencyContext() =>
        ActiveTab?.Model switch
        {
            UiCustomerOrderFormModel order => (NormalizeRate(order.ExchangeRate), order.CurrencyDecimalPlaces),
            UiSalesInvoiceFormModel invoice => (NormalizeRate(invoice.ExchangeRate), invoice.CurrencyDecimalPlaces),
            _ => (1m, 2)
        };

    private static decimal NormalizeRate(decimal rate) => rate > 0m ? rate : 1m;

    private static decimal ConvertFromBaseForUi(decimal baseAmount, decimal exchangeRate, byte decimalPlaces)
    {
        var rate = NormalizeRate(exchangeRate);
        return Math.Round(baseAmount / rate, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    private static decimal ConvertUiCurrency(
        decimal amount,
        decimal fromExchangeRate,
        decimal toExchangeRate,
        byte decimalPlaces)
    {
        var fromRate = NormalizeRate(fromExchangeRate);
        var toRate = NormalizeRate(toExchangeRate);
        return Math.Round(amount * fromRate / toRate, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    private static void RepriceUiForCurrencyChange(
        IEnumerable<UiSalesLineModel> lines,
        decimal fromExchangeRate,
        decimal toExchangeRate,
        byte decimalPlaces)
    {
        foreach (var line in lines)
        {
            line.BaseUnitPrice = ConvertUiCurrency(line.BaseUnitPrice, fromExchangeRate, toExchangeRate, decimalPlaces);
            line.ActualUnitPrice = ConvertUiCurrency(line.ActualUnitPrice, fromExchangeRate, toExchangeRate, decimalPlaces);

            if (string.Equals(line.DiscountType, "FixedAmount", StringComparison.OrdinalIgnoreCase) &&
                line.DiscountValue.HasValue)
            {
                line.DiscountValue = ConvertUiCurrency(
                    line.DiscountValue.Value,
                    fromExchangeRate,
                    toExchangeRate,
                    decimalPlaces);
            }

            line.DiscountAmount = ConvertUiCurrency(line.DiscountAmount, fromExchangeRate, toExchangeRate, decimalPlaces);
            line.TaxAmount = ConvertUiCurrency(line.TaxAmount, fromExchangeRate, toExchangeRate, decimalPlaces);
            line.NetAmount = ConvertUiCurrency(line.NetAmount, fromExchangeRate, toExchangeRate, decimalPlaces);
            line.FinalAmount = ConvertUiCurrency(line.FinalAmount, fromExchangeRate, toExchangeRate, decimalPlaces);
        }
    }

    private Task WarehouseSelectedAsync(UiSalesLineSelectionChange change) { if (Guid.TryParse(change.Value, out var id) && _warehouses.TryGetValue(id, out var w)) change.Line.WarehouseDisplay = $"{w.Code} - {w.NameAr}"; return Task.CompletedTask; }

    private async Task RequestPriceOverrideAsync(UiSalesLineModel line)
    {
        if (ActiveTab?.Model is not UiSalesInvoiceFormModel invoice || !invoice.Id.HasValue || !line.Id.HasValue) return;
        var result = await Dialogs.ShowAsync<UiSalesPriceOverrideDialog>(
            "طلب اعتماد تغيير السعر",
            new Dictionary<string, object>
            {
                [nameof(UiSalesPriceOverrideDialog.OriginalPrice)] = line.BaseUnitPrice,
                [nameof(UiSalesPriceOverrideDialog.OverridePrice)] = line.ActualUnitPrice
            });
        if (result.Cancelled || result.Value is not UiSalesPriceOverrideRequestModel request) return;
        try
        {
            var created = await Sales.RequestPriceOverrideAsync(
                invoice.Id.Value,
                new RequestSalesPriceOverrideRequest(line.Id.Value, request.OverridePrice, request.Reason, invoice.RowVersion));
            if (created is not null) _success = "تم إرسال طلب اعتماد السعر بنجاح.";
        }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
        Workspace.NotifyStateChanged();
    }

    private async Task PrintInvoiceAsync(MouseEventArgs _)
    {
        if (_isPrintingInvoice || ActiveTab?.Model is not UiSalesInvoiceFormModel invoice || !invoice.Id.HasValue)
            return;

        if (!string.Equals(invoice.Status, "Posted", StringComparison.OrdinalIgnoreCase))
        {
            _error = "يمكن طباعة فاتورة المبيعات بعد الترحيل فقط.";
            Workspace.NotifyStateChanged();
            return;
        }

        ClearMessages();
        _isPrintingInvoice = true;
        Workspace.NotifyStateChanged();
        try
        {
            var result = await PrintingService.PrintSalesInvoiceAsync(invoice.Id.Value);
            _success = result is null
                ? "تم إرسال فاتورة المبيعات إلى OAS Print."
                : "تم إرسال فاتورة المبيعات إلى OAS Print للطباعة.";
        }
        catch (ApiClientException ex)
        {
            _error = SalesApiErrorPresenter.GetMessage(ex.Error);
        }
        finally
        {
            _isPrintingInvoice = false;
            Workspace.NotifyStateChanged();
        }
    }

    private Task ViewJournalAsync(MouseEventArgs _) { Navigation.NavigateTo("/accounting/journals"); return Task.CompletedTask; }
    private Task ViewInventoryAsync(MouseEventArgs _) { Navigation.NavigateTo("/inventory/transactions"); return Task.CompletedTask; }
    private bool InvoiceReadOnly(UiSalesInvoiceFormModel invoice) => !ActiveTab!.IsEditMode || invoice.Status != "Draft";
    private static Guid? TryGuid(string? value) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
    private static string StatusCss(string status) => SalesArabicPresenter.StatusCss(status);
    private static string RouteFor(SalesEntityType type) => type switch { SalesEntityType.Prescriptions => "/sales/prescriptions", SalesEntityType.CustomerOrders => "/sales/customer-orders", _ => "/sales/invoices" };
    private void ClearMessages() { _error = null; _success = null; }
    public void Dispose() { Workspace.OnChange -= WorkspaceChanged; GC.SuppressFinalize(this); }
}
