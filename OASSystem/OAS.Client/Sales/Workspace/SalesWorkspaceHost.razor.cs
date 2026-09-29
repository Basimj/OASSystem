using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Sales.Common;
using OAS.Client.Sales.Mapping;
using OAS.Client.Sales.Services;
using OAS.Client.Sales.Workspace;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
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
    [Inject] private ISalesWorkspaceState Workspace { get; set; } = default!;
    [Inject] private IUiDialogService Dialogs { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly List<UiSalesDocumentListItem> _listItems = [];
    private readonly Dictionary<Guid, SalesCustomerLookupDto> _customers = [];
    private readonly Dictionary<Guid, SalesProductVariantLookupDto> _products = [];
    private readonly Dictionary<Guid, SalesWarehouseLookupDto> _warehouses = [];
    private readonly Dictionary<Guid, SalesCurrencyLookupDto> _currencies = [];
    private readonly Dictionary<Guid, CustomerOrderLookupDto> _orders = [];
    private readonly Dictionary<Guid, SalesPrescriptionRevisionLookupDto> _revisions = [];
    private string? _search;
    private bool _isLoading;
    private int _pageNumber = 1;
    private int _totalPages;
    private long _totalCount;
    private string? _error;
    private string? _success;
    private UiPrescriptionRevisionModel? _revisionDraft;
    private SalesEntityType? _lastSection;

    private SalesWorkspaceTabState? ActiveTab => Workspace.ActiveTab;
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
                    tab.Model = new UiPrescriptionFormModel { PrescriptionCode = rx?.Code ?? string.Empty };
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
                    RequireGuid(model.CustomerId, "يجب تحديد العميل.");
                    var p = model.Id.HasValue ? await Sales.UpdatePrescriptionAsync(model.Id.Value, SalesUiMapper.ToUpdate(model)) : await Sales.CreatePrescriptionAsync(SalesUiMapper.ToCreate(model));
                    if (p is not null) CompleteSave(ActiveTab, p.Id, p.PrescriptionCode, SalesUiMapper.ToUi(p));
                    break;
                case UiCustomerOrderFormModel model:
                    RequireGuid(model.CustomerId, "يجب تحديد العميل."); RequireGuid(model.CurrencyId, "يجب تحديد العملة.");
                    if (model.Lines.Count == 0) throw new InvalidOperationException("يجب إضافة سطر واحد على الأقل.");
                    var o = model.Id.HasValue ? await Sales.UpdateCustomerOrderAsync(model.Id.Value, SalesUiMapper.ToUpdate(model)) : await Sales.CreateCustomerOrderAsync(SalesUiMapper.ToCreate(model));
                    if (o is not null) CompleteSave(ActiveTab, o.Id, o.OrderCode, SalesUiMapper.ToUi(o));
                    break;
                case UiSalesInvoiceFormModel model:
                    SalesInvoiceDto? i;
                    if (!model.Id.HasValue && Guid.TryParse(model.CustomerOrderId, out var orderId) && _orders.TryGetValue(orderId, out var sourceOrder))
                    {
                        i = await Sales.CreateSalesInvoiceFromOrderAsync(orderId, new CreateSalesInvoiceFromOrderRequest(model.InvoiceCode, model.InvoiceDate ?? DateOnly.FromDateTime(DateTime.Today), model.PostingDate ?? DateOnly.FromDateTime(DateTime.Today), model.Description, sourceOrder.RowVersion));
                    }
                    else
                    {
                        RequireGuid(model.CustomerId, "يجب تحديد العميل."); RequireGuid(model.CurrencyId, "يجب تحديد العملة.");
                        if (model.Lines.Count == 0) throw new InvalidOperationException("يجب إضافة سطر واحد على الأقل.");
                        i = model.Id.HasValue ? await Sales.UpdateSalesInvoiceAsync(model.Id.Value, SalesUiMapper.ToUpdate(model)) : await Sales.CreateSalesInvoiceAsync(SalesUiMapper.ToCreate(model));
                    }
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

    private async Task ConfirmOrderAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiCustomerOrderFormModel m || !m.Id.HasValue) return;
        if (!EnsureSaved(ActiveTab)) return;
        try { var dto = await Sales.ConfirmCustomerOrderAsync(m.Id.Value, new ConfirmCustomerOrderRequest(m.RowVersion)); if (dto is not null) ActiveTab.Model = SalesUiMapper.ToUi(dto); _success = "تم تأكيد الطلب وفحص/حجز المخزون حسب المتاح."; Workspace.NotifyStateChanged(); await LoadListAsync(SalesEntityType.CustomerOrders); }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
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
        if (ActiveTab?.Model is not UiSalesInvoiceFormModel m || !m.Id.HasValue || !EnsureSaved(ActiveTab)) return;
        try { var dto = await Sales.ConfirmSalesInvoiceAsync(m.Id.Value, new ConfirmSalesInvoiceRequest(m.RowVersion)); if (dto is not null) ActiveTab.Model = SalesUiMapper.ToUi(dto); _success = "تم تأكيد الفاتورة. أصبحت الحقول المالية الحرجة للقراءة فقط."; Workspace.NotifyStateChanged(); await LoadListAsync(SalesEntityType.SalesInvoices); }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
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
            if (!await Dialogs.ConfirmAsync("ترحيل فاتورة المبيعات", "سيتم ترحيل فاتورة المبيعات وإنشاء أثر مخزني وقيد محاسبي رسمي. بعد الترحيل لن يمكن تعديل الفاتورة مباشرة. هل تريد المتابعة؟", AlertTone.Warning, "ترحيل", "رجوع")) return;
            await Sales.PostSalesInvoiceAsync(m.Id.Value, new PostSalesInvoiceRequest(m.RowVersion));
            var dto = await Sales.GetSalesInvoiceByIdAsync(m.Id.Value);
            if (dto is not null) ActiveTab.Model = SalesUiMapper.ToUi(dto);
            _success = "تم ترحيل الفاتورة بنجاح."; Workspace.NotifyStateChanged(); await LoadListAsync(SalesEntityType.SalesInvoices);
        }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
    }

    private async Task ActivatePrescriptionAsync(MouseEventArgs _) => await SetPrescriptionStatusAsync(PrescriptionStatus.Active);
    private async Task CancelPrescriptionAsync(MouseEventArgs _)
    {
        if (!await Dialogs.ConfirmAsync("إلغاء الوصفة", "هل تريد تغيير حالة الوصفة إلى ملغاة؟", AlertTone.Warning, "إلغاء الوصفة", "رجوع")) return;
        await SetPrescriptionStatusAsync(PrescriptionStatus.Cancelled);
    }
    private async Task SetPrescriptionStatusAsync(PrescriptionStatus status)
    {
        if (ActiveTab?.Model is not UiPrescriptionFormModel m || !m.Id.HasValue || !EnsureSaved(ActiveTab)) return;
        try { var dto = await Sales.SetPrescriptionStatusAsync(m.Id.Value, new SetPrescriptionStatusRequest(status, m.RowVersion)); if (dto is not null) ActiveTab.Model = SalesUiMapper.ToUi(dto); Workspace.NotifyStateChanged(); await LoadListAsync(SalesEntityType.Prescriptions); }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
    }

    private Task BeginRevisionAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiPrescriptionFormModel) return Task.CompletedTask;
        _revisionDraft = new UiPrescriptionRevisionModel { EffectiveDate = DateOnly.FromDateTime(DateTime.Today), Eyes = [new() { Eye = EyeSide.RightOD.ToString(), EyeText = SalesArabicPresenter.EyeText(EyeSide.RightOD) }, new() { Eye = EyeSide.LeftOS.ToString(), EyeText = SalesArabicPresenter.EyeText(EyeSide.LeftOS) }] };
        Workspace.NotifyStateChanged(); return Task.CompletedTask;
    }
    private Task CancelRevisionAsync(MouseEventArgs _) { _revisionDraft = null; Workspace.NotifyStateChanged(); return Task.CompletedTask; }
    private async Task SaveRevisionAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiPrescriptionFormModel p || !p.Id.HasValue || _revisionDraft is null) return;
        try { var dto = await Sales.CreatePrescriptionRevisionAsync(p.Id.Value, SalesUiMapper.ToRevisionRequest(_revisionDraft, p.RowVersion)); if (dto is not null) ActiveTab.Model = SalesUiMapper.ToUi(dto); _revisionDraft = null; _success = "تم إنشاء Revision جديد للوصفة."; Workspace.NotifyStateChanged(); }
        catch (ApiClientException ex) { _error = SalesApiErrorPresenter.GetMessage(ex.Error); }
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
        if (tab.IsDirty && !await Dialogs.ConfirmAsync("إغلاق التبويب", "يوجد تغييرات غير محفوظة في هذا التبويب. هل تريد إغلاقه؟", AlertTone.Warning, "إغلاق", "رجوع")) return;
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
        var date =
            ActiveTab?.Model switch
            {
                UiCustomerOrderFormModel order
                    => order.OrderDate,

                UiSalesInvoiceFormModel invoice
                    => invoice.InvoiceDate,

                _ => null
            }
            ?? DateOnly.FromDateTime(DateTime.Today);

        var rows =
            await Sales.SearchCurrenciesAsync(
                date,
                q,
                20,
                ct);

        foreach (var currency in rows)
        {
            _currencies[currency.Id] = currency;
        }

        return rows
            .Select(currency =>
            {
                /*
                 * العملة تكون غير قابلة للاختيار في حالتين:
                 *
                 * 1. غير نشطة.
                 * 2. لا يوجد لها سعر صرف فعال لتاريخ المستند.
                 */
                var disabled =
                    !currency.IsActive ||
                    !currency.HasEffectiveExchangeRate;

                string secondaryText;

                if (!currency.IsActive)
                {
                    secondaryText =
                        "العملة غير نشطة";
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
                    Value: currency.Id.ToString(),
                    PrimaryText:
                        $"{currency.Code} - {currency.NameAr}",
                    SecondaryText: secondaryText,
                    IconCssClass: currency.HasEffectiveExchangeRate
                        ? "fa-solid fa-coins"
                        : "fa-solid fa-triangle-exclamation",
                    Disabled: disabled);
            })
            .ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchProductItemsAsync(string q, CancellationToken ct)
    {
        var rows = await Sales.SearchProductVariantsAsync(q, 20, ct); foreach (var x in rows) _products[x.Id] = x;
        return rows.Select(x => new UiLookupItem(x.Id.ToString(), $"{x.ProductCode} - {x.ProductNameAr}", string.Join(" • ", new[] { x.SKU, x.Barcode, x.VariantName }.Where(s => !string.IsNullOrWhiteSpace(s))), "fa-solid fa-box", !x.IsActive)).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchWarehouseItemsAsync(UiSalesLineModel line, string q, CancellationToken ct)
    {
        Guid? variant = Guid.TryParse(line.ProductVariantId, out var id) ? id : null;
        var rows = await Sales.SearchWarehousesAsync(variant, q, 20, ct); foreach (var x in rows) _warehouses[x.Id] = x;
        return rows.Select(x => new UiLookupItem(x.Id.ToString(), $"{x.Code} - {x.NameAr}", x.AvailableQuantity.HasValue ? $"المتاح: {x.AvailableQuantity:N2}" : null, "fa-solid fa-warehouse", !x.IsActive)).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchOrderItemsAsync(string q, CancellationToken ct)
    {
        Guid? customerId = ActiveTab?.Model is UiSalesInvoiceFormModel i && Guid.TryParse(i.CustomerId, out var id) ? id : null;
        var rows = await Sales.SearchCustomerOrdersAsync(customerId, q, 20, ct); foreach (var x in rows) _orders[x.Id] = x;
        return rows.Select(x => new UiLookupItem(x.Id.ToString(), x.OrderCode, $"{x.OrderDate:yyyy-MM-dd} • {x.TotalAmount:N2} {x.CurrencyCode}", "fa-solid fa-clipboard-list", x.Status == CustomerOrderStatus.Cancelled)).ToArray();
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

    private Task SelectionChangedAsync(UiSalesSelectionChange change)
    {
        if (ActiveTab?.Model is UiCustomerOrderFormModel order)
        {
            if (change.Field == "Customer" && Guid.TryParse(change.Value, out var cid) && _customers.TryGetValue(cid, out var c)) order.CustomerDisplay = $"{c.CustomerCode} - {c.NameAr}";
            if (change.Field == "Currency" && Guid.TryParse(change.Value, out var curId) && _currencies.TryGetValue(curId, out var cur)) { order.CurrencyDisplay = $"{cur.Code} - {cur.NameAr}"; order.CurrencyCode = cur.Code; order.CurrencyDecimalPlaces = cur.DecimalPlaces; order.ExchangeRate = cur.EffectiveExchangeRate; }
            if (change.Field == "PrescriptionRevision" && Guid.TryParse(change.Value, out var rid) && _revisions.TryGetValue(rid, out var rev)) order.PrescriptionDisplay = $"{rev.PrescriptionCode} / Revision {rev.RevisionNumber}";
        }
        else if (ActiveTab?.Model is UiSalesInvoiceFormModel invoice)
        {
            if (change.Field == "Customer" && Guid.TryParse(change.Value, out var cid) && _customers.TryGetValue(cid, out var c)) invoice.CustomerDisplay = $"{c.CustomerCode} - {c.NameAr}";
            if (change.Field == "Currency" && Guid.TryParse(change.Value, out var curId) && _currencies.TryGetValue(curId, out var cur)) { invoice.CurrencyDisplay = $"{cur.Code} - {cur.NameAr}"; invoice.CurrencyCode = cur.Code; invoice.CurrencyDecimalPlaces = cur.DecimalPlaces; invoice.ExchangeRate = cur.EffectiveExchangeRate; }
            if (change.Field == "CustomerOrder" && Guid.TryParse(change.Value, out var oid) && _orders.TryGetValue(oid, out var source)) { invoice.CustomerOrderDisplay = source.OrderCode; invoice.CustomerId = source.CustomerId.ToString(); }
            if (change.Field == "PrescriptionRevision" && Guid.TryParse(change.Value, out var rid) && _revisions.TryGetValue(rid, out var rev)) invoice.PrescriptionDisplay = $"{rev.PrescriptionCode} / Revision {rev.RevisionNumber}";
        }
        return MarkDirtyAsync();
    }

    private Task ProductSelectedAsync(UiSalesLineSelectionChange change)
    {
        if (Guid.TryParse(change.Value, out var id) && _products.TryGetValue(id, out var product))
        {
            change.Line.ProductDisplay = string.IsNullOrWhiteSpace(product.VariantName) ? product.ProductNameAr : $"{product.ProductNameAr} - {product.VariantName}";
            change.Line.ProductCodeSnapshot = product.ProductCode; change.Line.Description = change.Line.ProductDisplay; if (change.Line.ActualUnitPrice <= 0) change.Line.ActualUnitPrice = product.SellingPrice;
        }
        return Task.CompletedTask;
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

    private Task ViewJournalAsync(MouseEventArgs _) { Navigation.NavigateTo("/accounting/journals"); return Task.CompletedTask; }
    private Task ViewInventoryAsync(MouseEventArgs _) { Navigation.NavigateTo("/inventory/transactions"); return Task.CompletedTask; }
    private bool InvoiceReadOnly(UiSalesInvoiceFormModel invoice) => !ActiveTab!.IsEditMode || invoice.Status != "Draft";
    private static Guid? TryGuid(string? value) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
    private static string StatusCss(string status) => SalesArabicPresenter.StatusCss(status);
    private static string RouteFor(SalesEntityType type) => type switch { SalesEntityType.Prescriptions => "/sales/prescriptions", SalesEntityType.CustomerOrders => "/sales/customer-orders", _ => "/sales/invoices" };
    private void ClearMessages() { _error = null; _success = null; }
    public void Dispose() { Workspace.OnChange -= WorkspaceChanged; GC.SuppressFinalize(this); }
}
