using Microsoft.AspNetCore.Components;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Optical.Services;
using OAS.Client.Optical.State;
using OAS.Client.Sales.Mapping;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.OpticalJobs;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Sales;

namespace OAS.Client.Optical.Pages;

public partial class OpticalJobsPage : ComponentBase
{
    [Inject] private IOpticalJobsClientService Jobs { get; set; } = default!;
    [Inject] private IEmployeeClientService Employees { get; set; } = default!;
    [Inject] private OpticalJobsState State { get; set; } = default!;

    [Parameter, SupplyParameterFromQuery(Name = "jobId")] public Guid? JobId { get; set; }

    private readonly UiOpticalJobFilterModel _filter = new();
    private IReadOnlyList<UiOpticalJobRowModel> _rows = [];
    private UiOpticalJobDetailsModel? _selected;
    private bool _initialLoaded;
    private Guid? _openedJobId;

    protected override async Task OnParametersSetAsync()
    {
        if (!_initialLoaded)
        {
            _initialLoaded = true;
            await LoadAsync(resetPage: true);
        }
        if (JobId.HasValue && _openedJobId != JobId.Value)
            await OpenJobIdAsync(JobId.Value);
    }

    private async Task LoadAsync(bool resetPage = false)
    {
        State.IsBusy = true;
        State.Error = null;
        try
        {
            State.Query = BuildQuery(resetPage ? 1 : Math.Max(1, State.Query.PageNumber));
            State.Page = await Jobs.GetPageAsync(State.Query);
            _rows = State.Page.Items.Select(SalesWorkflowUiMapper.ToUi).ToArray();
        }
        catch (ApiClientException ex) { State.Error = ex.Error.Message; }
        catch { State.Error = "تعذر تحميل قائمة أعمال المعمل."; }
        finally { State.IsBusy = false; }
    }

    private OpticalJobWorkQueueRequest BuildQuery(int page) => new()
    {
        PageNumber = page,
        PageSize = State.Query.PageSize <= 0 ? 25 : State.Query.PageSize,
        Search = _filter.Search,
        Status = Enum.TryParse<OpticalJobStatus>(_filter.Status, out var status) ? status : null,
        TechnicianId = ParseGuid(_filter.TechnicianId),
        RequiredDate = _filter.RequiredDate,
        SortBy = "RequiredDate",
        SortDirection = SortDirection.Ascending
    };

    private Task ApplyFiltersAsync() => LoadAsync(resetPage: true);

    private async Task ClearFiltersAsync()
    {
        _filter.Search = null;
        _filter.Status = string.Empty;
        _filter.TechnicianId = string.Empty;
        _filter.TechnicianDisplay = null;
        _filter.RequiredDate = null;
        await LoadAsync(true);
    }

    private async Task TabSelectedAsync(string key)
    {
        _filter.Status = key;
        await LoadAsync(true);
    }

    private async Task PreviousAsync()
    {
        if (!State.Page.HasPreviousPage) return;
        State.Query = BuildQuery(State.Page.PageNumber - 1);
        await LoadAsync();
    }

    private async Task NextAsync()
    {
        if (!State.Page.HasNextPage) return;
        State.Query = BuildQuery(State.Page.PageNumber + 1);
        await LoadAsync();
    }

    private Task OpenAsync(UiOpticalJobRowModel row) => OpenJobIdAsync(row.Id);

    private async Task OpenJobIdAsync(Guid id)
    {
        State.IsBusy = true;
        State.Error = null;
        try
        {
            State.Selected = await Jobs.GetDetailsAsync(id);
            _selected = State.Selected is null ? null : SalesWorkflowUiMapper.ToUi(State.Selected);
            _openedJobId = _selected?.Id;
        }
        catch (ApiClientException ex) { State.Error = ex.Error.Message; }
        catch { State.Error = "تعذر تحميل تفاصيل عمل المعمل."; }
        finally { State.IsBusy = false; }
    }

    private Task CloseDetailsAsync()
    {
        State.Selected = null;
        _selected = null;
        _openedJobId = null;
        return Task.CompletedTask;
    }

    private async Task AssignAsync()
    {
        if (_selected is null) return;
        await MutateAsync(async () =>
        {
            var dto = await Jobs.AssignAsync(_selected.Id,
                new AssignOpticalJobRequest(ParseGuid(_selected.TechnicianId), _selected.RowVersion));
            ApplyDetails(dto, "تم حفظ تعيين الفني.");
        });
    }

    private async Task StartAsync()
    {
        if (_selected is null) return;
        await MutateAsync(async () =>
        {
            var dto = await Jobs.StartAsync(_selected.Id, new OpticalJobActionRequest(_selected.RowVersion));
            ApplyDetails(dto, "تم بدء العمل في الطلب.");
        });
    }

    private async Task ReadyAsync()
    {
        if (_selected is null) return;
        await MutateAsync(async () =>
        {
            var dto = await Jobs.ReadyAsync(_selected.Id, new OpticalJobActionRequest(_selected.RowVersion));
            ApplyDetails(dto, "تم تحويل الطلب إلى جاهز للتسليم.");
        });
    }

    private async Task MutateAsync(Func<Task> action)
    {
        State.IsBusy = true;
        State.Error = null;
        State.Success = null;
        try { await action(); }
        catch (ApiClientException ex) { State.Error = ex.Error.Message; }
        catch (Exception ex) { State.Error = ex.Message; }
        finally { State.IsBusy = false; }
    }

    private void ApplyDetails(OpticalJobDetailsDto? dto, string success)
    {
        if (dto is null) return;
        State.Selected = dto;
        _selected = SalesWorkflowUiMapper.ToUi(dto);
        State.Success = success;
        _ = InvokeAsync(() => LoadAsync());
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchTechniciansAsync(string search, CancellationToken ct)
    {
        var page = await Employees.GetPageAsync(new PageRequest
        {
            PageNumber = 1,
            PageSize = 30,
            Search = string.IsNullOrWhiteSpace(search) ? null : search,
            SortBy = "EmployeeCode"
        }, ct);
        return page.Items.Where(x => x.IsActive && x.IsTechnician)
            .Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.EmployeeCode} - {x.DisplayName}", x.JobTitleName, "fa-solid fa-user-gear"))
            .ToArray();
    }

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
