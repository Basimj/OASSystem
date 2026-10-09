using Microsoft.AspNetCore.Components;
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

    [Parameter, SupplyParameterFromQuery(Name = "jobId")] public Guid? JobId { get; set; }

    private readonly UiOpticalJobFilterModel _filter = new();
    private IReadOnlyList<UiOpticalJobRowModel> _rows = [];
    private UiOpticalJobDetailsModel? _selected;
    private bool _initialLoaded;
    private Guid? _openedJobId;

    protected override async Task OnParametersSetAsync()
    {
        if (!_initialLoaded) { _initialLoaded = true; await LoadAsync(resetPage: true); }
        if (JobId.HasValue && _openedJobId != JobId.Value) await OpenJobIdAsync(JobId.Value);
    }

    private async Task LoadAsync(bool resetPage = false)
    {
        State.IsBusy = true;
        try
        {
            State.Query = BuildQuery(resetPage ? 1 : Math.Max(1, State.Query.PageNumber));
            State.Page = await Jobs.GetPageAsync(State.Query);
            _rows = State.Page.Items.Select(SalesWorkflowUiMapper.ToUi).ToArray();
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { State.IsBusy = false; }
    }

    private OpticalJobWorkQueueRequest BuildQuery(int page) => new()
    {
        PageNumber = page, PageSize = State.Query.PageSize <= 0 ? 25 : State.Query.PageSize,
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
    private async Task PreviousAsync() { if (!State.Page.HasPreviousPage) return; State.Query = BuildQuery(State.Page.PageNumber - 1); await LoadAsync(); }
    private async Task NextAsync() { if (!State.Page.HasNextPage) return; State.Query = BuildQuery(State.Page.PageNumber + 1); await LoadAsync(); }
    private Task OpenAsync(UiOpticalJobRowModel row) => OpenJobIdAsync(row.Id);

    private async Task OpenJobIdAsync(Guid id)
    {
        State.IsBusy = true;
        try
        {
            State.Selected = await Jobs.GetDetailsAsync(id);
            _selected = State.Selected is null ? null : SalesWorkflowUiMapper.ToUi(State.Selected);
            _openedJobId = _selected?.Id;
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
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
        var request = new IssueOpticalJobMaterialsRequest(warehouseId, [new IssueOpticalJobMaterialLineRequest(line.ProductVariantId.Value, _selected.MaterialQuantity)], _selected.RowVersion, Guid.NewGuid());
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
        var hasFailure = items.Any(x => x.Result == OpticalQualityCheckItemResult.Fail);
        var action = hasFailure && Enum.TryParse<OpticalQcFailureAction>(_selected.QcFailureAction, out var parsed) ? parsed : (OpticalQcFailureAction?)null;
        var remakeLine = action == OpticalQcFailureAction.Remake ? ParseGuid(_selected.QcRemakeLineId) : null;
        var request = new CompleteOpticalQualityCheckRequest(_selected.RowVersion, items, action,
            hasFailure ? _selected.QcFailureReason : null, qc.GeneralNotes, remakeLine, _selected.QcRemakeQuantity, qc.RowVersion);
        await MutateAsync(() => Jobs.CompleteQualityControlAsync(_selected.Id, qc.Id, request),
            hasFailure ? "تم تسجيل فشل الجودة وفتح مسار المعالجة." : "تم اجتياز فحص الجودة.");
    }

    private Task ReadyAsync() => MutateAsync(
        () => Jobs.ReadyAsync(_selected!.Id, new OpticalJobActionRequest(_selected.RowVersion)), "تم تحويل الطلب إلى جاهز للتسليم.");
    private Task DeliverAsync() => MutateAsync(
        () => Jobs.DeliverAsync(_selected!.Id, new OpticalJobActionRequest(_selected.RowVersion)), "تم تسليم أمر المعمل وإكمال الطلب.");
    private Task CancelAsync() => MutateAsync(
        () => Jobs.CancelAsync(_selected!.Id, new OpticalJobActionRequest(_selected.RowVersion)), "تم إلغاء أمر المعمل.");

    private async Task RecordBreakageAsync()
    {
        if (_selected is null || !Guid.TryParse(_selected.BreakageLineId, out var lineId)) { Snackbar.Error("اختر سطر العدسة/الإطار المتضرر."); return; }
        var line = _selected.Lines.FirstOrDefault(x => x.Id == lineId);
        if (line?.ProductVariantId is null) { Snackbar.Error("السطر المختار لا يحتوي SKU صالحًا لتسجيل الكسر."); return; }
        if (!Guid.TryParse(_selected.BreakageWarehouseId, out var warehouseId)) { Snackbar.Error("اختر مخزن البديل."); return; }
        var eye = line.EyeText == "OD" ? EyeSide.RightOD : line.EyeText == "OS" ? EyeSide.LeftOS : (EyeSide?)null;
        var request = new RecordOpticalJobBreakageRequest(line.Id, line.ProductVariantId.Value, eye,
            _selected.BreakageQuantity, _selected.BreakageReasonCode, _selected.BreakageReasonText,
            ParseGuid(_selected.TechnicianId), _selected.BreakageRequiresReplacement, warehouseId, _selected.RowVersion,
            $"LAB-{_selected.Id:N}-{Guid.NewGuid():N}");
        await MutateAsync(() => Jobs.RecordBreakageAsync(_selected.Id, request), "تم تسجيل الكسر ومعالجة البديل حسب التوفر.");
    }

    private async Task CreateRemakeAsync()
    {
        if (_selected is null || !Guid.TryParse(_selected.RemakeLineId, out var lineId)) { Snackbar.Error("اختر السطر المراد إعادة تصنيعه."); return; }
        var line = _selected.Lines.FirstOrDefault(x => x.Id == lineId);
        if (string.IsNullOrWhiteSpace(_selected.RemakeReason)) { Snackbar.Error("اكتب سبب إعادة التصنيع."); return; }
        var request = new CreateOpticalJobRemakeRequest(lineId, null, null, line?.ProductVariantId, _selected.RemakeQuantity, _selected.RemakeReason!, ParseGuid(_selected.RemakeWarehouseId), _selected.RowVersion);
        await MutateAsync(() => Jobs.CreateRemakeAsync(_selected.Id, request), "تم إنشاء إعادة التصنيع.");
    }

    private Task StartRemakeAsync(Guid remakeId)
    {
        var remake = _selected?.Remakes.FirstOrDefault(x => x.Id == remakeId);
        if (_selected is null || remake is null) return Task.CompletedTask;
        return MutateAsync(
            () => Jobs.StartRemakeAsync(_selected.Id, remakeId, new OpticalJobRemakeActionRequest(_selected.RowVersion, remake.RowVersion)),
            "تم بدء إعادة التصنيع.");
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
        if (_selected is null) return;
        State.IsBusy = true;
        try
        {
            var result = await action();
            if (!result.Succeeded) { if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected(); return; }
            if (result.Value is not null) ApplyDetails(result.Value);
            Snackbar.Success(success);
            await LoadAsync();
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { State.IsBusy = false; }
    }

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
        var rows = await Sales.SearchWarehousesAsync(variantId, search, 30, ct);
        return rows.Where(x => x.IsActive).Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", x.AvailableQuantity.HasValue ? $"المتاح {x.AvailableQuantity:N3}" : null, "fa-solid fa-warehouse")).ToArray();
    }

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
