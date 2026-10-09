using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
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
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Sales.Checkout.Pages;

public partial class SalesCheckoutPage : ComponentBase
{
    [Inject] private ISalesCheckoutClientService Checkout { get; set; } = default!;
    [Inject] private ISalesClientService Sales { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private SalesCheckoutState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;

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
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { Snackbar.Error("تعذر تحميل بيانات Checkout."); }
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
            {
                Snackbar.Error("خطة السداد غير صحيحة.");
                return;
            }

            if (!TryBuildPayments(_model.PaymentLines, out var payments, out var paymentError))
            {
                Snackbar.Error(paymentError!);
                return;
            }

            if (!ValidatePaymentPlan(paymentPlan, payments, _model.CurrencyId, _model.OutstandingAmount, out var planError))
            {
                Snackbar.Error(planError!);
                return;
            }

            var request = new CheckoutCustomerOrderRequest(
                _model.OrderId,
                paymentPlan,
                payments,
                _model.Shortages.Select(x => new CheckoutSupplierScheduleRequest(
                    x.CustomerOrderLineId,
                    ParseGuid(x.PreferredSupplierId),
                    x.ScheduledOrderAtUtc,
                    x.RequiredDate)).ToArray(),
                _model.RowVersion,
                State.IdempotencyKey);

            var call = await Checkout.CheckoutResultAsync(_model.OrderId, request);
            if (!call.Succeeded)
            {
                if (call.Error is not null) ApiFeedback.Show(call.Error); else ApiFeedback.ShowUnexpected();
                return;
            }

            var result = call.Value;
            var orderId = _model.OrderId;
            await LoadAsync(orderId);
            State.SetResult(result);
            if (result is not null)
                Snackbar.Success(string.IsNullOrWhiteSpace(result.InvoiceCode)
                    ? $"تم تنفيذ العملية للطلب {result.OrderCode}."
                    : $"تم تنفيذ العملية للطلب {result.OrderCode} وإنشاء الفاتورة {result.InvoiceCode}.");
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { State.IsBusy = false; }
    }

    private static bool TryBuildPayments(
        IEnumerable<UiCheckoutPaymentLineModel> lines,
        out IReadOnlyList<CheckoutPaymentLineRequest> payments,
        out string? error)
    {
        var result = new List<CheckoutPaymentLineRequest>();
        foreach (var line in lines.Where(x => x.Amount > 0m))
        {
            if (!Enum.TryParse<PaymentMethod>(line.PaymentMethod, out var method))
            {
                payments = [];
                error = "طريقة دفع غير صحيحة.";
                return false;
            }

            if (!Guid.TryParse(line.CurrencyId, out var currencyId))
            {
                payments = [];
                error = "يجب تحديد عملة الدفعة.";
                return false;
            }

            if (method == PaymentMethod.Cash && !ParseGuid(line.CashAccountId).HasValue)
            {
                payments = [];
                error = "يجب اختيار الصندوق للدفع النقدي.";
                return false;
            }

            if ((method is PaymentMethod.Card or PaymentMethod.BankTransfer or PaymentMethod.Cheque) && !ParseGuid(line.BankAccountId).HasValue)
            {
                payments = [];
                error = "يجب اختيار الحساب البنكي لطريقة الدفع المحددة.";
                return false;
            }

            if (method == PaymentMethod.Cheque && string.IsNullOrWhiteSpace(line.ReferenceNumber))
            {
                payments = [];
                error = "رقم الشيك مطلوب.";
                return false;
            }

            if (method == PaymentMethod.Other && !ParseGuid(line.SettlementAccountId).HasValue)
            {
                payments = [];
                error = "يجب تحديد حساب التسوية لطريقة الدفع الأخرى.";
                return false;
            }

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

        payments = result;
        error = null;
        return true;
    }

    private static bool ValidatePaymentPlan(
        SalesPaymentPlan plan,
        IReadOnlyList<CheckoutPaymentLineRequest> payments,
        string orderCurrencyIdText,
        decimal outstandingAmount,
        out string? error)
    {
        const decimal tolerance = 0.0001m;
        var paid = payments.Sum(x => x.Amount);
        var orderCurrencyId = ParseGuid(orderCurrencyIdText);
        var comparable = orderCurrencyId.HasValue && payments.All(x => x.CurrencyId == orderCurrencyId.Value);
        switch (plan)
        {
            case SalesPaymentPlan.FullNow:
                if (payments.Count == 0)
                {
                    error = "الدفع الكامل يتطلب دفعات تغطي كامل المبلغ المستحق.";
                    return false;
                }
                if (comparable && Math.Abs(paid - outstandingAmount) > tolerance)
                {
                    error = paid > outstandingAmount + tolerance
                        ? "مبلغ الدفعة يتجاوز كامل المبلغ المستحق."
                        : "الدفع الكامل يتطلب دفعات تغطي كامل المبلغ المستحق.";
                    return false;
                }
                break;
            case SalesPaymentPlan.PartialNow:
                if (payments.Count == 0 || paid <= tolerance)
                {
                    error = "أدخل مبلغ الدفع الجزئي أولًا.";
                    return false;
                }
                if (comparable && paid >= outstandingAmount - tolerance)
                {
                    error = paid > outstandingAmount + tolerance
                        ? "مبلغ الدفعة يتجاوز كامل المبلغ المستحق."
                        : "المبلغ المدخل يغطي كامل المستحق. اختر الدفع الكامل.";
                    return false;
                }
                break;
            case SalesPaymentPlan.PayOnPickup:
            case SalesPaymentPlan.AccountCredit:
                if (payments.Count != 0)
                {
                    error = plan == SalesPaymentPlan.PayOnPickup
                        ? "الدفع عند الاستلام لا يقبل دفعات الآن."
                        : "البيع الآجل لا يقبل دفعات عند تنفيذ Checkout.";
                    return false;
                }
                break;
            default:
                error = "خطة السداد غير صحيحة.";
                return false;
        }

        error = null;
        return true;
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
