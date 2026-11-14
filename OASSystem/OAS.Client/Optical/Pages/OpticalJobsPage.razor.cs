using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Optical.Services;
using OAS.Client.Optical.State;
using OAS.Client.Sales.Mapping;
using OAS.Client.Sales.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.OpticalJobs;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Sales;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Optical.Pages;

public partial class OpticalJobsPage : ComponentBase
{
    [Inject] private IOpticalJobsClientService Jobs { get; set; } = default!;
    [Inject] private IEmployeeClientService Employees { get; set; } = default!;
    [Inject] private ISalesClientService Sales { get; set; } = default!;
    [Inject] private OpticalJobsState State { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter, SupplyParameterFromQuery(Name = "jobId")] public Guid? JobId { get; set; }
    [Parameter] public Guid ProductionJobId { get; set; }
    private bool IsProductionRoute => Navigation.ToBaseRelativePath(Navigation.Uri).StartsWith("optical/jobs/production/", StringComparison.OrdinalIgnoreCase);
    private bool _wasProduction;
    private Guid? _materialIssueKey;
    private string? _materialIssueSignature;
    private string? _breakageSignature;
    private string? _breakageKey;
    private bool _mutationInProgress;

    private readonly UiOpticalJobFilterModel _filter = new();
    private IReadOnlyList<UiOpticalJobRowModel> _rows = [];
    private UiOpticalJobDetailsModel? _selected;
    private bool _initialLoaded;
    private Guid? _openedJobId;

    protected override async Task OnParametersSetAsync()
    {
        if (IsProductionRoute)
        {
            _wasProduction = true;
            if (ProductionJobId != Guid.Empty && _openedJobId != ProductionJobId)
                await OpenJobIdAsync(ProductionJobId);
            return;
        }
        if (JobId.HasValue && JobId.Value != Guid.Empty)
        {
            Navigation.NavigateTo($"/optical/jobs/production/{JobId.Value:D}", replace: true);
            return;
        }
        if (!_initialLoaded || _wasProduction)
        {
            _wasProduction = false;
            _initialLoaded = true;
            await LoadAsync(resetPage: true);
        }
    }

    private async Task LoadAsync(bool resetPage = false)
    {
        State.IsBusy = true;
        try
        {
            State.Query = BuildQuery(resetPage ? 1 : Math.Max(1, State.Query.PageNumber));
            State.Page = await Jobs.GetPageAsync(State.Query);
            State.Error = null;
            _rows = State.Page.Items.Select(SalesWorkflowUiMapper.ToUi).ToArray();
        }
        catch (ApiClientException ex) { State.Error = "تعذر تحديث قائمة المعمل. تأكد من الاتصال وحاول مجددًا."; ApiFeedback.Show(ex.Error); }
        catch { State.Error = "تعذر تحديث قائمة المعمل. تأكد من الاتصال وحاول مجددًا."; ApiFeedback.ShowUnexpected(); }
        finally { State.IsBusy = false; }
    }

    private OpticalJobWorkQueueRequest BuildQuery(int page) => new()
    {
        PageNumber = page, PageSize = State.Query.PageSize <= 0 ? 10 : State.Query.PageSize,
        Search = _filter.Search,
        Status = Enum.TryParse<OpticalJobStatus>(_filter.Status, out var status) ? status : null,
        TechnicianId = ParseGuid(_filter.TechnicianId), RequiredDate = _filter.RequiredDate,
        RequiredDateFrom = _filter.RequiredDateFrom, RequiredDateTo = _filter.RequiredDateTo,
        HasBreakage = _filter.HasBreakage, HasRemake = _filter.HasRemake,
        SortBy = "RequiredDate", SortDirection = SortDirection.Ascending
    };

    private Task ApplyFiltersAsync() => LoadAsync(resetPage: true);
    private async Task ClearFiltersAsync()
    {
        _filter.Search = null; _filter.Status = string.Empty; _filter.TechnicianId = string.Empty; _filter.TechnicianDisplay = null;
        _filter.RequiredDate = null; _filter.RequiredDateFrom = null; _filter.RequiredDateTo = null; _filter.HasBreakage = null; _filter.HasRemake = null;
        await LoadAsync(true);
    }
    private async Task TabSelectedAsync(string key) { _filter.Status = key; await LoadAsync(true); }
    private async Task PageSizeChangedAsync(int size) { State.Query = State.Query with { PageSize = size }; await LoadAsync(true); }
    private Task BackToListAsync() { Navigation.NavigateTo("/optical/jobs"); return Task.CompletedTask; }
    private Task OpenCustomerOrderAsync()
    {
        if (_selected is not null && _selected.CustomerOrderId != Guid.Empty)
            Navigation.NavigateTo($"/sales/customer-orders?orderId={_selected.CustomerOrderId:D}");
        return Task.CompletedTask;
    }
    private async Task PreviousAsync() { if (!State.Page.HasPreviousPage) return; State.Query = BuildQuery(State.Page.PageNumber - 1); await LoadAsync(); }
    private async Task NextAsync() { if (!State.Page.HasNextPage) return; State.Query = BuildQuery(State.Page.PageNumber + 1); await LoadAsync(); }
    private Task OpenAsync(UiOpticalJobRowModel row) { Navigation.NavigateTo($"/optical/jobs/production/{row.Id:D}"); return Task.CompletedTask; }

    private async Task OpenJobIdAsync(Guid id)
    {
        State.IsBusy = true;
        try
        {
            State.Selected = await Jobs.GetDetailsAsync(id);
            State.Error = null;
            _selected = State.Selected is null ? null : SalesWorkflowUiMapper.ToUi(State.Selected);
            _openedJobId = _selected?.Id;
        }
        catch (ApiClientException ex) { State.Error = "تعذر تحميل تفاصيل أمر المعمل."; _selected = null; _openedJobId = null; ApiFeedback.Show(ex.Error); }
        catch { State.Error = "تعذر تحميل تفاصيل أمر المعمل."; _selected = null; _openedJobId = null; ApiFeedback.ShowUnexpected(); }
        finally { State.IsBusy = false; }
    }

    private Task CloseDetailsAsync() { State.Selected = null; _selected = null; _openedJobId = null; return Task.CompletedTask; }

    private Task AssignAsync() => MutateAsync(
        () => Jobs.AssignAsync(_selected!.Id, new AssignOpticalJobRequest(ParseGuid(_selected.TechnicianId), _selected.RowVersion)),
        "تم حفظ تعيين الفني.");

    private Task StartAsync() => MutateAsync(
        () => Jobs.StartAsync(_selected!.Id, new OpticalJobActionRequest(_selected.RowVersion)), "تم بدء العمل في الطلب.");

    private async Task IssueMaterialsAsync()
    {
        if (_selected is null || !Guid.TryParse(_selected.MaterialLineId, out var lineId)) { Snackbar.Error("اختر المادة المطلوب صرفها."); return; }
        var line = _selected.Lines.FirstOrDefault(x => x.Id == lineId);
        if (line?.ProductVariantId is null) { Snackbar.Error("السطر المختار لا يحتوي SKU صالحًا للصرف."); return; }
        if (!Guid.TryParse(_selected.MaterialWarehouseId, out var warehouseId)) { Snackbar.Error("اختر مخزن الصرف."); return; }
        if (_selected.StatusKey is not ("MaterialsAvailable" or "InProduction")) { Snackbar.Error("لا يمكن صرف مادة إضافية في المرحلة الحالية."); return; }
        if (_selected.MaterialQuantity <= 0m) { Snackbar.Error("كمية الصرف يجب أن تكون أكبر من صفر."); return; }
        var signature = $"{_selected.Id:D}:{warehouseId:D}:{line.ProductVariantId.Value:D}:{_selected.MaterialQuantity}";
        if (_materialIssueSignature != signature)
        {
            _materialIssueKey = Guid.NewGuid();
            _materialIssueSignature = signature;
        }
        var request = new IssueOpticalJobMaterialsRequest(warehouseId, [new IssueOpticalJobMaterialLineRequest(line.ProductVariantId.Value, _selected.MaterialQuantity)], _selected.RowVersion, _materialIssueKey);
        await MutateAsync(() => Jobs.IssueMaterialsAsync(_selected.Id, request), "تم صرف المواد الإضافية لأمر المعمل.");
    }

    private Task SendToQualityControlAsync() => MutateAsync(
        () => Jobs.SendToQualityControlAsync(_selected!.Id, new OpticalJobActionRequest(_selected.RowVersion)), "تم إنشاء محاولة فحص الجودة.");

    private async Task CompleteQualityControlAsync()
    {
        if (_selected?.LatestQualityCheck is null) return;
        var qc = _selected.LatestQualityCheck;
        var items = qc.Items.Select(x => new OpticalQualityCheckItemRequest(
            x.CheckCode,
            Enum.TryParse<OpticalQualityCheckItemResult>(x.Result, out var result) ? result : OpticalQualityCheckItemResult.NotChecked,
            x.Notes)).ToArray();
        if (items.Any(x => x.Result == OpticalQualityCheckItemResult.NotChecked))
        {
            Snackbar.Error("أكمل تقييم جميع بنود فحص الجودة قبل اعتماد النتيجة.");
            return;
        }
        var hasFailure = items.Any(x => x.Result == OpticalQualityCheckItemResult.Fail);
        var action = hasFailure && Enum.TryParse<OpticalQcFailureAction>(_selected.QcFailureAction, out var parsed) ? parsed : (OpticalQcFailureAction?)null;
        if (hasFailure && string.IsNullOrWhiteSpace(_selected.QcFailureReason))
        {
            Snackbar.Error("اكتب سبب فشل فحص الجودة.");
            return;
        }
        var remakeLine = action == OpticalQcFailureAction.Remake ? ParseGuid(_selected.QcRemakeLineId) : null;
        if (hasFailure && action == OpticalQcFailureAction.Remake && (!remakeLine.HasValue || _selected.QcRemakeQuantity <= 0m))
        {
            Snackbar.Error("حدد سطر إعادة التصنيع وكمية صحيحة.");
            return;
        }
        var request = new CompleteOpticalQualityCheckRequest(_selected.RowVersion, items, action,
            hasFailure ? _selected.QcFailureReason : null, qc.GeneralNotes, remakeLine, _selected.QcRemakeQuantity, qc.RowVersion);
        await MutateAsync(() => Jobs.CompleteQualityControlAsync(_selected.Id, qc.Id, request),
            hasFailure ? "تم تسجيل فشل الجودة وفتح مسار المعالجة." : "تم اجتياز فحص الجودة.");
    }

    private Task ReadyAsync() => MutateAsync(
        () => Jobs.ReadyAsync(_selected!.Id, new OpticalJobActionRequest(_selected.RowVersion)), "تم تحويل الطلب إلى جاهز للتسليم.");
    private Task DeliverAsync()
    {
        if (_selected is null || _selected.CustomerOrderId == Guid.Empty)
        {
            Snackbar.Error("لا يوجد طلب عميل صالح لإكمال التسوية والتسليم.");
            return Task.CompletedTask;
        }

        Navigation.NavigateTo($"/sales/delivery/{_selected.CustomerOrderId:D}");
        return Task.CompletedTask;
    }
    private async Task CancelAsync()
    {
        if (_selected is null || _selected.StatusKey is "Delivered" or "Cancelled") return;
        if (!await ConfirmAsync("إلغاء أمر المعمل سيوقف سير العمل. هل تؤكد الإلغاء؟")) return;
        await MutateAsync(() => Jobs.CancelAsync(_selected.Id, new OpticalJobActionRequest(_selected.RowVersion)), "تم إلغاء أمر المعمل.");
    }

    private async Task RecordBreakageAsync()
    {
        if (_selected is null || !Guid.TryParse(_selected.BreakageLineId, out var lineId)) { Snackbar.Error("اختر سطر العدسة/الإطار المتضرر."); return; }
        var line = _selected.Lines.FirstOrDefault(x => x.Id == lineId);
        if (line?.ProductVariantId is null) { Snackbar.Error("السطر المختار لا يحتوي SKU صالحًا لتسجيل الكسر."); return; }
        Guid? warehouseId = null;
        if (_selected.BreakageRequiresReplacement)
        {
            if (!Guid.TryParse(_selected.BreakageWarehouseId, out var parsedWarehouse)) { Snackbar.Error("اختر مخزن البديل."); return; }
            warehouseId = parsedWarehouse;
        }
        if (_selected.StatusKey is not ("InProduction" or "AwaitingQC")) { Snackbar.Error("لا يمكن تسجيل كسر إلا أثناء التصنيع أو الجودة."); return; }
        if (_selected.BreakageQuantity <= 0m || _selected.BreakageQuantity > line.Quantity) { Snackbar.Error("كمية الكسر غير صالحة."); return; }
        if (!await ConfirmAsync($"تسجيل كسر للسطر {line.LineNumber} بكمية {_selected.BreakageQuantity:0.###}. {( _selected.BreakageRequiresReplacement ? "سيجري التحقق من المخزون وتسجيل تكلفة البديل." : "لن يُصرف بديل.")} هل تؤكد؟")) return;
        var eye = line.EyeText == "OD" ? EyeSide.RightOD : line.EyeText == "OS" ? EyeSide.LeftOS : (EyeSide?)null;
        var breakageSignature = $"{_selected.Id:D}:{line.Id:D}:{_selected.BreakageQuantity}:{_selected.BreakageRequiresReplacement}:{warehouseId}:{_selected.BreakageReasonCode}:{_selected.BreakageReasonText}";
        if (_breakageSignature != breakageSignature)
        {
            _breakageSignature = breakageSignature;
            _breakageKey = $"LAB-{_selected.Id:N}-{Guid.NewGuid():N}";
        }
        var request = new RecordOpticalJobBreakageRequest(line.Id, line.ProductVariantId.Value, eye,
            _selected.BreakageQuantity, _selected.BreakageReasonCode, _selected.BreakageReasonText,
            ParseGuid(_selected.TechnicianId), _selected.BreakageRequiresReplacement, warehouseId, _selected.RowVersion,
            _breakageKey);
        await MutateAsync(() => Jobs.RecordBreakageAsync(_selected.Id, request), "تم تسجيل الكسر ومعالجة البديل حسب التوفر.");
    }

    private async Task CreateRemakeAsync()
    {
        if (_selected is null || !Guid.TryParse(_selected.RemakeLineId, out var lineId)) { Snackbar.Error("اختر السطر المراد إعادة تصنيعه."); return; }
        if (_selected.StatusKey is "Delivered" or "Cancelled") { Snackbar.Error("لا يمكن إعادة تصنيع أمر مغلق."); return; }
        var line = _selected.Lines.FirstOrDefault(x => x.Id == lineId);
        if (_selected.RemakeQuantity <= 0m || line is null || _selected.RemakeQuantity > line.Quantity) { Snackbar.Error("كمية إعادة التصنيع غير صالحة."); return; }
        if (string.IsNullOrWhiteSpace(_selected.RemakeReason)) { Snackbar.Error("اكتب سبب إعادة التصنيع."); return; }
        var request = new CreateOpticalJobRemakeRequest(lineId, null, null, line?.ProductVariantId, _selected.RemakeQuantity, _selected.RemakeReason!, ParseGuid(_selected.RemakeWarehouseId), _selected.RowVersion);
        if (!await ConfirmAsync($"إنشاء إعادة تصنيع للسطر {line?.LineNumber} بكمية {_selected.RemakeQuantity:0.###}؟")) return;
        await MutateAsync(() => Jobs.CreateRemakeAsync(_selected.Id, request), "تم إنشاء إعادة التصنيع.");
    }

    private Task StartRemakeAsync(Guid remakeId)
    {
        var remake = _selected?.Remakes.FirstOrDefault(x => x.Id == remakeId);
        if (_selected is null || remake is null) return Task.CompletedTask;
        if (_selected.StatusKey != "InProduction") { Snackbar.Error("يجب أن يكون الأمر قيد الإنتاج."); return Task.CompletedTask; }
        var line = _selected.Lines.FirstOrDefault(x => x.Id == remake.OpticalJobLineId);
        var warehouseId = ParseGuid(_selected.RemakeWarehouseId);
        if (line?.ProductVariantId.HasValue == true && !warehouseId.HasValue)
        {
            Snackbar.Error("اختر مخزن المواد وتحقق من توفرها قبل بدء إعادة التصنيع.");
            return Task.CompletedTask;
        }
        return MutateAsync(
            () => Jobs.StartRemakeAsync(_selected.Id, remakeId, new OpticalJobRemakeActionRequest(_selected.RowVersion, remake.RowVersion, warehouseId)),
            "تم بدء إعادة التصنيع بعد التحقق من المواد.");
    }

    private Task SendRemakeToQcAsync(Guid remakeId)
    {
        var remake = _selected?.Remakes.FirstOrDefault(x => x.Id == remakeId);
        if (_selected is null || remake is null) return Task.CompletedTask;
        return MutateAsync(
            () => Jobs.SendRemakeToQualityControlAsync(_selected.Id, remakeId, new OpticalJobRemakeActionRequest(_selected.RowVersion, remake.RowVersion)),
            "تم إرسال إعادة التصنيع إلى فحص الجودة.");
    }

    private async Task MutateAsync(Func<Task<ApiCallResult<OpticalJobDetailsDto>>> action, string success)
    {
        if (_selected is null || _mutationInProgress) return;
        _mutationInProgress = true;
        State.IsBusy = true;
        try
        {
            var result = await action();
            if (!result.Succeeded) { if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected(); return; }
            if (result.Value is not null) ApplyDetails(result.Value);
            _materialIssueSignature = null;
            _materialIssueKey = null;
            _breakageSignature = null;
            _breakageKey = null;
            Snackbar.Success(success);
            if (!IsProductionRoute) await LoadAsync();
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { State.IsBusy = false; _mutationInProgress = false; }
    }

    private ValueTask<bool> ConfirmAsync(string message) => JS.InvokeAsync<bool>("confirm", message);

    private void ApplyDetails(OpticalJobDetailsDto dto)
    {
        State.Selected = dto; _selected = SalesWorkflowUiMapper.ToUi(dto); _openedJobId = dto.Id;
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchTechniciansAsync(string search, CancellationToken ct)
    {
        var page = await Employees.GetPageAsync(new PageRequest { PageNumber = 1, PageSize = 30, Search = string.IsNullOrWhiteSpace(search) ? null : search, SortBy = "EmployeeCode" }, ct);
        return page.Items.Where(x => x.IsActive && x.IsTechnician).Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.EmployeeCode} - {x.DisplayName}", x.JobTitleName, "fa-solid fa-user-gear")).ToArray();
    }

    private Task<IReadOnlyList<UiLookupItem>> SearchBreakageWarehousesAsync(string search, CancellationToken ct) => SearchWarehousesAsync(ParseGuid(_selected?.BreakageLineId), search, ct);
    private Task<IReadOnlyList<UiLookupItem>> SearchMaterialWarehousesAsync(string search, CancellationToken ct) => SearchWarehousesAsync(ParseGuid(_selected?.MaterialLineId), search, ct);
    private Task<IReadOnlyList<UiLookupItem>> SearchRemakeWarehousesAsync(string search, CancellationToken ct) => SearchWarehousesAsync(ParseGuid(_selected?.RemakeLineId), search, ct);

    private async Task<IReadOnlyList<UiLookupItem>> SearchWarehousesAsync(Guid? lineId, string search, CancellationToken ct)
    {
        var variantId = lineId.HasValue ? _selected?.Lines.FirstOrDefault(x => x.Id == lineId.Value)?.ProductVariantId : null;
        var rows = await Sales.SearchWarehousesAsync(variantId, search, 30, false, ct);
        return rows.Where(x => x.IsActive).Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.Code} - {x.Name}", x.AvailableQuantity.HasValue ? $"المتاح {x.AvailableQuantity:N3}" : null, "fa-solid fa-warehouse")).ToArray();
    }

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
