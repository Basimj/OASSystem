using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Sales.Checkout.Services;
using OAS.Client.Sales.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Checkout;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Sales;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Sales.Checkout.Pages;

public partial class DeliverySettlementPage : ComponentBase
{
    [Inject] private ISalesCheckoutClientService Checkout { get; set; } = default!;
    [Inject] private ISalesClientService Sales { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;

    [Parameter] public Guid OrderId { get; set; }

    private UiDeliverySettlementModel _model = new();
    private bool _busy;
    private string _idempotencyKey = Guid.NewGuid().ToString("N");
    private Guid _orderCurrencyId;
    private Guid? _loadedOrderId;

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedOrderId != OrderId)
            await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _busy = true;
        try
        {
            var context = await Checkout.GetCheckoutContextAsync(OrderId);
            if (context is null)
            {
                Snackbar.Error("تعذر تحميل بيانات التسليم.");
                return;
            }
            _orderCurrencyId = context.Order.CurrencyId;
            _model = new UiDeliverySettlementModel
            {
                OrderId = OrderId,
                OrderCode = context.Order.OrderCode,
                CustomerText = $"{context.Customer.CustomerCode} - {context.Customer.NameAr}",
                InvoiceCode = context.InvoiceCode,
                CurrencyId = context.Order.CurrencyId.ToString("D"),
                CurrencyCode = context.Order.CurrencyCodeSnapshot,
                Total = context.PaymentSummary.TotalAmount,
                Paid = context.PaymentSummary.PaidAmount,
                AppliedAdvances = context.PaymentSummary.AppliedAdvanceAmount,
                Outstanding = context.PaymentSummary.OutstandingAmount,
                PaymentPlan = context.Order.PaymentPlan.ToString(),
                RowVersion = context.Order.RowVersion
            };
            if (_model.Outstanding > 0m && _model.PaymentPlan != "AccountCredit")
                _model.PaymentLines.Add(NewPayment(_model.Outstanding));
            _loadedOrderId = OrderId;
            _idempotencyKey = Guid.NewGuid().ToString("N");
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _busy = false; }
    }

    private async Task DeliverAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (!TryBuildPayments(out var payments, out var paymentError))
            {
                Snackbar.Error(paymentError!);
                return;
            }

            var call = await Checkout.DeliverResultAsync(OrderId,
                new DeliverCustomerOrderRequest(payments, _model.RowVersion, _idempotencyKey));
            if (!call.Succeeded)
            {
                if (call.Error is not null) ApiFeedback.Show(call.Error); else ApiFeedback.ShowUnexpected();
                return;
            }

            var result = call.Value;
            var successMessage = result is null ? "تم التسليم بنجاح." : $"تم تسليم الطلب {result.OrderCode} بنجاح.";
            Snackbar.Success(successMessage);
            _idempotencyKey = Guid.NewGuid().ToString("N");
            await LoadAsync();
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _busy = false; }
    }

    private bool TryBuildPayments(
        out IReadOnlyList<CheckoutPaymentLineRequest> payments,
        out string? error)
    {
        var result = new List<CheckoutPaymentLineRequest>();
        foreach (var line in _model.PaymentLines.Where(x => x.Amount > 0m))
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
                method, currencyId, line.Amount,
                ParseGuid(line.CashAccountId), ParseGuid(line.BankAccountId), line.ReferenceNumber,
                line.ReferenceDate, line.Description)
            { SettlementAccountId = ParseGuid(line.SettlementAccountId) });
        }

        payments = result;
        error = null;
        return true;
    }

    private UiCheckoutPaymentLineModel NewPayment(decimal amount) => new()
    {
        PaymentMethod = "Cash",
        CurrencyId = _orderCurrencyId.ToString("D"),
        CurrencyDisplay = _model.CurrencyCode,
        Amount = amount
    };

    private Task ModelChangedAsync() => Task.CompletedTask;

    private async Task<IReadOnlyList<UiLookupItem>> SearchCurrenciesAsync(string search, CancellationToken ct)
    {
        var rows = await Sales.SearchCurrenciesAsync(DateOnly.FromDateTime(DateTime.Today), search, 20, ct);
        return rows.Where(x => x.IsActive)
            .Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", x.Symbol, "fa-solid fa-coins"))
            .ToArray();
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
        return page.Items.Where(x => x.IsActive && x.IsPostingAccount)
            .Select(x => new UiLookupItem(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}", null, "fa-solid fa-book"))
            .ToArray();
    }

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
