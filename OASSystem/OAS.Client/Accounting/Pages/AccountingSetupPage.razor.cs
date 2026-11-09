using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using OAS.Client.Accounting.Common;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.EmployeeAccounts;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.ExchangeRates;
using OAS.Contracts.Accounting.Settings;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Core.Enums;

namespace OAS.Client.Accounting.Pages;

public partial class AccountingSetupPage
{
    [Inject]
    private IAccountingClientService Accounting { get; set; } = default!;

    [Inject]
    private IEmployeeClientService Employees { get; set; } = default!;

    [Inject]
    private IApiFeedbackService ApiFeedback { get; set; } = default!;

    [Inject]
    private IUiSnackbarService Snackbar { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IUiDialogService Dialogs { get; set; } = default!;

    [Parameter] public string Section { get; set; } = "general";
    private string SectionTitle => Section switch
    {
        "cash" => "العميل النقدي", "receivables" => "ذمم العملاء",
        "exchange" => "فروقات العملات", "purchasing" => "حسابات المخزون والمشتريات",
        "currencies" => "العملات", "rates" => "أسعار الصرف",
        "employees" => "الربط المحاسبي للموظفين", _ => "الإعدادات الأساسية"
    };
    private CustomerReceivablesSettingsField? _standaloneReceivablesField;
    private async Task SaveStandaloneReceivablesAsync()
    {
        if (_standaloneReceivablesField is null || !_standaloneReceivablesField.IsLoaded)
        {
            Snackbar.Warning("لم تُحمّل إعدادات ذمم العملاء بعد.");
            return;
        }
        if (await _standaloneReceivablesField.SaveAsync())
            Snackbar.Success("تم حفظ الحساب الرئيسي لذمم العملاء.");
    }
    private string _activeSection = string.Empty;
    private bool _currencyEditorOpen;
    private bool _rateEditorOpen;
    private string? _settingsError;
    private string? _baseCurrencyError;
    private string? _currencyCodeError;
    private string? _currencyNameError;
    private string? _currencyDecimalsError;
    private string? _rateCurrencyError;
    private string? _rateDateError;
    private string? _currencyError;
    private string? _rateError;
    private string? _rateFilterError;
    private string _currencySearch = string.Empty;
    private string _currencyStatusFilter = "all";
    private string _rateSearch = string.Empty;
    private string _rateStatusFilter = "all";
    private Guid? _rateFilterCurrencyId;
    private DateOnly? _rateDateFrom;
    private DateOnly? _rateDateTo;
    private int _ratePageNumber = 1;
    private long _rateTotalCount;
    private const int RatePageSize = 30;
    private string _employeeAccountSearch = string.Empty;
    private int _employeeAccountPageNumber = 1;
    private long _employeeAccountTotalCount;
    private const int EmployeeAccountsPageSize = 30;

    private static IReadOnlyList<UiSelectOption> CurrencyStatusFilterOptions { get; } =
    [
        new("all", "جميع العملات"), new("active", "نشطة"), new("inactive", "معطلة")
    ];
    private static IReadOnlyList<UiSelectOption> RateStatusFilterOptions { get; } =
    [
        new("all", "جميع الحالات"), new("active", "نشطة"), new("inactive", "معطلة")
    ];
    private IReadOnlyList<UiSelectOption> RateFilterCurrencyOptions =>
        _currencies.Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} — {x.NameAr}")).ToArray();
    private IEnumerable<CurrencyDto> FilteredCurrencies => _currencies.Where(c =>
        (_currencyStatusFilter == "all" ||
         (_currencyStatusFilter == "active" ? c.IsActive : !c.IsActive)) &&
        (string.IsNullOrWhiteSpace(_currencySearch) ||
         c.Code.Contains(_currencySearch.Trim(), StringComparison.OrdinalIgnoreCase) ||
         c.NameAr.Contains(_currencySearch.Trim(), StringComparison.OrdinalIgnoreCase) ||
         (c.NameEn?.Contains(_currencySearch.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)));
    private string BaseCurrencyLabel => _currencies.FirstOrDefault(x => x.Id == _settingsBaseCurrencyId)?.Code
        ?? _settings?.BaseCurrencyCode ?? "العملة الأساسية";
    private string CurrencyFormatPreview =>
        1234.5m.ToString("N" + Math.Clamp(_currencyDecimalPlaces, 0, 6));
    private string RateConversionPreview => _rateCurrencyId.HasValue
        ? $"1 {(_currencies.FirstOrDefault(x => x.Id == _rateCurrencyId)?.Code ?? "عملة")} = {FormatExchangeRate(IsSelectedRateBaseCurrency ? 1m : _rateValue)} {BaseCurrencyLabel}"
        : "اختر العملة لعرض اتجاه التحويل إلى العملة الأساسية.";
    private string? SettingsFieldError(string label)
    {
        if (string.IsNullOrWhiteSpace(_settingsError)) return null;
        var lookupKey = label switch
        {
            "حساب بضاعة مستلمة غير مفوترة (GRNI)" => "حساب بضاعة مستلمة غير مفوترة",
            "حساب أرباح فروقات الصرف" => "حساب أرباح فروق العملة",
            "حساب خسائر فروقات الصرف" => "حساب خسائر فروق العملة",
            _ => label
        };
        return _settingsError.StartsWith(lookupKey + ":", StringComparison.Ordinal)
            ? _settingsError : null;
    }

    private string AccountDisplay(Guid? id)
    {
        if (!id.HasValue) return "— لا يوجد حساب في ملف ترحيل نشط —";
        var account = _accounts.FirstOrDefault(x => x.Id == id.Value);
        return account is null ? "حساب مرتبط (حدّث بيانات الحسابات)" : $"{account.Code} — {account.NameAr}";
    }

    private Task SetCurrencyStatusFilter(string? value) { _currencyStatusFilter = value ?? "all"; return Task.CompletedTask; }
    private Task SetRateStatusFilter(string? value) { _rateStatusFilter = value ?? "all"; return Task.CompletedTask; }
    private Task SetRateFilterCurrency(string? value) { _rateFilterCurrencyId = ParseNullableGuid(value); return Task.CompletedTask; }
    private Task SetRateDateFrom(DateOnly? value) { _rateDateFrom = value; return Task.CompletedTask; }
    private Task SetRateDateTo(DateOnly? value) { _rateDateTo = value; return Task.CompletedTask; }
    private void NewCurrency() { _ = CancelCurrencyEdit(); _currencyEditorOpen = true; }
    private void CloseCurrencyEditor() { _ = CancelCurrencyEdit(); }
    private void NewRate() { _ = CancelRateEdit(); _rateEditorOpen = true; }
    private void CloseRateEditor() { _ = CancelRateEdit(); }



    private void CurrencyWarning(string message)
    {
        _currencyError = message;
        if (message.Contains("كود العملة")) _currencyCodeError = message;
        else if (message.Contains("اسم العملة بالعربي")) _currencyNameError = message;
        else if (message.Contains("الخانات العشرية")) _currencyDecimalsError = message;
        Snackbar.Warning(message);
    }
    private void RateWarning(string message)
    {
        _rateError = message;
        if (message.Contains("اختر العملة")) _rateCurrencyError = message;
        else if (message.Contains("تاريخ")) _rateDateError = message;
        Snackbar.Warning(message);
    }

    // ================================================================
    // الحالة العامة
    // ================================================================

    private bool _busy;

    private List<CurrencyDto> _currencies = [];
    private List<ExchangeRateDto> _rates = [];
    private List<AccountDto> _accounts = [];
    private List<EmployeeDto> _employees = [];
    private List<EmployeeAccountDto> _employeeAccounts = [];

    private AccountingSettingsDto? _settings;


    // ================================================================
    // إعدادات المحاسبة
    // ================================================================

    private Guid? _settingsBaseCurrencyId;

    private Guid? _settingsEmployeeParentAccountId;

    private Guid? _settingsCashParentAccountId;

    private Guid? _settingsBankParentAccountId;

    private Guid? _settingsExchangeGainAccountId;

    private Guid? _settingsExchangeLossAccountId;

    private Guid? _settingsRetainedEarningsAccountId;

    private Guid? _settingsSalesRevenueAccountId;

    private Guid? _settingsTaxPayableAccountId;

    private Guid? _settingsCustomerAdvancesAccountId;

    private Guid? _settingsInventoryAccountId;

    private Guid? _settingsCogsAccountId;
    private Guid? _settingsGrniAccountId;
    private Guid? _settingsPurchaseTaxAccountId;
    private Guid? _settingsPurchasePriceVarianceAccountId;


    // ================================================================
    // العملات
    // ================================================================

    private Guid? _editingCurrencyId;

    private string _editingCurrencyRowVersion =
        string.Empty;

    private string _currencyCode =
        string.Empty;

    private string _currencyNameAr =
        string.Empty;

    private string? _currencyNameEn;

    private string? _currencySymbol;

    private int _currencyDecimalPlaces = 2;

    private bool _currencyIsActive = true;


    // ================================================================
    // أسعار الصرف
    // ================================================================

    private Guid? _editingRateId;

    private string _editingRateRowVersion =
        string.Empty;

    private Guid? _rateCurrencyId;

    private DateOnly? _rateDate =
        DateOnly.FromDateTime(
            DateTime.Today);

    private decimal _rateValue = 1m;

    private ExchangeRateType _rateType =
        ExchangeRateType.Accounting;

    private bool _rateIsActive = true;


    private bool IsSelectedRateBaseCurrency =>
        _rateCurrencyId.HasValue &&
        IsBaseCurrency(
            _rateCurrencyId.Value);

    private IReadOnlyList<UiSelectOption> BaseCurrencyOptions =>
        _currencies
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}"))
            .ToArray();

    private static IReadOnlyList<UiSelectOption> CurrencyDecimalOptions { get; } =
        Enumerable.Range(0, 7)
            .Select(x => new UiSelectOption(x.ToString(), x.ToString()))
            .ToArray();

    private IReadOnlyList<UiSelectOption> RateCurrencyOptions =>
        _currencies
            .Where(x => x.IsActive || x.Id == _rateCurrencyId)
            .OrderBy(x => x.Code)
            .Select(x => new UiSelectOption(
                x.Id.ToString("D"),
                $"{x.Code} - {x.NameAr}{(IsBaseCurrency(x.Id) ? " (العملة الأساسية)" : string.Empty)}{(!x.IsActive ? " (معطلة)" : string.Empty)}"))
            .ToArray();

    private static IReadOnlyList<UiSelectOption> RateTypeOptions { get; } =
    [
        new(((int)ExchangeRateType.Accounting).ToString(), "محاسبي")
    ];

    private string RateTypeValue =>
        ((int)_rateType).ToString();


    // ================================================================
    // الموظفين
    // ================================================================

    private Guid? _employeeToActivateId;

    private IReadOnlyList<UiSelectOption> AvailableEmployeeOptions =>
        AvailableEmployees
            .Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.EmployeeCode} - {x.DisplayName}"))
            .ToArray();


    // ================================================================
    // الحساب المختار لذمم الموظفين
    // ================================================================

    private UiLookupItem? EmployeeParentAccountLookupItem =>
        GetAccountLookupItem(_settingsEmployeeParentAccountId, AccountingAccountEligibilityContext.AssetControlParent);


    // ================================================================
    // الحساب المختار للصناديق
    // ================================================================

    private UiLookupItem? CashParentAccountLookupItem =>
        GetAccountLookupItem(_settingsCashParentAccountId, AccountingAccountEligibilityContext.AssetControlParent);


    // ================================================================
    // الحساب المختار للبنوك
    // ================================================================

    private UiLookupItem? BankParentAccountLookupItem =>
        GetAccountLookupItem(_settingsBankParentAccountId, AccountingAccountEligibilityContext.AssetControlParent);

    private UiLookupItem? SalesRevenueAccountLookupItem =>
        GetAccountLookupItem(_settingsSalesRevenueAccountId, AccountingAccountEligibilityContext.SalesRevenue);

    private UiLookupItem? TaxPayableAccountLookupItem =>
        GetAccountLookupItem(_settingsTaxPayableAccountId, AccountingAccountEligibilityContext.TaxPayable);

    private UiLookupItem? CustomerAdvancesAccountLookupItem =>
        GetAccountLookupItem(_settingsCustomerAdvancesAccountId, AccountingAccountEligibilityContext.CustomerAdvances);

    private UiLookupItem? InventoryAccountLookupItem =>
        GetAccountLookupItem(_settingsInventoryAccountId, AccountingAccountEligibilityContext.Inventory);

    private UiLookupItem? CogsAccountLookupItem =>
        GetAccountLookupItem(_settingsCogsAccountId, AccountingAccountEligibilityContext.CostOfGoodsSold);

    private UiLookupItem? GrniAccountLookupItem =>
        GetAccountLookupItem(_settingsGrniAccountId, AccountingAccountEligibilityContext.GoodsReceivedNotInvoiced);

    private UiLookupItem? PurchaseTaxAccountLookupItem =>
        GetAccountLookupItem(_settingsPurchaseTaxAccountId, AccountingAccountEligibilityContext.PurchaseTax);

    private UiLookupItem? PurchasePriceVarianceAccountLookupItem =>
        GetAccountLookupItem(_settingsPurchasePriceVarianceAccountId, AccountingAccountEligibilityContext.PurchasePriceVariance);

    private UiLookupItem? ExchangeGainAccountLookupItem =>
        GetAccountLookupItem(_settingsExchangeGainAccountId, AccountingAccountEligibilityContext.ExchangeGain);

    private UiLookupItem? ExchangeLossAccountLookupItem =>
        GetAccountLookupItem(_settingsExchangeLossAccountId, AccountingAccountEligibilityContext.ExchangeLoss);

    private UiLookupItem? RetainedEarningsAccountLookupItem =>
        GetAccountLookupItem(_settingsRetainedEarningsAccountId, AccountingAccountEligibilityContext.RetainedEarnings);


    // ================================================================
    // البحث في الحسابات - Server-side + سياق الأهلية
    // ================================================================

    private Task<IReadOnlyList<UiLookupItem>> SearchParentAccountsAsync(
        string search,
        CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.AssetControlParent, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchSalesRevenueAccountsAsync(
        string search,
        CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.SalesRevenue, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchTaxPayableAccountsAsync(
        string search,
        CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.TaxPayable, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchCustomerAdvancesAccountsAsync(
        string search,
        CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.CustomerAdvances, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchInventoryAccountsAsync(
        string search,
        CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.Inventory, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchCogsAccountsAsync(
        string search,
        CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.CostOfGoodsSold, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchGrniAccountsAsync(string search, CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.GoodsReceivedNotInvoiced, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchPurchaseTaxAccountsAsync(string search, CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.PurchaseTax, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchPurchasePriceVarianceAccountsAsync(string search, CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.PurchasePriceVariance, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchExchangeGainAccountsAsync(
        string search,
        CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.ExchangeGain, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchExchangeLossAccountsAsync(
        string search,
        CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.ExchangeLoss, cancellationToken);

    private Task<IReadOnlyList<UiLookupItem>> SearchRetainedEarningsAccountsAsync(
        string search,
        CancellationToken cancellationToken) =>
        SearchAccountsAsync(search, AccountingAccountEligibilityContext.RetainedEarnings, cancellationToken);

    private async Task<IReadOnlyList<UiLookupItem>> SearchAccountsAsync(
        string search,
        AccountingAccountEligibilityContext context,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var matches = new List<AccountDto>();
        var pageNumber = 1;

        while (true)
        {
            var page = await Accounting.GetAccountsPageAsync(
                new PageRequest
                {
                    PageNumber = pageNumber,
                    PageSize = PageRequest.MaximumPageSize,
                    Search = normalizedSearch,
                    SortBy = "Code",
                    SortDirection = SortDirection.Ascending
                },
                cancellationToken);

            matches.AddRange(page.Items);

            if (normalizedSearch is null || !page.HasNextPage)
                break;

            pageNumber++;
        }

        foreach (var account in matches)
        {
            var index = _accounts.FindIndex(x => x.Id == account.Id);
            if (index >= 0) _accounts[index] = account;
            else _accounts.Add(account);
        }

        return matches
            .Select(x => AccountingLookupEligibility.ToAccountLookup(x, context))
            .ToArray();
    }

    // ================================================================
    // الحصول على الحساب المحدد
    // ================================================================

    private UiLookupItem? GetAccountLookupItem(
        Guid? accountId,
        AccountingAccountEligibilityContext context)
    {
        if (!accountId.HasValue) return null;

        var account = _accounts.FirstOrDefault(x => x.Id == accountId.Value);
        return account is null
            ? null
            : AccountingLookupEligibility.ToAccountLookup(account, context);
    }

    // ================================================================
    // تحويل الحساب إلى عنصر Lookup
    // ================================================================

    private static UiLookupItem CreateAccountLookupItem(
        AccountDto account,
        AccountingAccountEligibilityContext context) =>
        AccountingLookupEligibility.ToAccountLookup(account, context);

    // ================================================================
    // أيقونة الحساب
    // ================================================================

    private static string
        GetAccountIcon(
            AccountDto account)
    {
        return account.AccountType switch
        {
            AccountType.Header =>
                "fa-solid fa-folder",

            AccountType.Control =>
                "fa-solid fa-sitemap",

            AccountType.Posting =>
                "fa-solid fa-file-invoice-dollar",

            AccountType.Subledger =>
                "fa-solid fa-list-ul",

            _ =>
                "fa-solid fa-book"
        };
    }


    // ================================================================
    // Guid -> Lookup Value
    // ================================================================

    private static string?
        ToLookupValue(
            Guid? value)
    {
        return value?
            .ToString("D");
    }


    private Task SetBaseCurrency(string? value)
    {
        _settingsBaseCurrencyId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    private Task SetCurrencyDecimalPlaces(string? value)
    {
        if (int.TryParse(value, out var parsed) && parsed is >= 0 and <= 6)
            _currencyDecimalPlaces = parsed;
        return Task.CompletedTask;
    }

    private Task SetRateCurrency(string? value)
    {
        _rateCurrencyId = ParseNullableGuid(value);
        OnRateCurrencyChanged();
        return Task.CompletedTask;
    }

    private Task SetRateType(string? value)
    {
        if (int.TryParse(value, out var parsed) &&
            Enum.IsDefined(typeof(ExchangeRateType), parsed))
        {
            _rateType = (ExchangeRateType)parsed;
        }
        return Task.CompletedTask;
    }

    private Task SetEmployeeToActivate(string? value)
    {
        _employeeToActivateId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    // ================================================================
    // اختيار حساب ذمم الموظفين
    // ================================================================

    private Task
        SetEmployeeParentAccount(
            string? value)
    {
        _settingsEmployeeParentAccountId =
            ParseNullableGuid(
                value);

        return Task.CompletedTask;
    }


    // ================================================================
    // اختيار حساب الصناديق
    // ================================================================

    private Task
        SetCashParentAccount(
            string? value)
    {
        _settingsCashParentAccountId =
            ParseNullableGuid(
                value);

        return Task.CompletedTask;
    }


    // ================================================================
    // اختيار حساب البنوك
    // ================================================================

    private Task
        SetBankParentAccount(
            string? value)
    {
        _settingsBankParentAccountId =
            ParseNullableGuid(
                value);

        return Task.CompletedTask;
    }


    private Task SetSalesRevenueAccount(string? value)
    {
        _settingsSalesRevenueAccountId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    private Task SetTaxPayableAccount(string? value)
    {
        _settingsTaxPayableAccountId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    private Task SetCustomerAdvancesAccount(string? value)
    {
        _settingsCustomerAdvancesAccountId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    private Task SetInventoryAccount(string? value)
    {
        _settingsInventoryAccountId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    private Task SetCogsAccount(string? value)
    {
        _settingsCogsAccountId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    private Task SetGrniAccount(string? value) { _settingsGrniAccountId = ParseNullableGuid(value); return Task.CompletedTask; }
    private Task SetPurchaseTaxAccount(string? value) { _settingsPurchaseTaxAccountId = ParseNullableGuid(value); return Task.CompletedTask; }
    private Task SetPurchasePriceVarianceAccount(string? value) { _settingsPurchasePriceVarianceAccountId = ParseNullableGuid(value); return Task.CompletedTask; }

    private Task SetExchangeGainAccount(string? value)
    {
        _settingsExchangeGainAccountId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    private Task SetExchangeLossAccount(string? value)
    {
        _settingsExchangeLossAccountId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    private Task SetRetainedEarningsAccount(string? value)
    {
        _settingsRetainedEarningsAccountId = ParseNullableGuid(value);
        return Task.CompletedTask;
    }

    // ================================================================
    // تحويل النص إلى Guid
    // ================================================================

    private static Guid?
        ParseNullableGuid(
            string? value)
    {
        return Guid.TryParse(
            value,
            out var id)
                ? id
                : null;
    }


    // ================================================================
    // الموظفون غير المرتبطين محاسبيًا
    // ================================================================

    private IEnumerable<EmployeeDto>
        AvailableEmployees
    {
        get
        {
            var mapped =
                _employeeAccounts
                    .Select(
                        x => x.EmployeeId)
                    .ToHashSet();


            return _employees
                .Where(
                    x =>
                        x.IsActive &&
                        !mapped.Contains(
                            x.Id))
                .OrderBy(
                    x => x.EmployeeCode);
        }
    }


    // ================================================================
    // التهيئة
    // ================================================================

    protected override async Task OnParametersSetAsync()
    {
        var section = Section switch
        {
            "general" or "cash" or "receivables" or "exchange" or "purchasing"
                or "currencies" or "rates" or "employees" => Section,
            _ => "general"
        };
        if (_activeSection == section) return;
        _activeSection = section;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            switch (_activeSection)
            {
                case "general":
                case "exchange":
                case "purchasing":
                    _currencies = await LoadAllPagesAsync<CurrencyDto>(
                        request => Accounting.GetCurrenciesPageAsync(request), "Code");
                    _settings = await Accounting.GetAccountingSettingsAsync();
                    ApplySettings(_settings);
                    await LoadSelectedAccountsAsync();
                    _settingsError = null;
                    break;
                case "currencies":
                    _currencies = await LoadAllPagesAsync<CurrencyDto>(
                        request => Accounting.GetCurrenciesPageAsync(request), "Code");
                    _settings = await Accounting.GetAccountingSettingsAsync();
                    ApplySettings(_settings);
                    break;
                case "rates":
                    _currencies = await LoadAllPagesAsync<CurrencyDto>(
                        request => Accounting.GetCurrenciesPageAsync(request), "Code");
                    _settings = await Accounting.GetAccountingSettingsAsync();
                    ApplySettings(_settings);
                    await ReloadRatesPageAsync();
                    break;
                case "employees":
                    _employees = await LoadAllPagesAsync<EmployeeDto>(
                        request => Employees.GetPageAsync(request), "EmployeeCode");
                    await ReloadEmployeeAccountsPageAsync();
                    break;
            }
        }
        catch (ApiClientException ex) { ShowAccountingError(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _busy = false; }
    }

    private async Task ReloadRatesPageAsync()
    {
        var page = await Accounting.GetExchangeRatesPageAsync(new PageRequest
        {
            PageNumber = _ratePageNumber,
            PageSize = RatePageSize,
            Search = _rateSearch,
            Status = _rateStatusFilter == "all" ? null : _rateStatusFilter,
            ExchangeRateCurrencyId = _rateFilterCurrencyId,
            DateFrom = _rateDateFrom,
            DateTo = _rateDateTo,
            SortBy = "RateDate", SortDirection = SortDirection.Descending
        });
        _rates = page.Items.ToList();
        _rateTotalCount = page.TotalCount;
    }

    private async Task ApplyRateFiltersAsync()
    {
        if (_rateDateFrom.HasValue && _rateDateTo.HasValue && _rateDateFrom > _rateDateTo)
        {
            _rateFilterError = "تاريخ البداية يجب ألا يكون بعد تاريخ النهاية.";
            return;
        }
        _rateFilterError = null;
        _ratePageNumber = 1;
        await RunAsync(ReloadRatesPageAsync);
    }

    private async Task PreviousRatePageAsync()
    {
        if (_ratePageNumber <= 1) return;
        _ratePageNumber--;
        await RunAsync(ReloadRatesPageAsync);
    }
    private async Task NextRatePageAsync()
    {
        if (_ratePageNumber * RatePageSize >= _rateTotalCount) return;
        _ratePageNumber++;
        await RunAsync(ReloadRatesPageAsync);
    }

    private async Task ReloadEmployeeAccountsPageAsync()
    {
        var page = await Accounting.GetEmployeeAccountsPageAsync(new PageRequest
        {
            PageNumber = _employeeAccountPageNumber,
            PageSize = EmployeeAccountsPageSize,
            Search = _employeeAccountSearch
        });
        _employeeAccounts = page.Items.ToList();
        _employeeAccountTotalCount = page.TotalCount;
    }
    private async Task SearchEmployeeAccountsAsync()
    {
        _employeeAccountPageNumber = 1;
        await RunAsync(ReloadEmployeeAccountsPageAsync);
    }
    private async Task PreviousEmployeeAccountsPageAsync()
    {
        if (_employeeAccountPageNumber <= 1) return;
        _employeeAccountPageNumber--;
        await RunAsync(ReloadEmployeeAccountsPageAsync);
    }
    private async Task NextEmployeeAccountsPageAsync()
    {
        if (_employeeAccountPageNumber * EmployeeAccountsPageSize >= _employeeAccountTotalCount) return;
        _employeeAccountPageNumber++;
        await RunAsync(ReloadEmployeeAccountsPageAsync);
    }

    private static async Task<List<T>> LoadAllPagesAsync<T>(
        Func<PageRequest, Task<PagedResult<T>>> loader,
        string? sortBy = null,
        SortDirection sortDirection = SortDirection.Ascending)
    {
        var result = new List<T>();
        var pageNumber = 1;

        while (true)
        {
            var page = await loader(
                new PageRequest
                {
                    PageNumber = pageNumber,
                    PageSize = PageRequest.MaximumPageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                });

            result.AddRange(page.Items);
            if (!page.HasNextPage)
                return result;

            pageNumber++;
        }
    }


    // Loading every account from the chart is costly. Selected accounts are
    // requested by ID; all other candidates come from the server-side lookup.
    private async Task LoadSelectedAccountsAsync()
    {
        _accounts.Clear();
        var ids = new Guid?[]
        {
            _settingsEmployeeParentAccountId, _settingsCashParentAccountId,
            _settingsBankParentAccountId, _settingsRetainedEarningsAccountId,
            _settingsExchangeGainAccountId, _settingsExchangeLossAccountId,
            _settingsInventoryAccountId, _settingsGrniAccountId,
            _settingsPurchaseTaxAccountId, _settingsPurchasePriceVarianceAccountId,
            _settingsSalesRevenueAccountId, _settingsTaxPayableAccountId,
            _settingsCogsAccountId, _settingsCustomerAdvancesAccountId
        };
        foreach (var id in ids.Where(x => x.HasValue).Select(x => x!.Value).Distinct())
        {
            var account = await Accounting.GetAccountByIdAsync(id);
            if (account is not null) _accounts.Add(account);
        }
    }

    // ================================================================
    // تحميل جميع الحسابات
    // ================================================================

    private async Task<List<AccountDto>>
        LoadAllAccountsAsync()
    {
        var result =
            new List<AccountDto>();

        var pageNumber = 1;


        while (true)
        {
            var page =
                await Accounting
                    .GetAccountsPageAsync(
                        new PageRequest
                        {
                            PageNumber =
                                pageNumber,

                            PageSize =
                                PageRequest.MaximumPageSize,

                            SortBy =
                                "Code",

                            SortDirection =
                                SortDirection.Ascending
                        });


            result.AddRange(
                page.Items);


            if (!page.HasNextPage)
            {
                break;
            }


            pageNumber++;
        }


        return result;
    }


    // ================================================================
    // تطبيق إعدادات الخادم
    // ================================================================

    private void
        ApplySettings(
            AccountingSettingsDto? settings)
    {
        _settingsBaseCurrencyId =
            settings?
                .BaseCurrencyId;


        _settingsEmployeeParentAccountId =
            settings?
                .EmployeeParentAccountId;


        _settingsCashParentAccountId =
            settings?
                .CashParentAccountId;


        _settingsBankParentAccountId =
            settings?
                .BankParentAccountId;


        _settingsExchangeGainAccountId =
            settings?
                .ExchangeGainAccountId;


        _settingsExchangeLossAccountId =
            settings?
                .ExchangeLossAccountId;

        _settingsRetainedEarningsAccountId =
            settings?
                .RetainedEarningsAccountId;


        _settingsSalesRevenueAccountId =
            settings?
                .SalesRevenueAccountId;


        _settingsTaxPayableAccountId =
            settings?
                .TaxPayableAccountId;

        _settingsCustomerAdvancesAccountId = settings?.CustomerAdvancesAccountId;


        _settingsInventoryAccountId =
            settings?
                .InventoryAccountId;


        _settingsCogsAccountId = settings?.CogsAccountId;
        _settingsGrniAccountId = settings?.GrniAccountId;
        _settingsPurchaseTaxAccountId = settings?.PurchaseTaxAccountId;
        _settingsPurchasePriceVarianceAccountId = settings?.PurchasePriceVarianceAccountId;
    }


    // ================================================================
    // حفظ الإعدادات
    // ================================================================

    private async Task
        SaveSettingsAsync()
    {
        await RunAsync(
            async () =>
            {
                _settingsError = null;
                _baseCurrencyError = null;
                if (!_settingsBaseCurrencyId.HasValue)
                {
                    _settingsError = "اختر العملة الأساسية قبل حفظ الإعدادات.";
                    _baseCurrencyError = _settingsError;
                    return;
                }
                if (_settings is { BaseCurrencyChangeAllowed: false } &&
                    _settingsBaseCurrencyId != _settings.BaseCurrencyId)
                {
                    _settingsError = "لا يمكن تغيير العملة الأساسية بعد وجود قيود مرحلة.";
                    _baseCurrencyError = _settingsError;
                    return;
                }


                if (!await ValidateSettingsAccountAsync(
                        _settingsEmployeeParentAccountId,
                        AccountingAccountEligibilityContext.AssetControlParent,
                        "الحساب الرئيسي لذمم الموظفين"))
                {
                    return;
                }

                if (!await ValidateSettingsAccountAsync(
                        _settingsCashParentAccountId,
                        AccountingAccountEligibilityContext.AssetControlParent,
                        "الحساب الرئيسي للصناديق"))
                {
                    return;
                }

                if (!await ValidateSettingsAccountAsync(
                        _settingsBankParentAccountId,
                        AccountingAccountEligibilityContext.AssetControlParent,
                        "الحساب الرئيسي للبنوك"))
                {
                    return;
                }


                if (!await ValidateSettingsAccountAsync(
                        _settingsInventoryAccountId,
                        AccountingAccountEligibilityContext.Inventory,
                        "حساب المخزون"))
                {
                    return;
                }


                if (!await ValidateSettingsAccountAsync(_settingsGrniAccountId, AccountingAccountEligibilityContext.GoodsReceivedNotInvoiced, "حساب بضاعة مستلمة غير مفوترة")) return;
                if (!await ValidateSettingsAccountAsync(_settingsPurchaseTaxAccountId, AccountingAccountEligibilityContext.PurchaseTax, "حساب ضريبة المشتريات")) return;
                if (!await ValidateSettingsAccountAsync(_settingsPurchasePriceVarianceAccountId, AccountingAccountEligibilityContext.PurchasePriceVariance, "حساب فرق سعر المشتريات")) return;

                var hasPurchasingReceiptAccount = _settingsInventoryAccountId.HasValue || _settingsGrniAccountId.HasValue;
                if (hasPurchasingReceiptAccount && (!_settingsInventoryAccountId.HasValue || !_settingsGrniAccountId.HasValue))
                {
                    _settingsError = "حساب بضاعة مستلمة غير مفوترة: حدد حساب المخزون وحساب GRNI معًا.";
                    Snackbar.Warning(_settingsError);
                    return;
                }

                if ((_settingsPurchaseTaxAccountId.HasValue || _settingsPurchasePriceVarianceAccountId.HasValue) && !_settingsGrniAccountId.HasValue)
                {
                    _settingsError = "حساب بضاعة مستلمة غير مفوترة: حدد حساب GRNI قبل إعداد الضريبة أو فرق السعر.";
                    Snackbar.Warning(_settingsError);
                    return;
                }

                if (!await ValidateSettingsAccountAsync(
                        _settingsExchangeGainAccountId,
                        AccountingAccountEligibilityContext.ExchangeGain,
                        "حساب أرباح فروق العملة"))
                {
                    return;
                }

                if (!await ValidateSettingsAccountAsync(
                        _settingsExchangeLossAccountId,
                        AccountingAccountEligibilityContext.ExchangeLoss,
                        "حساب خسائر فروق العملة"))
                {
                    return;
                }

                if (!await ValidateSettingsAccountAsync(
                        _settingsRetainedEarningsAccountId,
                        AccountingAccountEligibilityContext.RetainedEarnings,
                        "حساب الأرباح المحتجزة"))
                {
                    return;
                }

                var updated =
                    await Accounting
                        .UpdateAccountingSettingsAsync(
                            new UpdateAccountingSettingsRequest(
                                _settingsBaseCurrencyId.Value,

                                _settingsEmployeeParentAccountId,

                                _settingsCashParentAccountId,

                                _settingsBankParentAccountId,

                                _settingsExchangeGainAccountId,

                                _settingsExchangeLossAccountId,

                                _settingsSalesRevenueAccountId,

                                _settingsTaxPayableAccountId,

                                _settingsCustomerAdvancesAccountId,

                                _settingsInventoryAccountId,

                                _settingsCogsAccountId,

                                ExchangeRateType.Accounting,

                                _settings?
                                    .RowVersion,

                                _settingsRetainedEarningsAccountId,
                                _settingsGrniAccountId,
                                _settingsPurchaseTaxAccountId,
                                _settingsPurchasePriceVarianceAccountId));


                if (updated is null)
                {
                    return;
                }


                _settings =
                    updated;


                ApplySettings(
                    updated);


                if (IsSelectedRateBaseCurrency)
                {
                    _rateValue = 1m;
                }


                Snackbar.Success(
                    "تم حفظ إعدادات المحاسبة.");
            });
    }


    private async Task<bool> ValidateSettingsAccountAsync(
        Guid? accountId,
        AccountingAccountEligibilityContext context,
        string fieldLabel)
    {
        if (!accountId.HasValue)
        {
            return true;
        }

        var account = await Accounting.GetAccountByIdAsync(accountId.Value);
        if (account is null)
        {
            _settingsError = $"{fieldLabel}: الحساب المحدد لم يعد موجودًا.";
            Snackbar.Warning(_settingsError);
            return false;
        }

        var index = _accounts.FindIndex(x => x.Id == account.Id);
        if (index >= 0) _accounts[index] = account;
        else _accounts.Add(account);

        var eligibility = AccountingAccountEligibility.Evaluate(account, context);
        if (eligibility.IsEligible)
        {
            return true;
        }

        _settingsError = $"{fieldLabel}: {eligibility.Reason ?? "الحساب المحدد غير صالح لهذه العملية."}";
        Snackbar.Warning(_settingsError);
        return false;
    }


    // ================================================================
    // حفظ العملة
    // ================================================================

    private async Task
        SaveCurrencyAsync()
    {
        await RunAsync(
            async () =>
            {
                _currencyError = null;
                _currencyCodeError = _currencyNameError = _currencyDecimalsError = null;
                var code =
                    _currencyCode
                        .Trim()
                        .ToUpperInvariant();


                var name =
                    _currencyNameAr
                        .Trim();


                if (string.IsNullOrWhiteSpace(
                    code))
                {
                    CurrencyWarning(
                        "كود العملة مطلوب.");

                    return;
                }


                if (code.Length < 3)
                {
                    CurrencyWarning(
                        "كود العملة يجب ألا يقل عن 3 أحرف.");

                    return;
                }


                if (code.Length > 8)
                {
                    CurrencyWarning(
                        "كود العملة يجب ألا يزيد عن 8 أحرف.");

                    return;
                }


                if (string.IsNullOrWhiteSpace(
                    name))
                {
                    CurrencyWarning(
                        "اسم العملة بالعربي مطلوب.");

                    return;
                }


                if (name.Length > 100)
                {
                    CurrencyWarning(
                        "اسم العملة بالعربي يجب ألا يزيد عن 100 حرف.");

                    return;
                }


                if (!string.IsNullOrWhiteSpace(
                        _currencyNameEn)
                    &&
                    _currencyNameEn
                        .Trim()
                        .Length > 100)
                {
                    CurrencyWarning(
                        "اسم العملة بالإنجليزي يجب ألا يزيد عن 100 حرف.");

                    return;
                }


                if (!string.IsNullOrWhiteSpace(
                        _currencySymbol)
                    &&
                    _currencySymbol
                        .Trim()
                        .Length > 12)
                {
                    CurrencyWarning(
                        "رمز العملة يجب ألا يزيد عن 12 حرفًا.");

                    return;
                }


                if (_currencyDecimalPlaces
                    is < 0 or > 6)
                {
                    CurrencyWarning(
                        "عدد الخانات العشرية يجب أن يكون بين 0 و6.");

                    return;
                }


                CurrencyDto? result;

                if (_editingCurrencyId.HasValue)
                {
                    var existing = _currencies.FirstOrDefault(x => x.Id == _editingCurrencyId.Value);
                    if (existing is not null)
                    {
                        if (_settings?.BaseCurrencyId == existing.Id && !_currencyIsActive)
                        {
                            CurrencyWarning("لا يمكن تعطيل العملة الأساسية. غيّر هذا الخيار قبل الحفظ.");
                            return;
                        }
                        if (existing.DecimalPlaces != _currencyDecimalPlaces &&
                            !await Dialogs.ConfirmAsync("تأكيد تغيير دقة العملة",
                                $"العملة: {existing.Code} — {existing.NameAr}\n" +
                                $"الدقة القديمة: {existing.DecimalPlaces}، الجديدة: {_currencyDecimalPlaces}.\n" +
                                "قد يرفض الخادم التغيير إذا استُخدمت العملة في قيود. راجع الأثر قبل المتابعة.",
                                AlertTone.Warning, confirmText: "متابعة", cancelText: "رجوع")) return;
                        if (existing.IsActive && !_currencyIsActive &&
                            !await Dialogs.ConfirmAsync("تأكيد تعطيل العملة",
                                $"العملة: {existing.Code} — {existing.NameAr}\n" +
                                "لن تظهر هذه العملة ضمن خيارات العمليات الجديدة بعد تعطيلها.",
                                AlertTone.Warning, confirmText: "تعطيل", cancelText: "رجوع")) return;
                    }
                }

                if (_editingCurrencyId.HasValue)
                {
                    result =
                        await Accounting
                            .UpdateCurrencyAsync(
                                _editingCurrencyId.Value,

                                new UpdateCurrencyRequest(
                                    name,

                                    N(
                                        _currencyNameEn),

                                    N(
                                        _currencySymbol),

                                    checked(
                                        (byte)
                                        _currencyDecimalPlaces),

                                    _currencyIsActive,

                                    _editingCurrencyRowVersion));
                }
                else
                {
                    result =
                        await Accounting
                            .CreateCurrencyAsync(
                                new CreateCurrencyRequest(
                                    code,

                                    name,

                                    N(
                                        _currencyNameEn),

                                    N(
                                        _currencySymbol),

                                    checked(
                                        (byte)
                                        _currencyDecimalPlaces),

                                    _currencyIsActive));
                }


                if (result is null)
                {
                    return;
                }


                Snackbar.Success(
                    "تم حفظ العملة.");


                await CancelCurrencyEdit();


                await ReloadReferenceListsAsync();
            });
    }


    // ================================================================
    // تعديل العملة
    // ================================================================

    private void
        EditCurrency(
            CurrencyDto item)
    {
        _currencyEditorOpen = true;
        _currencyError = null;
        _currencyCodeError = _currencyNameError = _currencyDecimalsError = null;
        _editingCurrencyId =
            item.Id;


        _editingCurrencyRowVersion =
            item.RowVersion;


        _currencyCode =
            item.Code;


        _currencyNameAr =
            item.NameAr;


        _currencyNameEn =
            item.NameEn;


        _currencySymbol =
            item.Symbol;


        _currencyDecimalPlaces =
            item.DecimalPlaces;


        _currencyIsActive =
            item.IsActive;
    }


    // ================================================================
    // إلغاء تعديل العملة
    // ================================================================

    private Task
        CancelCurrencyEdit()
    {
        _currencyEditorOpen = false;
        _currencyError = null;
        _currencyCodeError = _currencyNameError = _currencyDecimalsError = null;
        _editingCurrencyId =
            null;


        _editingCurrencyRowVersion =
            string.Empty;


        _currencyCode =
            string.Empty;


        _currencyNameAr =
            string.Empty;


        _currencyNameEn =
            null;


        _currencySymbol =
            null;


        _currencyDecimalPlaces =
            2;


        _currencyIsActive =
            true;


        return Task.CompletedTask;
    }


    // ================================================================
    // حفظ سعر الصرف
    // ================================================================

    private async Task
        SaveRateAsync()
    {
        await RunAsync(
            async () =>
            {
                _rateError = null;
                _rateCurrencyError = _rateDateError = null;
                if (!_rateCurrencyId
                    .HasValue)
                {
                    RateWarning(
                        "اختر العملة.");

                    return;
                }


                if (!_rateDate
                    .HasValue)
                {
                    RateWarning(
                        "تاريخ سعر الصرف مطلوب.");

                    return;
                }


                /*
                 * العملة الأساسية سعرها دائمًا 1.
                 */
                if (IsBaseCurrency(
                    _rateCurrencyId.Value))
                {
                    _rateValue = 1m;
                }


                if (_rateValue <= 0)
                {
                    RateWarning(
                        "سعر الصرف يجب أن يكون أكبر من صفر.");

                    return;
                }


                ExchangeRateDto? result;


                if (_editingRateId.HasValue)
                {
                    result =
                        await Accounting
                            .UpdateExchangeRateAsync(
                                _editingRateId.Value,

                                new UpdateExchangeRateRequest(
                                    _rateDate.Value,

                                    _rateValue,

                                    _rateType,

                                    _rateIsActive,

                                    _editingRateRowVersion));
                }
                else
                {
                    result =
                        await Accounting
                            .CreateExchangeRateAsync(
                                new CreateExchangeRateRequest(
                                    _rateCurrencyId.Value,

                                    _rateDate.Value,

                                    _rateValue,

                                    _rateType,

                                    _rateIsActive));
                }


                if (result is null)
                {
                    return;
                }


                Snackbar.Success(
                    IsSelectedRateBaseCurrency
                        ? "تم حفظ سعر العملة الأساسية بقيمة 1."
                        : "تم حفظ سعر الصرف.");


                await CancelRateEdit();


                await ReloadReferenceListsAsync();
            });
    }


    // ================================================================
    // تعديل سعر الصرف
    // ================================================================

    private void
        EditRate(
            ExchangeRateDto item)
    {
        _rateEditorOpen = true;
        _rateError = null;
        _rateCurrencyError = _rateDateError = null;
        _editingRateId =
            item.Id;


        _editingRateRowVersion =
            item.RowVersion;


        _rateCurrencyId =
            item.CurrencyId;


        _rateDate =
            item.RateDate;


        _rateValue =
            IsBaseCurrency(
                item.CurrencyId)
                ? 1m
                : item.Rate;


        _rateType =
            item.RateType;


        _rateIsActive =
            item.IsActive;
    }


    // ================================================================
    // إلغاء تعديل سعر الصرف
    // ================================================================

    private Task
        CancelRateEdit()
    {
        _rateEditorOpen = false;
        _rateError = null;
        _rateCurrencyError = _rateDateError = null;
        _editingRateId =
            null;


        _editingRateRowVersion =
            string.Empty;


        _rateCurrencyId =
            null;


        _rateDate =
            DateOnly.FromDateTime(
                DateTime.Today);


        _rateValue =
            1m;


        _rateType =
            ExchangeRateType.Accounting;


        _rateIsActive =
            true;


        return Task.CompletedTask;
    }


    // ================================================================
    // تغيير العملة في سعر الصرف
    // ================================================================

    private void
        OnRateCurrencyChanged()
    {
        if (IsSelectedRateBaseCurrency)
        {
            _rateValue = 1m;
        }
    }


    // ================================================================
    // هل العملة هي العملة الأساسية؟
    // ================================================================

    private bool
        IsBaseCurrency(
            Guid currencyId)
    {
        return
            _settingsBaseCurrencyId.HasValue
            &&
            _settingsBaseCurrencyId.Value ==
            currencyId;
    }


    // ================================================================
    // تنسيق سعر الصرف
    // ================================================================

    private static string
        FormatExchangeRate(
            decimal value)
    {
        return value
            .ToString(
                "0.########");
    }


    // ================================================================
    // تفعيل الموظف محاسبيًا
    // ================================================================

    private async Task
        ActivateEmployeeAsync()
    {
        await RunAsync(
            async () =>
            {
                if (!_employeeToActivateId
                    .HasValue)
                {
                    return;
                }


                var settings =
                    await Accounting
                        .GetAccountingSettingsAsync();

                if (settings?.EmployeeParentAccountId is null)
                {
                    Snackbar.Warning(
                        "يجب تحديد الحساب الرئيسي لذمم الموظفين في إعدادات المحاسبة أولاً.");
                    return;
                }

                if (!await ValidateSettingsAccountAsync(
                        settings.EmployeeParentAccountId,
                        AccountingAccountEligibilityContext.AssetControlParent,
                        "الحساب الرئيسي لذمم الموظفين"))
                {
                    return;
                }

                var result =
                    await Accounting
                        .ActivateEmployeeAccountAsync(
                            new ActivateEmployeeAccountRequest(
                                _employeeToActivateId.Value,
                                true));


                if (result is null)
                {
                    return;
                }


                Snackbar.Success(
                    "تم تفعيل الموظف محاسبيًا وإنشاء حسابه المرتبط.");


                _employeeToActivateId =
                    null;


                await ReloadEmployeeAccountsAsync();
            });
    }


    // ================================================================
    // تفعيل / تعطيل حساب الموظف
    // ================================================================

    private async Task
        ToggleEmployeeAccountAsync(
            EmployeeAccountDto item)
    {
        var operation = item.IsActive ? "تعطيل" : "تفعيل";
        if (!await Dialogs.ConfirmAsync($"تأكيد {operation} ربط الموظف",
            $"الموظف: {item.EmployeeCode} — {item.EmployeeName}\nالحساب: {item.AccountCode} — {item.AccountName}\n" +
            (item.IsActive ? "لن يمكن استخدام هذا الربط في عمليات جديدة بعد التعطيل." : "سيصبح الربط المحاسبي نشطًا."),
            AlertTone.Warning, confirmText: operation, cancelText: "رجوع")) return;
        await RunAsync(
            async () =>
            {
                var result =
                    await Accounting
                        .SetEmployeeAccountStatusAsync(
                            item.Id,

                            new SetEmployeeAccountStatusRequest(
                                !item.IsActive,

                                item.RowVersion));


                if (result is null)
                {
                    return;
                }


                Snackbar.Success(
                    result.IsActive
                        ? "تم تفعيل الربط المحاسبي."
                        : "تم تعطيل الربط المحاسبي.");


                await ReloadEmployeeAccountsAsync();
            });
    }


    // ================================================================
    // إعادة تحميل العملات وأسعار الصرف
    // ================================================================

    private async Task ReloadReferenceListsAsync()
    {
        if (_activeSection == "currencies" || _activeSection == "rates")
            _currencies = await LoadAllPagesAsync<CurrencyDto>(
                request => Accounting.GetCurrenciesPageAsync(request), "Code");
        if (_activeSection == "rates")
            await ReloadRatesPageAsync();
    }

    // ================================================================
    // إعادة تحميل الربط المحاسبي للموظفين
    // ================================================================

    private Task ReloadEmployeeAccountsAsync() => ReloadEmployeeAccountsPageAsync();

    // ================================================================
    // تشغيل عملية مع معالجة الأخطاء
    // ================================================================

    private async Task
        RunAsync(
            Func<Task> action)
    {
        if (_busy)
        {
            return;
        }


        _busy = true;


        try
        {
            await action();
        }
        catch (ApiClientException ex)
        {
            if (_activeSection is "general" or "exchange" or "purchasing") _settingsError = "تعذر حفظ الإعدادات؛ راجع الرسالة وصحح البيانات.";
            if (_activeSection == "currencies") _currencyError = "تعذر حفظ العملة؛ راجع الرسالة وصحح الحقول.";
            if (_activeSection == "rates") _rateError = "تعذر حفظ سعر الصرف؛ راجع الرسالة وصحح الحقول.";
            ShowAccountingError(ex.Error);
        }
        catch
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _busy = false;
        }
    }


    // ================================================================
    // Normalize String
    // ================================================================

    private static string?
        N(
            string? value)
    {
        return string.IsNullOrWhiteSpace(
            value)
                ? null
                : value.Trim();
    }

    private void ShowAccountingError(OAS.Contracts.Common.Errors.ApiError error)
    {
        if (AccountingApiErrorPresenter.TryGetMessage(error, out var message))
        {
            Snackbar.Error(message);
            return;
        }

        ApiFeedback.Show(error);
    }

}