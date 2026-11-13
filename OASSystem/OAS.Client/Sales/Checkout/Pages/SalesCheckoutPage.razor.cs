using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter, SupplyParameterFromQuery(Name = "orderId")] public Guid? OrderId { get; set; }

    private readonly UiSalesCheckoutSelectionModel _selection = new();
    private UiSalesCheckoutModel? _model;
    private Guid? _loadedOrderId;
    private bool _paymentChanges;
    private int _previewGeneration;
    // A fixed "بيع جديد" tab plus per-order tabs, retaining unsent payment edits on tab switches.
    private readonly List<CheckoutTabSession> _orderTabs = [];
    private IReadOnlyList<UiCheckoutOrderTab> CheckoutOrderTabs =>
        _orderTabs.Select(x => new UiCheckoutOrderTab(x.OrderId, x.OrderCode)).ToArray();

    private sealed class CheckoutTabSession(Guid orderId, string orderCode,
        UiSalesCheckoutModel model, SalesCheckoutContextDto context)
    {
        public Guid OrderId { get; } = orderId;
        public string OrderCode { get; } = orderCode;
        public UiSalesCheckoutModel Model { get; set; } = model;
        public SalesCheckoutContextDto Context { get; set; } = context;
        public bool HasChanges { get; set; }
        public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!OrderId.HasValue || OrderId == _loadedOrderId) return;
        var cached = _orderTabs.FirstOrDefault(x => x.OrderId == OrderId.Value);
        if (cached is not null)
            ActivateTab(cached);
        else
            await LoadAsync(OrderId.Value);
    }

    private void StoreTabChanges()
    {
        var current = _orderTabs.FirstOrDefault(x => x.OrderId == _loadedOrderId);
        if (current is not null) current.HasChanges = _paymentChanges;
    }

    private void ActivateTab(CheckoutTabSession tab)
    {
        StoreTabChanges();
        ++_previewGeneration;
        _model = tab.Model;
        _loadedOrderId = tab.OrderId;
        _paymentChanges = tab.HasChanges;
        _selection.OrderId = tab.OrderId.ToString("D");
        _selection.OrderDisplay = $"{tab.OrderCode} - {tab.Model.CustomerName}";
        State.SetContext(tab.Context);
    }

    private Task ShowNewCheckoutTabAsync()
    {
        StoreTabChanges();
        ++_previewGeneration;
        _model = null;
        _loadedOrderId = null;
        _paymentChanges = false;
        _selection.OrderId = string.Empty;
        _selection.OrderDisplay = null;
        State.SetContext(null);
        Navigation.NavigateTo("/sales/checkout", replace: true);
        return Task.CompletedTask;
    }

    private Task SelectCheckoutTabAsync(Guid orderId)
    {
        var tab = _orderTabs.FirstOrDefault(x => x.OrderId == orderId);
        if (tab is null) return Task.CompletedTask;
        ActivateTab(tab);
        Navigation.NavigateTo($"/sales/checkout?orderId={orderId:D}", replace: true);
        return Task.CompletedTask;
    }

    private async Task CloseCheckoutTabAsync(Guid orderId)
    {
        var tab = _orderTabs.FirstOrDefault(x => x.OrderId == orderId);
        if (tab is null) return;
        if (orderId == _loadedOrderId)
            StoreTabChanges();
        if (tab.HasChanges && !await JS.InvokeAsync<bool>("confirm",
                "توجد دفعات أو تعديلات غير منفذة في هذا التبويب. هل تريد إغلاقه؟")) return;
        _orderTabs.Remove(tab);
        if (orderId == _loadedOrderId)
            await ShowNewCheckoutTabAsync();
    }

    private Task CollectInvoiceAsync()
    {
        if (_model?.InvoiceId is Guid invoiceId)
            Navigation.NavigateTo($"/sales/invoices?invoiceId={invoiceId:D}&collect=true");
        return Task.CompletedTask;
    }

    private async Task OpenOrderEditorAsync()
    {
        if (!await ConfirmDiscardAsync()) return;
        if (_model is not null)
            Navigation.NavigateTo($"/sales/customer-orders?orderId={_model.OrderId:D}");
    }

    private async Task BackToOrdersAsync()
    {
        if (!await ConfirmDiscardAsync()) return;
        Navigation.NavigateTo("/sales/customer-orders");
    }

    private async Task CancelCheckoutAsync()
    {
        if (!await ConfirmDiscardAsync()) return;
        if (_loadedOrderId.HasValue)
            _orderTabs.RemoveAll(x => x.OrderId == _loadedOrderId.Value);
        await ShowNewCheckoutTabAsync();
    }

    private Task ViewInvoiceAsync()
    {
        if (_model?.InvoiceId is Guid invoiceId)
            Navigation.NavigateTo($"/sales/invoices?invoiceId={invoiceId:D}");
        return Task.CompletedTask;
    }

    private async Task<bool> ConfirmDiscardAsync()
    {
        if (!_paymentChanges) return true;
        return await JS.InvokeAsync<bool>("confirm", "توجد دفعات أو تعديلات غير منفذة. هل تريد تجاهلها؟");
    }

    private async Task OrderChangedAsync(string? value)
    {
        if (value == _loadedOrderId?.ToString("D")) return;
        if (!Guid.TryParse(value, out var id))
        {
            await ShowNewCheckoutTabAsync();
            return;
        }
        var cached = _orderTabs.FirstOrDefault(x => x.OrderId == id);
        if (cached is not null)
            ActivateTab(cached);
        else
            await LoadAsync(id);
        Navigation.NavigateTo($"/sales/checkout?orderId={id:D}", replace: true);
    }

    private async Task LoadAsync(Guid orderId)
    {
        StoreTabChanges();
        State.IsBusy = true;
        State.Error = null;
        try
        {
            var context = await Checkout.GetCheckoutContextAsync(orderId);
            State.SetContext(context);
            _model = context is null ? null : SalesWorkflowUiMapper.ToUi(context);
            _loadedOrderId = context?.Order.Id;
            _paymentChanges = false;
            if (_model is not null && context is not null)
            {
                var existing = _orderTabs.FirstOrDefault(x => x.OrderId == orderId);
                if (existing is null)
                    _orderTabs.Add(new CheckoutTabSession(orderId, _model.OrderCode, _model, context));
                else
                {
                    existing.Model = _model;
                    existing.Context = context;
                    existing.HasChanges = false;
                }
                _selection.OrderId = _model.OrderId.ToString("D");
                _selection.OrderDisplay = $"{_model.OrderCode} - {_model.CustomerName}";
                if (!_model.InvoiceId.HasValue) EnsureInitialPaymentLine();
                await UpdatePreviewAsync();
            }
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { Snackbar.Error("تعذر تحميل بيانات إتمام البيع."); }
        finally { State.IsBusy = false; }
    }

    private async Task RefreshAsync()
    {
        if (_model is null || !await ConfirmDiscardAsync()) return;
        await LoadAsync(_model.OrderId);
    }

    private async Task PaymentPlanChangedAsync(string value)
    {
        if (_model is null || value == _model.PaymentPlan) return;
        if (value == "AccountCredit" && _model.PaymentLines.Any(x => x.Amount > 0m)
            && !await JS.InvokeAsync<bool>("confirm", "الانتقال للبيع الآجل سيمسح الدفعات المدخلة. هل تريد المتابعة؟"))
            return;
        _model.PaymentPlan = value;
        if (value == "AccountCredit") _model.PaymentLines.Clear();
        else EnsureInitialPaymentLine();
        _paymentChanges = true;
        await UpdatePreviewAsync();
    }

    private async Task AddPaymentLineAsync()
    {
        if (_model is null) return;
        // An added row is explicitly user-entered and must have a positive amount before checkout.
        _model.PaymentLines.Add(NewPaymentLine());
        _paymentChanges = true;
        await UpdatePreviewAsync();
    }

    private async Task ConfirmPaymentLineAsync(UiCheckoutPaymentLineModel line)
    {
        if (_model is null || _model.InvoiceId.HasValue || State.IsBusy) return;
        if (!_model.PaymentLines.Contains(line)) return;
        if (!TryBuildPayments([line], out _, out var error))
        {
            State.Error = error;
            return;
        }
        State.Error = null;
        line.IsConfirmed = true;
        _paymentChanges = true;
        await UpdatePreviewAsync();
        if (_model.PreviewMessage is not null || !_model.PreviewPaidNow.HasValue ||
            (_model.PreviewPaidBase.HasValue && _model.PreviewRequiredBase.HasValue &&
             _model.PreviewPaidBase.Value > _model.PreviewRequiredBase.Value + 0.0001m))
        {
            line.IsConfirmed = false;
            State.Error = _model.PreviewMessage ?? "مجموع الدفعات يتجاوز المبلغ المستحق، أو تعذرت معاينة الدفعة.";
            await UpdatePreviewAsync();
        }
    }

    private async Task RemovePaymentLineAsync(UiCheckoutPaymentLineModel line)
    {
        _model?.PaymentLines.Remove(line);
        _paymentChanges = true;
        await UpdatePreviewAsync();
    }

    private async Task ModelChangedAsync()
    {
        _paymentChanges = true;
        await UpdatePreviewAsync();
    }

    private async Task UpdatePreviewAsync()
    {
        if (_model is null) return;
        var model = _model;
        var generation = ++_previewGeneration;
        model.PreviewPaidNow = 0m;
        model.PreviewPaidBase = 0m;
        model.PreviewRequiredBase = null;
        model.PreviewRemaining = model.OutstandingAmount;
        model.PreviewMessage = null;
        // Previously posted invoice payments are loaded in PaymentSummary by the server.
        // Checkout must not collect another payment when an invoice already exists.
        if (model.InvoiceId.HasValue || model.PaymentPlan == "AccountCredit") return;
        var confirmed = model.PaymentLines.Where(x => x.IsConfirmed).ToArray();
        if (confirmed.Length == 0) return;
        if (!TryBuildPayments(confirmed, out var payments, out var error))
        {
            model.PreviewMessage = error;
            return;
        }
        try
        {
            var result = await Checkout.PreviewPaymentsAsync(model.OrderId, payments);
            if (!ReferenceEquals(model, _model) || generation != _previewGeneration || result is null) return;
            model.PreviewPaidNow = result.PaidInOrderCurrency;
            model.PreviewPaidBase = result.PaidBaseAmount;
            model.PreviewRequiredBase = result.RequiredBaseAmount;
            model.PreviewRemaining = result.RemainingInOrderCurrency;
        }
        catch
        {
            if (ReferenceEquals(model, _model) && generation == _previewGeneration)
            {
                model.PreviewPaidNow = null;
                model.PreviewPaidBase = null;
                model.PreviewMessage = "تعذرت معاينة سعر الصرف. أعد المحاولة قبل تأكيد الدفعة.";
            }
        }
    }

    private async Task CheckoutAsync()
    {
        if (_model is null || State.IsBusy) return;
        if (_model.InvoiceId.HasValue)
        {
            State.Error = "الفاتورة موجودة بالفعل. افتح الفاتورة بدل إعادة إتمام البيع.";
            return;
        }
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

            if (_model.PaymentLines.Any(x => !x.IsConfirmed))
            {
                State.Error = "توجد دفعات غير مؤكدة. أكد كل دفعة أو احذف السطر غير المطلوب قبل إتمام البيع.";
                Snackbar.Error(State.Error);
                return;
            }
            if (!TryBuildPayments(_model.PaymentLines, out var payments, out var paymentError))
            {
                State.Error = paymentError;
                Snackbar.Error(paymentError!);
                return;
            }

            await UpdatePreviewAsync();
            if (payments.Count > 0 && (!_model.PreviewPaidNow.HasValue || !_model.PreviewRemaining.HasValue))
            {
                State.Error = "تعذرت معاينة الدفعات بسعر الصرف المعتمد. أصلح بيانات الدفعة أو أعد المحاولة.";
                return;
            }
            if (!ValidatePaymentPlan(paymentPlan, payments, _model.OutstandingAmount,
                    _model.PreviewPaidBase, _model.PreviewRequiredBase, out var planError))
            {
                State.Error = planError;
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
                _orderTabs.First(x => x.OrderId == _model.OrderId).IdempotencyKey);

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
            var session = _orderTabs.FirstOrDefault(x => x.OrderId == orderId);
            if (session is not null)
            {
                session.IdempotencyKey = Guid.NewGuid().ToString("N");
                session.HasChanges = false;
            }
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
        foreach (var line in lines)
        {
            line.ValidationError = null;
            line.ValidationField = null;
            if (line.Amount <= 0m)
            {
                line.ValidationField = "amount";
                line.ValidationError = "يجب أن يكون مبلغ الدفعة أكبر من صفر. احذف السطر غير المطلوب.";
                payments = [];
                error = line.ValidationError;
                return false;
            }
            if (!Enum.TryParse<PaymentMethod>(line.PaymentMethod, out var method))
            {
                payments = [];
                error = "طريقة دفع غير صحيحة.";
                line.ValidationError = error;
                line.ValidationField = "method";
                return false;
            }

            if (!Guid.TryParse(line.CurrencyId, out var currencyId))
            {
                payments = [];
                error = "يجب تحديد عملة الدفعة.";
                line.ValidationError = error;
                line.ValidationField = "currency";
                return false;
            }

            if (method == PaymentMethod.Cash && !ParseGuid(line.CashAccountId).HasValue)
            {
                payments = [];
                error = "يجب اختيار الصندوق للدفع النقدي.";
                line.ValidationError = error;
                line.ValidationField = "cash";
                return false;
            }

            if ((method is PaymentMethod.Card or PaymentMethod.BankTransfer or PaymentMethod.Cheque) && !ParseGuid(line.BankAccountId).HasValue)
            {
                payments = [];
                error = "يجب اختيار الحساب البنكي لطريقة الدفع المحددة.";
                line.ValidationError = error;
                line.ValidationField = "bank";
                return false;
            }

            if (method == PaymentMethod.Cheque && string.IsNullOrWhiteSpace(line.ReferenceNumber))
            {
                payments = [];
                error = "رقم الشيك مطلوب.";
                line.ValidationError = error;
                line.ValidationField = "reference";
                return false;
            }

            if (method == PaymentMethod.Other && !ParseGuid(line.SettlementAccountId).HasValue)
            {
                payments = [];
                error = "يجب تحديد حساب التسوية لطريقة الدفع الأخرى.";
                line.ValidationError = error;
                line.ValidationField = "settlement";
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
        decimal outstandingAmount,
        decimal? serverPaidBase,
        decimal? serverRequiredBase,
        out string? error)
    {
        const decimal tolerance = 0.0001m;
        // Compare base-currency amounts, matching the accounting rule on the server.
        var paid = serverPaidBase ?? 0m;
        var required = serverRequiredBase ?? 0m;
        var comparable = serverPaidBase.HasValue && serverRequiredBase.HasValue;
        switch (plan)
        {
            case SalesPaymentPlan.FullNow:
                // When an existing advance covers the balance, do NOT create a zero-value voucher.
                if (outstandingAmount <= tolerance && payments.Count == 0) break;
                if (payments.Count == 0)
                {
                    error = "الدفع الكامل يتطلب دفعات تغطي كامل المبلغ المستحق.";
                    return false;
                }
                if (outstandingAmount <= tolerance)
                {
                    error = "الرصيد مغطى مسبقًا ولا يجوز إضافة دفعة أخرى.";
                    return false;
                }
                if (comparable && Math.Abs(paid - required) > tolerance)
                {
                    error = paid > required + tolerance
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
                if (comparable && paid >= required - tolerance)
                {
                    error = paid > required + tolerance
                        ? "مبلغ الدفعة يتجاوز كامل المبلغ المستحق."
                        : "المبلغ المدخل يغطي كامل المستحق. اختر الدفع الكامل.";
                    return false;
                }
                break;
            case SalesPaymentPlan.AccountCredit:
                if (payments.Count != 0)
                {
                    error = "البيع الآجل لا يقبل دفعات عند تنفيذ Checkout.";
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
        if (_model is null || _model.PaymentPlan == "AccountCredit" || _model.OutstandingAmount <= 0.0001m || _model.PaymentLines.Count > 0) return;
        _model.PaymentLines.Add(NewPaymentLine(prefillFullAmount: true));
    }

    private UiCheckoutPaymentLineModel NewPaymentLine(bool prefillFullAmount = false) => new()
    {
        PaymentMethod = "Cash",
        CurrencyId = _model?.CurrencyId ?? string.Empty,
        CurrencyDisplay = _model?.CurrencyCode,
        Amount = prefillFullAmount && _model?.PaymentPlan == "FullNow" ? Math.Max(0m, _model.OutstandingAmount) : 0m
    };

    private async Task<IReadOnlyList<UiLookupItem>> SearchOrdersAsync(string search, CancellationToken ct)
    {
        var rows = await Sales.SearchCustomerOrdersAsync(null, string.IsNullOrWhiteSpace(search) ? null : search, 50, ct, checkoutOnly: true);
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
