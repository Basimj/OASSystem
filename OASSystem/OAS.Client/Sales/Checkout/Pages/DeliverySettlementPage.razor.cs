using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Services;
using OAS.Client.Sales.Checkout.Services;
using OAS.Client.Sales.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Checkout;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Sales;

namespace OAS.Client.Sales.Checkout.Pages;

public partial class DeliverySettlementPage : ComponentBase
{
    [Inject] private ISalesCheckoutClientService Checkout { get; set; } = default!;
    [Inject] private ISalesClientService Sales { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;

    [Parameter] public Guid OrderId { get; set; }

    private UiDeliverySettlementModel _model = new();
    private bool _busy;
    private string? _error;
    private string? _success;
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
        _error = null;
        try
        {
            var context = await Checkout.GetCheckoutContextAsync(OrderId);
            if (context is null) throw new InvalidOperationException("تعذر تحميل بيانات التسليم.");
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
        catch (ApiClientException ex) { _error = ex.Error.Message; }
        catch (Exception ex) { _error = ex.Message; }
        finally { _busy = false; }
    }

    private async Task DeliverAsync()
    {
        if (_busy) return;
        _busy = true;
        _error = null;
        _success = null;
        try
        {
            var result = await Checkout.DeliverAsync(OrderId,
                new DeliverCustomerOrderRequest(BuildPayments(), _model.RowVersion, _idempotencyKey));
            _success = result is null ? "تم التسليم بنجاح." : $"تم تسليم الطلب {result.OrderCode} بنجاح.";
            _idempotencyKey = Guid.NewGuid().ToString("N");
            await LoadAsync();
        }
        catch (ApiClientException ex) { _error = ex.Error.Message; }
        catch (Exception ex) { _error = ex.Message; }
        finally { _busy = false; }
    }

    private IReadOnlyList<CheckoutPaymentLineRequest> BuildPayments()
        => _model.PaymentLines.Where(x => x.Amount > 0m).Select(x =>
        {
            if (!Enum.TryParse<PaymentMethod>(x.PaymentMethod, out var method))
                throw new InvalidOperationException("طريقة دفع غير صحيحة.");
            if (!Guid.TryParse(x.CurrencyId, out var currencyId))
                throw new InvalidOperationException("يجب تحديد عملة الدفعة.");
            return new CheckoutPaymentLineRequest(method, currencyId, x.Amount,
                ParseGuid(x.CashAccountId), ParseGuid(x.BankAccountId), x.ReferenceNumber,
                x.ReferenceDate, x.Description)
            { SettlementAccountId = ParseGuid(x.SettlementAccountId) };
        }).ToArray();

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
