using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Services;
using OAS.Client.Inventory.Services;
using OAS.Client.Purchasing.CustomerDemand.Services;
using OAS.Client.Purchasing.CustomerDemand.State;
using OAS.Client.Sales.Mapping;
using OAS.Client.Sales.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.CustomerDemand;
using OAS.Contracts.Sales.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Sales;

namespace OAS.Client.Purchasing.CustomerDemand.Pages;

public partial class CustomerDemandTrackingPage : ComponentBase
{
    [Inject] private ICustomerDemandTrackingClientService Tracking { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IInventoryClientService Inventory { get; set; } = default!;
    [Inject] private ISalesClientService Sales { get; set; } = default!;
    [Inject] private CustomerDemandTrackingState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter, SupplyParameterFromQuery(Name = "orderId")] public Guid? CustomerOrderId { get; set; }
    [Parameter, SupplyParameterFromQuery(Name = "lineId")] public Guid? LineId { get; set; }

    private readonly UiDemandTrackingFilterModel _filter = new();
    private IReadOnlyList<UiWorkflowMetric> _metrics = [];
    private IReadOnlyList<UiDemandTrackingRowModel> _rows = [];
    private UiDemandTrackingDetailsModel? _selected;
    private IReadOnlyList<UiSupplierHistoryRowModel> _history = [];
    private bool _initialLoaded;
    private Guid? _openedLineId;

    protected override async Task OnParametersSetAsync()
    {
        if (!_initialLoaded)
        {
            _initialLoaded = true;
            if (CustomerOrderId.HasValue)
            {
                var order = await Sales.GetCustomerOrderByIdAsync(CustomerOrderId.Value);
                if (order is not null)
                {
                    _filter.CustomerOrderId = order.Id.ToString("D");
                    _filter.CustomerOrderDisplay = order.OrderCode;
                    _filter.CustomerId = order.CustomerId.ToString("D");
                    _filter.CustomerDisplay = order.CustomerName;
                }
            }
            await LoadAsync(resetPage: true);
        }

        if (LineId.HasValue && _openedLineId != LineId.Value)
            await OpenLineIdAsync(LineId.Value);
    }

    private async Task LoadAsync(bool resetPage = false)
    {
        State.IsBusy = true;
        State.Error = null;
        try
        {
            State.Query = BuildQuery(resetPage ? 1 : Math.Max(1, State.Query.PageNumber));
            var pageTask = Tracking.GetPageAsync(State.Query);
            var summaryTask = Tracking.GetSummaryAsync(State.Query);
            await Task.WhenAll(pageTask, summaryTask);
            State.Page = pageTask.Result;
            State.Summary = summaryTask.Result;
            _rows = State.Page.Items.Select(SalesWorkflowUiMapper.ToUi).ToArray();
            _metrics = BuildMetrics(State.Summary);
        }
        catch (ApiClientException ex) { State.Error = ex.Error.Message; }
        catch { State.Error = "تعذر تحميل متابعة النواقص والتوريد."; }
        finally { State.IsBusy = false; }
    }

    private CustomerDemandTrackingQueryRequest BuildQuery(int page) => new()
    {
        PageNumber = page,
        PageSize = State.Query.PageSize <= 0 ? 25 : State.Query.PageSize,
        Search = _filter.Search,
        RequestDateFrom = _filter.RequestDateFrom,
        RequestDateTo = _filter.RequestDateTo,
        SupplierId = ParseGuid(_filter.SupplierId),
        Status = Enum.TryParse<CustomerDemandTrackingStatus>(_filter.Status, out var status) ? status : null,
        ProductType = Enum.TryParse<SalesLineType>(_filter.ProductType, out var productType) ? productType : null,
        WarehouseId = ParseGuid(_filter.WarehouseId),
        ExpectedDeliveryDate = _filter.ExpectedDeliveryDate,
        CustomerOrderId = ParseGuid(_filter.CustomerOrderId) ?? CustomerOrderId,
        CustomerId = ParseGuid(_filter.CustomerId),
        OverdueOnly = _filter.OverdueOnly,
        SortBy = "RequestDate",
        SortDirection = SortDirection.Descending
    };

    private static IReadOnlyList<UiWorkflowMetric> BuildMetrics(CustomerDemandTrackingSummaryDto? s) => s is null ? [] :
    [
        new("New", "جديدة", s.New, "fa-solid fa-circle-plus"),
        new("AwaitingSupplier", "بانتظار المورد", s.AwaitingSupplier, "fa-solid fa-user-clock", "is-warning"),
        new("Ordered", "تم طلبها", s.Ordered, "fa-solid fa-paper-plane", "is-info"),
        new("DueToday", "تصل اليوم", s.DueToday, "fa-solid fa-truck-fast", "is-info"),
        new("Overdue", "متأخرة", s.Overdue, "fa-solid fa-triangle-exclamation", "is-danger")
    ];

    private Task ApplyFiltersAsync() => LoadAsync(resetPage: true);

    private async Task ClearFiltersAsync()
    {
        _filter.Search = null;
        _filter.RequestDateFrom = null;
        _filter.RequestDateTo = null;
        _filter.Status = string.Empty;
        _filter.SupplierId = string.Empty;
        _filter.SupplierDisplay = null;
        _filter.ProductType = string.Empty;
        _filter.WarehouseId = string.Empty;
        _filter.WarehouseDisplay = null;
        _filter.CustomerOrderId = string.Empty;
        _filter.CustomerOrderDisplay = null;
        _filter.CustomerId = string.Empty;
        _filter.CustomerDisplay = null;
        _filter.ExpectedDeliveryDate = null;
        _filter.OverdueOnly = false;
        CustomerOrderId = null;
        Navigation.NavigateTo("/purchasing/customer-demand", replace: true);
        await LoadAsync(true);
    }

    private async Task TabSelectedAsync(string key)
    {
        _filter.Status = key;
        _filter.OverdueOnly = false;
        await LoadAsync(true);
    }

    private async Task MetricSelectedAsync(string key)
    {
        _filter.Status = key;
        _filter.OverdueOnly = key == "Overdue";
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

    private Task OpenAsync(UiDemandTrackingRowModel row) => OpenLineIdAsync(row.LineId);

    private async Task OpenLineIdAsync(Guid id)
    {
        State.IsBusy = true;
        State.Error = null;
        try
        {
            State.Selected = await Tracking.GetDetailsAsync(id);
            _selected = State.Selected is null ? null : SalesWorkflowUiMapper.ToUi(State.Selected);
            _history = [];
            _openedLineId = _selected?.LineId;
        }
        catch (ApiClientException ex) { State.Error = ex.Error.Message; }
        catch { State.Error = "تعذر تحميل تفاصيل النقص."; }
        finally { State.IsBusy = false; }
    }

    private Task CloseDetailsAsync()
    {
        State.Selected = null;
        _selected = null;
        _history = [];
        _openedLineId = null;
        return Task.CompletedTask;
    }

    private async Task AssignSupplierAsync()
    {
        if (_selected is null) return;
        await MutateAsync(async () =>
        {
            var dto = await Tracking.AssignSupplierAsync(_selected.LineId,
                new AssignCustomerDemandSupplierRequest(ParseGuid(_selected.PreferredSupplierId), _selected.RowVersion));
            ApplyDetails(dto, "تم حفظ المورد المفضل.");
        });
    }

    private async Task ScheduleAsync()
    {
        if (_selected is null) return;
        await MutateAsync(async () =>
        {
            var dto = await Tracking.ScheduleAsync(_selected.LineId,
                new ScheduleCustomerDemandRequest(_selected.ScheduledAt, _selected.RowVersion));
            ApplyDetails(dto, "تم حفظ موعد الطلب/المتابعة.");
        });
    }

    private async Task CreatePurchaseOrderAsync()
    {
        if (_selected is null || !Guid.TryParse(_selected.PreferredSupplierId, out var supplierId)) return;
        await MutateAsync(async () =>
        {
            var poId = await Tracking.CreatePurchaseOrderAsync(_selected.LineId,
                new CreateCustomerDemandPurchaseOrderRequest(supplierId, null, _selected.ExpectedDeliveryDateInput, _selected.RowVersion));
            State.Success = poId.HasValue ? "تم إنشاء/ربط أمر الشراء بنجاح." : "تم تنفيذ العملية.";
            await ReloadSelectedAndPageAsync();
        });
    }

    private async Task ResourceRemainingAsync()
    {
        if (_selected is null || !Guid.TryParse(_selected.ResupplySupplierId, out var supplierId)) return;
        await MutateAsync(async () =>
        {
            var poId = await Tracking.ResourceRemainingAsync(_selected.LineId,
                new ResourceCustomerDemandRemainingRequest(
                    supplierId,
                    _selected.ResupplyScheduledAt,
                    _selected.ExpectedDeliveryDateInput,
                    _selected.RowVersion));
            State.Success = poId.HasValue ? "تمت إعادة توجيه الكمية المتبقية مع الحفاظ على سجل المورد السابق." : "تم تنفيذ إعادة التوريد.";
            await ReloadSelectedAndPageAsync();
        });
    }

    private async Task LoadHistoryAsync()
    {
        if (_selected is null || !Guid.TryParse(_selected.PreferredSupplierId, out var supplierId)) return;
        State.IsBusy = true;
        State.Error = null;
        try
        {
            var page = await Tracking.GetSupplierHistoryAsync(new SupplierPurchaseHistoryQueryRequest
            {
                SupplierId = supplierId,
                ProductVariantId = _selected.ProductVariantId,
                PageNumber = 1,
                PageSize = 20
            });
            _history = page.Items.Select(x => new UiSupplierHistoryRowModel(
                x.PurchaseOrderCode,
                x.OrderDate.ToString("yyyy-MM-dd"),
                string.IsNullOrWhiteSpace(x.ProductCode) ? x.ProductName : $"{x.ProductCode} - {x.ProductName}",
                x.OrderedQuantity,
                x.AcceptedQuantity,
                x.ExpectedDeliveryDate?.ToString("yyyy-MM-dd"),
                x.ReceiptCode,
                x.ReceiptDate?.ToString("yyyy-MM-dd"),
                x.UnitPrice,
                x.ActualUnitCost)).ToArray();
        }
        catch (ApiClientException ex) { State.Error = ex.Error.Message; }
        catch { State.Error = "تعذر تحميل سجل المورد."; }
        finally { State.IsBusy = false; }
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

    private void ApplyDetails(CustomerDemandTrackingDetailsDto? dto, string success)
    {
        if (dto is null) return;
        State.Selected = dto;
        _selected = SalesWorkflowUiMapper.ToUi(dto);
        State.Success = success;
        _ = InvokeAsync(() => LoadAsync());
    }

    private async Task ReloadSelectedAndPageAsync()
    {
        if (_selected is not null)
        {
            var dto = await Tracking.GetDetailsAsync(_selected.LineId);
            State.Selected = dto;
            _selected = dto is null ? null : SalesWorkflowUiMapper.ToUi(dto);
        }
        await LoadAsync();
    }

    private Task OpenOrderAsync(Guid id)
    {
        Navigation.NavigateTo($"/sales/order-tracking?orderId={id:D}");
        return Task.CompletedTask;
    }

    private Task OpenPurchaseOrderAsync(Guid id)
    {
        Navigation.NavigateTo($"/purchases/orders?purchaseOrderId={id:D}");
        return Task.CompletedTask;
    }



    private async Task<IReadOnlyList<UiLookupItem>> SearchCustomersAsync(string search, CancellationToken ct)
    {
        var rows = await Sales.SearchCustomersAsync(string.IsNullOrWhiteSpace(search) ? null : search, 20, ct);
        return rows.Where(x => x.IsActive).Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.CustomerCode} - {x.NameAr}", x.Mobile ?? x.Phone, "fa-solid fa-user")).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchOrdersAsync(string search, CancellationToken ct)
    {
        var rows = await Sales.SearchCustomerOrdersAsync(ParseGuid(_filter.CustomerId), string.IsNullOrWhiteSpace(search) ? null : search, 20, ct);
        return rows.Select(x => new UiLookupItem(x.Id.ToString("D"), x.OrderCode, SalesWorkflowUiMapper.OrderStatus(x.Status), "fa-solid fa-clipboard-list")).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchSuppliersAsync(string search, CancellationToken ct)
    {
        var rows = await Accounting.LookupSuppliersAsync(string.IsNullOrWhiteSpace(search) ? null : search, ct) ?? [];
        return rows.Where(x => x.IsActive).Take(20)
            .Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.SupplierCode} - {x.NameAr}", null, "fa-solid fa-truck-field"))
            .ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchWarehousesAsync(string search, CancellationToken ct)
    {
        var page = await Inventory.GetWarehousesAsync(new PageRequest { PageNumber = 1, PageSize = 30, Search = string.IsNullOrWhiteSpace(search) ? null : search, SortBy = "Code" }, ct);
        return page.Items.Where(x => x.IsActive)
            .Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", null, "fa-solid fa-warehouse"))
            .ToArray();
    }

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
