using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Sales.Checkout.Services;
using OAS.Client.Sales.Mapping;
using OAS.Client.Sales.OrderTracking.Services;
using OAS.Client.Sales.OrderTracking.State;
using OAS.Client.Sales.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.OrderOperations;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Sales;

namespace OAS.Client.Sales.OrderTracking.Pages;

public partial class CustomerOrderOperationsPage : ComponentBase
{
    [Inject] private ICustomerOrderOperationsClientService Operations { get; set; } = default!;
    [Inject] private ISalesCheckoutClientService Checkout { get; set; } = default!;
    [Inject] private ISalesClientService Sales { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IEmployeeClientService Employees { get; set; } = default!;
    [Inject] private CustomerOrderOperationsState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter, SupplyParameterFromQuery(Name = "orderId")] public Guid? OrderId { get; set; }

    private readonly UiOrderOperationsFilterModel _filter = new();
    private IReadOnlyList<UiWorkflowMetric> _metrics = [];
    private IReadOnlyList<UiOrderOperationsRowModel> _rows = [];
    private UiOrderOperationsDetailsModel? _selected;
    private UiDeliverySettlementModel? _delivery;
    private bool _deliveryBusy;
    private string? _deliveryError;
    private string _deliveryKey = Guid.NewGuid().ToString("N");
    private bool _initialLoaded;

    protected override async Task OnParametersSetAsync()
    {
        if (!_initialLoaded)
        {
            _initialLoaded = true;
            await LoadAsync(resetPage: true);
        }
        if (OrderId.HasValue && (_selected?.OrderId != OrderId.Value))
            await OpenOrderIdAsync(OrderId.Value);
    }

    private async Task LoadAsync(bool resetPage = false)
    {
        State.IsBusy = true; State.Error = null;
        try
        {
            State.Query = BuildQuery(resetPage ? 1 : Math.Max(1, State.Query.PageNumber));
            var pageTask = Operations.GetPageAsync(State.Query);
            var summaryTask = Operations.GetSummaryAsync(State.Query);
            await Task.WhenAll(pageTask, summaryTask);
            State.Page = pageTask.Result;
            State.Summary = summaryTask.Result;
            _rows = State.Page.Items.Select(SalesWorkflowUiMapper.ToUi).ToArray();
            _metrics = BuildMetrics(State.Summary);
        }
        catch (ApiClientException ex) { State.Error = ex.Error.Message; }
        catch { State.Error = "تعذر تحميل متابعة طلبات العملاء."; }
        finally { State.IsBusy = false; }
    }

    private CustomerOrderOperationsQueryRequest BuildQuery(int page) => new()
    {
        PageNumber = page,
        PageSize = State.Query.PageSize <= 0 ? 25 : State.Query.PageSize,
        Search = _filter.Search,
        OrderDateFrom = _filter.OrderDateFrom,
        OrderDateTo = _filter.OrderDateTo,
        Status = Enum.TryParse<CustomerOrderStatus>(_filter.Status, out var status) ? status : null,
        ProductType = Enum.TryParse<SalesLineType>(_filter.ProductType, out var productType) ? productType : null,
        WarehouseId = ParseGuid(_filter.WarehouseId),
        RequiresProduction = bool.TryParse(_filter.RequiresProduction, out var production) ? production : null,
        Availability = Enum.TryParse<CustomerOrderLineAvailabilityState>(_filter.Availability, out var availability) ? availability : null,
        RequiredDate = _filter.RequiredDate,
        OverdueOnly = _filter.OverdueOnly,
        TechnicianId = ParseGuid(_filter.TechnicianId),
        SortBy = "LastUpdatedAt",
        SortDirection = SortDirection.Descending
    };

    private static IReadOnlyList<UiWorkflowMetric> BuildMetrics(CustomerOrderOperationsSummaryDto? s) => s is null ? [] :
    [
        new("today","طلبات اليوم",s.TodayOrders,"fa-solid fa-calendar-day"),
        new("AwaitingStock","بانتظار مخزون",s.AwaitingStock,"fa-solid fa-box-open","is-warning"),
        new("ReadyForProduction","جاهزة للمعمل",s.ReadyForProduction,"fa-solid fa-flask","is-info"),
        new("InProduction","قيد التجهيز",s.InProduction,"fa-solid fa-gears","is-info"),
        new("ReadyForDelivery","جاهزة للتسليم",s.ReadyForDelivery,"fa-solid fa-box","is-success"),
        new("overdue","متأخرة",s.Overdue,"fa-solid fa-triangle-exclamation","is-danger")
    ];

    private Task ApplyFiltersAsync() => LoadAsync(resetPage: true);
    private async Task ClearFiltersAsync(){_filter.Search=null;_filter.OrderDateFrom=null;_filter.OrderDateTo=null;_filter.Status="";_filter.ProductType="";_filter.WarehouseId="";_filter.WarehouseDisplay=null;_filter.RequiresProduction="";_filter.Availability="";_filter.RequiredDate=null;_filter.TechnicianId="";_filter.TechnicianDisplay=null;_filter.OverdueOnly=false;await LoadAsync(true);}
    private async Task TabSelectedAsync(string key){_filter.Status=key;_filter.OverdueOnly=false;await LoadAsync(true);}
    private async Task MetricSelectedAsync(string key)
    {
        _filter.OverdueOnly = key == "overdue";
        _filter.Status = key is "today" or "overdue" ? "" : key;
        if(key=="today"){var today=DateOnly.FromDateTime(DateTime.Today);_filter.OrderDateFrom=today;_filter.OrderDateTo=today;}
        else if(key!="overdue"){_filter.OrderDateFrom=null;_filter.OrderDateTo=null;}
        await LoadAsync(true);
    }
    private async Task PreviousAsync(){if(State.Page.HasPreviousPage){State.Query=BuildQuery(State.Page.PageNumber-1);await LoadAsync();}}
    private async Task NextAsync(){if(State.Page.HasNextPage){State.Query=BuildQuery(State.Page.PageNumber+1);await LoadAsync();}}
    private Task OpenAsync(UiOrderOperationsRowModel row)=>OpenOrderIdAsync(row.Id);
    private async Task OpenOrderIdAsync(Guid id)
    {
        State.IsBusy=true;
        try{State.Selected=await Operations.GetDetailsAsync(id);_selected=State.Selected is null?null:SalesWorkflowUiMapper.ToUi(State.Selected);}
        catch(ApiClientException ex){State.Error=ex.Error.Message;}
        finally{State.IsBusy=false;}
    }
    private Task CloseDetailsAsync(){State.Selected=null;_selected=null;return Task.CompletedTask;}
    private Task OpenSupplyAsync(Guid id){Navigation.NavigateTo($"/purchasing/customer-demand?orderId={id:D}");return Task.CompletedTask;}
    private Task OpenJobAsync(Guid id){Navigation.NavigateTo($"/optical/jobs?jobId={id:D}");return Task.CompletedTask;}

    private async Task OpenDeliveryAsync(Guid orderId)
    {
        _deliveryError=null;_deliveryBusy=true;
        try
        {
            var context=await Checkout.GetCheckoutContextAsync(orderId);
            if(context is null) throw new InvalidOperationException("تعذر تحميل بيانات التسليم.");
            var customerText=$"{context.Customer.CustomerCode} - {context.Customer.NameAr}";
            _delivery=new UiDeliverySettlementModel
            {
                OrderId=orderId,OrderCode=context.Order.OrderCode,CustomerText=customerText,InvoiceCode=context.InvoiceCode,CurrencyId=context.Order.CurrencyId.ToString("D"),CurrencyCode=context.Order.CurrencyCodeSnapshot,
                Total=context.PaymentSummary.TotalAmount,Paid=context.PaymentSummary.PaidAmount,AppliedAdvances=context.PaymentSummary.AppliedAdvanceAmount,
                Outstanding=context.PaymentSummary.OutstandingAmount,PaymentPlan=context.Order.PaymentPlan.ToString(),RowVersion=context.Order.RowVersion
            };
            _deliveryKey=Guid.NewGuid().ToString("N");
            if(_delivery.Outstanding>0 && _delivery.PaymentPlan!="AccountCredit") _delivery.PaymentLines.Add(NewDeliveryPayment(context.Order.CurrencyId,context.Order.CurrencyCodeSnapshot,_delivery.Outstanding));
        }
        catch(ApiClientException ex){State.Error=ex.Error.Message;}
        catch(Exception ex){State.Error=ex.Message;}
        finally{_deliveryBusy=false;}
    }

    private Task AddDeliveryPaymentAsync(){if(_delivery is not null)_delivery.PaymentLines.Add(NewDeliveryPayment(ParseGuid(_delivery.CurrencyId)??Guid.Empty,_delivery.CurrencyCode,0m));return Task.CompletedTask;}
    private Task RemoveDeliveryPaymentAsync(UiCheckoutPaymentLineModel line){_delivery?.PaymentLines.Remove(line);return Task.CompletedTask;}
    private Task CloseDeliveryAsync(){_delivery=null;_deliveryError=null;return Task.CompletedTask;}
    private async Task DeliverAsync()
    {
        if(_delivery is null)return;_deliveryBusy=true;_deliveryError=null;
        try
        {
            var payments=BuildPayments(_delivery.PaymentLines);
            await Checkout.DeliverAsync(_delivery.OrderId,new DeliverCustomerOrderRequest(payments,_delivery.RowVersion,_deliveryKey));
            _delivery=null;_selected=null;State.Selected=null;await LoadAsync();
        }
        catch(ApiClientException ex){_deliveryError=ex.Error.Message;}
        catch(Exception ex){_deliveryError=ex.Message;}
        finally{_deliveryBusy=false;}
    }

    private static UiCheckoutPaymentLineModel NewDeliveryPayment(Guid currencyId,string currencyCode,decimal amount)=>new(){PaymentMethod="Cash",CurrencyId=currencyId==Guid.Empty?"":currencyId.ToString("D"),CurrencyDisplay=currencyCode,Amount=amount};
    private static Guid? ParseGuid(string? v)=>Guid.TryParse(v,out var id)?id:null;
    private static IReadOnlyList<CheckoutPaymentLineRequest> BuildPayments(IEnumerable<UiCheckoutPaymentLineModel> lines)=>lines.Where(x=>x.Amount>0).Select(x=>new CheckoutPaymentLineRequest(Enum.Parse<PaymentMethod>(x.PaymentMethod),Guid.Parse(x.CurrencyId),x.Amount,ParseGuid(x.CashAccountId),ParseGuid(x.BankAccountId),x.ReferenceNumber,x.ReferenceDate,x.Description){SettlementAccountId=ParseGuid(x.SettlementAccountId)}).ToArray();

    private async Task<IReadOnlyList<UiLookupItem>> SearchWarehousesAsync(string search,CancellationToken ct)
    {
        var rows=await Sales.SearchWarehousesAsync(null,string.IsNullOrWhiteSpace(search)?null:search,20,ct);
        return rows.Where(x=>x.IsActive).Select(x=>new UiLookupItem(x.Id.ToString("D"),$"{x.Code} - {x.NameAr}",null,"fa-solid fa-warehouse")).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchTechniciansAsync(string search,CancellationToken ct)
    {
        var page=await Employees.GetPageAsync(new PageRequest{PageNumber=1,PageSize=30,Search=string.IsNullOrWhiteSpace(search)?null:search,SortBy="EmployeeCode"},ct);
        return page.Items.Where(x=>x.IsActive&&x.IsTechnician).Select(x=>new UiLookupItem(x.Id.ToString("D"),$"{x.EmployeeCode} - {x.DisplayName}",x.JobTitleName,"fa-solid fa-user-gear")).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCurrenciesAsync(string search,CancellationToken ct){var rows=await Sales.SearchCurrenciesAsync(DateOnly.FromDateTime(DateTime.Today),search,20,ct);return rows.Where(x=>x.IsActive).Select(x=>new UiLookupItem(x.Id.ToString("D"),$"{x.Code} - {x.NameAr}",x.Symbol,"fa-solid fa-coins")).ToArray();}
    private async Task<IReadOnlyList<UiLookupItem>> SearchCashAccountsAsync(UiCheckoutPaymentLineModel line,string search,CancellationToken ct){var c=ParseGuid(line.CurrencyId);if(!c.HasValue)return[];var rows=await Sales.SearchCashAccountsAsync(c.Value,search,20,ct);return rows.Where(x=>x.IsActive).Select(x=>new UiLookupItem(x.Id.ToString("D"),x.Name,x.Code,"fa-solid fa-vault")).ToArray();}
    private async Task<IReadOnlyList<UiLookupItem>> SearchBankAccountsAsync(UiCheckoutPaymentLineModel line,string search,CancellationToken ct){var c=ParseGuid(line.CurrencyId);if(!c.HasValue)return[];var rows=await Sales.SearchBankAccountsAsync(c.Value,search,20,ct);return rows.Where(x=>x.IsActive).Select(x=>new UiLookupItem(x.Id.ToString("D"),$"{x.BankName} - {x.AccountName}",x.AccountNumber,"fa-solid fa-building-columns")).ToArray();}
    private async Task<IReadOnlyList<UiLookupItem>> SearchSettlementAccountsAsync(string search,CancellationToken ct){var page=await Accounting.GetAccountsPageAsync(new PageRequest{PageNumber=1,PageSize=20,Search=string.IsNullOrWhiteSpace(search)?null:search},ct);return page.Items.Where(x=>x.IsActive&&x.IsPostingAccount).Select(x=>new UiLookupItem(x.Id.ToString("D"),$"{x.Code} - {x.NameAr}",null,"fa-solid fa-book")).ToArray();}
}
