using System.Globalization;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.UiLib.Components.Accounting.Vouchers;
using Line = OAS.UiLib.Components.Accounting.Vouchers.UiVoucherSettlementLinesEditor.EditableSettlementLine;
using Method = OAS.UiLib.Components.Accounting.Vouchers.UiVoucherSettlementLinesEditor.PaymentMethodKind;

namespace OAS.Client.Accounting.Common;

internal static class PaymentVoucherFundsPreview
{
    internal static (Guid Id, bool Bank)[] Keys(IReadOnlyList<Line> lines) => lines
        .Select(Key).Where(x => x.HasValue).Select(x => x!.Value).Distinct()
        .OrderBy(x => x.Bank).ThenBy(x => x.Id).ToArray();

    internal static async Task<Dictionary<(Guid Id, bool Bank), PaymentVoucherBalanceDto?>> LoadAsync(
        OasApiClient api, IReadOnlyList<(Guid Id, bool Bank)> keys, DateOnly date, CancellationToken ct)
    {
        var balances = new Dictionary<(Guid Id, bool Bank), PaymentVoucherBalanceDto?>();
        foreach (var key in keys)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                balances[key] = await api.GetAsync<PaymentVoucherBalanceDto>(
                    $"api/accounting/payment-vouchers/balance?definitionId={key.Id:D}&isBank={key.Bank.ToString().ToLowerInvariant()}&date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}", ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { balances[key] = null; }
        }
        ct.ThrowIfCancellationRequested();
        return balances;
    }

    internal static void Clear(IReadOnlyList<Line> lines)
    {
        foreach (var line in lines)
        {
            line.AvailableFunds = null; line.RemainingFunds = null; line.FundsError = null; line.FundsCurrencyCode = null;
        }
    }

    internal static async Task<bool> RefreshAsync(OasApiClient api, IReadOnlyList<Line> lines, DateOnly date, CancellationToken ct = default, bool forPosting = false)
    {
        var balances = await LoadAsync(api, Keys(lines), date, ct);
        return Apply(lines, balances, forPosting);
    }

    internal static bool Apply(IReadOnlyList<Line> lines,
        IReadOnlyDictionary<(Guid Id, bool Bank), PaymentVoucherBalanceDto?> balances, bool forPosting = false)
    {
        Clear(lines);
        var groups = lines.Select(l => (Line: l, Key: Key(l))).Where(x => x.Key.HasValue)
            .GroupBy(x => x.Key!.Value).ToArray();
        var valid = true;
        foreach (var group in groups)
        {
            var key = group.Key;
            var members = group.Select(x => x.Line).ToArray();
            balances.TryGetValue(key, out var balance);
            string? error = null;
            decimal? available = balance?.AvailableAmount;
            decimal? remaining = null;
            if (balance is null || available is null || balance.DecimalPlaces is not byte decimals || decimals > 4)
                error = balance?.Error ?? "تعذر جلب الرصيد الدفتري. حدّث الرصيد وحاول مرة أخرى.";
            else if (members.Any(l => l.CurrencyId != balance.CurrencyId))
                error = "عملة السطر لا تطابق عملة حساب الصرف.";
            else if (members.Any(l => l.Amount <= 0 || l.Amount >= 1000000000000000m))
                error = "أدخل مبلغًا صالحًا وأكبر من صفر لكل بند.";
            else
            {
                var required = members.Sum(l => Math.Round(l.Amount, decimals, MidpointRounding.AwayFromZero));
                remaining = available.Value - required;
                if (remaining < 0)
                    error = $"مجموع مبالغ الصرف لهذا الحساب ({required:0.####}) يتجاوز الرصيد المتاح ({available:0.####}).";
            }
            if (forPosting && error is null && balance?.PostingError is string postingError) error = postingError;
            foreach (var line in members)
            {
                line.AvailableFunds = available; line.RemainingFunds = remaining;
                line.FundsCurrencyCode = balance?.CurrencyCode; line.FundsError = error;
            }
            valid &= error is null;
        }
        return valid;
    }

    private static (Guid Id, bool Bank)? Key(Line line) => line.PaymentMethod switch
    {
        Method.Cash when line.CashAccountId is Guid id => (id, false),
        Method.BankTransfer or Method.Card or Method.Cheque when line.BankAccountId is Guid id => (id, true),
        _ => null
    };
}
