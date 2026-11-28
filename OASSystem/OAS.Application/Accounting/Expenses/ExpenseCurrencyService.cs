using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Expenses;

public sealed class ExpenseCurrencyService(
    IReadRepository<CashAccount, Guid> cashAccounts,
    IReadRepository<BankAccount, Guid> bankAccounts,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    IExchangeRateResolver rates, ICurrencyRoundingService rounding) : IExpenseCurrencyService
{
    public async Task ApplyAsync(Expense expense, Guid? currencyId, decimal? expectedRate, CancellationToken ct)
    {
        Guid? selectedCurrency;
        if (expense.PaymentMethod == PaymentMethod.Cash && expense.CashAccountId is Guid cashId && expense.BankAccountId is null)
        {
            var cash = await cashAccounts.GetByIdAsync(cashId, ct) ?? throw new NotFoundException(nameof(CashAccount), cashId);
            if (!cash.IsActive) throw new ConflictException("cash_account_inactive", "الصندوق غير نشط.");
            OAS.Application.Accounting.CashAccounts.CashAccountMovementPolicy.EnsureAllowed(cash, false);
            selectedCurrency = cash.CurrencyId;
        }
        else if (expense.PaymentMethod != PaymentMethod.Cash && expense.BankAccountId is Guid bankId && expense.CashAccountId is null)
        {
            var bank = await bankAccounts.GetByIdAsync(bankId, ct) ?? throw new NotFoundException(nameof(BankAccount), bankId);
            if (!bank.IsActive) throw new ConflictException("bank_account_inactive", "الحساب البنكي غير نشط.");
            selectedCurrency = bank.CurrencyId;
        }
        else throw new ConflictException("payment_account_required", "حدد حساب الدفع المطابق لطريقة الدفع.");
        var config = await settings.GetByIdAsync(AccountingSettings.SingletonId, ct)
            ?? throw new ConflictException("accounting_settings_required", "حدد العملة الأساسية أولاً.");
        var baseCurrency = await currencies.GetByIdAsync(config.BaseCurrencyId, ct)
            ?? throw new ConflictException("base_currency_missing", "العملة الأساسية غير متاحة.");
        if (selectedCurrency is not Guid actualCurrency || (currencyId ?? baseCurrency.Id) != actualCurrency)
            throw new ConflictException("expense_currency_mismatch", "عملة المصروف يجب أن تطابق عملة حساب الدفع. راجع المبلغ قبل الحفظ.");
        var rate = await rates.ResolveAsync(actualCurrency, expense.ExpenseDate, cancellationToken: ct);
        if (expectedRate.HasValue && expectedRate.Value != rate.Rate)
            throw new ConflictException("expense_rate_changed", "تغير سعر الصرف. حدّث بيانات العملة قبل الحفظ.");
        if (expense.Amount >= 1000000000000000m || rate.Rate <= 0 || rate.Rate >= 100000000000m || Math.Round(rate.Rate, 8) != rate.Rate || rate.CurrencyDecimalPlaces > 4 || baseCurrency.DecimalPlaces > 4 ||
            rounding.Round(expense.Amount, rate.CurrencyDecimalPlaces) != expense.Amount)
            throw new ConflictException("expense_currency_precision", "المبلغ أو دقة العملة غير مدعومين في القيود الحالية (حتى أربع منازل عشرية).");
        var baseAmount = rounding.CalculateBaseAmount(expense.Amount, rate.Rate, rate.CurrencyDecimalPlaces, baseCurrency.DecimalPlaces);
        if (baseAmount <= 0 || baseAmount >= 1000000000000000m)
            throw new ConflictException("expense_base_amount_invalid", "القيمة الأساسية خارج النطاق المسموح.");
        expense.SetCurrencySnapshot(actualCurrency, rate.CurrencyCode, rate.CurrencyDecimalPlaces,
            baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces, baseAmount, rate.Rate, rate.RateDate, rate.RateType, rate.Source);
    }
}
