using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Services;
using OAS.Client.Sales.Checkout.Services;
using OAS.Client.Sales.Checkout.State;
using OAS.Client.Sales.Mapping;
using OAS.Client.Sales.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Sales;

namespace OAS.Client.Sales.Checkout.Pages;

public partial class SalesCheckoutPage : ComponentBase
{
    [Inject] private ISalesCheckoutClientService Checkout { get; set; } = default!;
    [Inject] private ISalesClientService Sales { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private SalesCheckoutState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter, SupplyParameterFromQuery(Name = "orderId")] public Guid? OrderId { get; set; }

    private readonly UiSalesCheckoutSelectionModel _selection = new();
    private UiSalesCheckoutModel? _model;
    private Guid? _loadedOrderId;

    protected override async Task OnParametersSetAsync()
    {
        if (OrderId.HasValue && OrderId != _loadedOrderId)
        {
            _selection.OrderId = OrderId.Value.ToString("D");
            await LoadAsync(OrderId.Value);
        }
    }

    private Task OpenOrderEditorAsync(){Navigation.NavigateTo("/sales/customer-orders");return Task.CompletedTask;}

    private async Task OrderChangedAsync(string? value)
    {
        _selection.OrderId = value ?? string.Empty;
        State.ClearMessages();
        if (!Guid.TryParse(value, out var id))
        {
            _model = null;
            State.SetContext(null);
            return;
        }
        await LoadAsync(id);
    }

    private async Task LoadAsync(Guid orderId)
    {
        State.IsBusy = true;
        State.Error = null;
        try
        {
            var context = await Checkout.GetCheckoutContextAsync(orderId);
            State.SetContext(context);
            _model = context is null ? null : SalesWorkflowUiMapper.ToUi(context);
            _loadedOrderId = context?.Order.Id;
            if (_model is not null)
            {
                _selection.OrderId = _model.OrderId.ToString("D");
                _selection.OrderDisplay = $"{_model.OrderCode} - {_model.CustomerName}";
                EnsureInitialPaymentLine();
            }
        }
        catch (ApiClientException ex) { State.Error = ex.Error.Message; }
        catch { State.Error = "تعذر تحميل بيانات Checkout."; }
        finally { State.IsBusy = false; }
    }

    private Task RefreshAsync() => _model is null ? Task.CompletedTask : LoadAsync(_model.OrderId);

    private Task PaymentPlanChangedAsync(string value)
    {
        if (_model is null) return Task.CompletedTask;
        _model.PaymentPlan = value;
        if (value is "PayOnPickup" or "AccountCredit") _model.PaymentLines.Clear();
        else EnsureInitialPaymentLine();
        return Task.CompletedTask;
    }

    private Task AddPaymentLineAsync()
    {
        if (_model is null) return Task.CompletedTask;
        _model.PaymentLines.Add(NewPaymentLine());
        return Task.CompletedTask;
    }

    private Task RemovePaymentLineAsync(UiCheckoutPaymentLineModel line)
    {
        _model?.PaymentLines.Remove(line);
        return Task.CompletedTask;
    }

    private Task ModelChangedAsync() => Task.CompletedTask;

    private async Task CheckoutAsync()
    {
        if (_model is null) return;
        State.IsBusy = true;
        State.Error = null;
        State.Success = null;
        try
        {
            if (!Enum.TryParse<SalesPaymentPlan>(_model.PaymentPlan, out var paymentPlan))
                throw new InvalidOperationException("خطة السداد غير صحيحة.");

            var request = new CheckoutCustomerOrderRequest(
                _model.OrderId,
                paymentPlan,
                BuildPayments(_model.PaymentLines),
                _model.Shortages.Select(x => new CheckoutSupplierScheduleRequest(
                    x.CustomerOrderLineId,
                    ParseGuid(x.PreferredSupplierId),
                    x.ScheduledOrderAtUtc,
                    x.RequiredDate)).ToArray(),
                _model.RowVersion,
                State.IdempotencyKey);

            var result = await Checkout.CheckoutAsync(_model.OrderId, request);
            var orderId = _model.OrderId;
            await LoadAsync(orderId);
            State.SetResult(result);
        }
        catch (ApiClientException ex) { State.Error = ex.Error.Message; }
        catch (Exception ex) { State.Error = ex.Message; }
        finally { State.IsBusy = false; }
    }

    private IReadOnlyList<CheckoutPaymentLineRequest> BuildPayments(IEnumerable<UiCheckoutPaymentLineModel> lines)
    {
        var result = new List<CheckoutPaymentLineRequest>();
        foreach (var line in lines.Where(x => x.Amount > 0m))
        {
            if (!Enum.TryParse<PaymentMethod>(line.PaymentMethod, out var method))
                throw new InvalidOperationException("طريقة دفع غير صحيحة.");
            if (!Guid.TryParse(line.CurrencyId, out var currencyId))
                throw new InvalidOperationException("يجب تحديد عملة الدفعة.");

            result.Add(new CheckoutPaymentLineRequest(
                method,
                currencyId,
                line.Amount,
                ParseGuid(line.CashAccountId),
                ParseGuid(line.BankAccountId),
                line.ReferenceNumber,
                line.ReferenceDate,
                line.Description)
            { SettlementAccountId = ParseGuid(line.SettlementAccountId) });
        }
        return result;
    }

    private void EnsureInitialPaymentLine()
    {
        if (_model is null || _model.PaymentPlan is "PayOnPickup" or "AccountCredit" || _model.PaymentLines.Count > 0) return;
        _model.PaymentLines.Add(NewPaymentLine());
    }

    private UiCheckoutPaymentLineModel NewPaymentLine() => new()
    {
        PaymentMethod = "Cash",
        CurrencyId = _model?.CurrencyId ?? string.Empty,
        CurrencyDisplay = _model?.CurrencyCode,
        Amount = _model?.PaymentPlan == "FullNow" ? Math.Max(0m, _model.OutstandingAmount) : 0m
    };

    private async Task<IReadOnlyList<UiLookupItem>> SearchOrdersAsync(string search, CancellationToken ct)
    {
        var rows = await Sales.SearchCustomerOrdersAsync(null, string.IsNullOrWhiteSpace(search) ? null : search, 20, ct);
        return rows
            .Where(x => x.Status is not CustomerOrderStatus.Completed and not CustomerOrderStatus.Cancelled)
            .Select(x => new UiLookupItem(x.Id.ToString("D"), x.OrderCode, $"{SalesWorkflowUiMapper.OrderStatus(x.Status)} • {x.TotalAmount:N2} {x.CurrencyCode}", "fa-solid fa-clipboard-list"))
            .ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchSuppliersAsync(string search, CancellationToken ct)
    {
        var rows = await Accounting.LookupSuppliersAsync(string.IsNullOrWhiteSpace(search) ? null : search, ct) ?? [];
        return rows.Where(x => x.IsActive).Take(20).Select(x => new UiLookupItem(x.Id.ToString("D"), x.NameAr, x.SupplierCode, "fa-solid fa-truck-field")).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCurrenciesAsync(string search, CancellationToken ct)
    {
        if (_model is null) return [];
        var rows = await Sales.SearchCurrenciesAsync(DateOnly.FromDateTime(DateTime.Today), search, 20, ct);
        return rows.Where(x => x.IsActive).Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", x.Symbol, "fa-solid fa-coins")).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchCashAccountsAsync(UiCheckoutPaymentLineModel line, string search, CancellationToken ct)
    {
        var currencyId = ParseGuid(line.CurrencyId); if (!currencyId.HasValue) return [];
        var rows = await Sales.SearchCashAccountsAsync(currencyId.Value, search, 20, ct);
        return rows.Where(x => x.IsActive).Select(x => new UiLookupItem(x.Id.ToString("D"), x.Name, x.Code, "fa-solid fa-vault")).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchBankAccountsAsync(UiCheckoutPaymentLineModel line, string search, CancellationToken ct)
    {
        var currencyId = ParseGuid(line.CurrencyId); if (!currencyId.HasValue) return [];
        var rows = await Sales.SearchBankAccountsAsync(currencyId.Value, search, 20, ct);
        return rows.Where(x => x.IsActive).Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.BankName} - {x.AccountName}", x.AccountNumber, "fa-solid fa-building-columns")).ToArray();
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchSettlementAccountsAsync(string search, CancellationToken ct)
    {
        var page = await Accounting.GetAccountsPageAsync(new PageRequest { PageNumber = 1, PageSize = 20, Search = string.IsNullOrWhiteSpace(search) ? null : search }, ct);
        return page.Items.Where(x => x.IsActive && x.IsPostingAccount).Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", null, "fa-solid fa-book")).ToArray();
    }

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
