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
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new UiSelectOption(
                x.Id.ToString("D"),
                $"{x.Code} - {x.NameAr}{(IsBaseCurrency(x.Id) ? " (العملة الأساسية)" : string.Empty)}"))
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

    protected override Task
        OnInitializedAsync()
    {
        return LoadAsync();
    }


    // ================================================================
    // تحميل الصفحة
    // ================================================================

    private async Task
        LoadAsync()
    {
        if (_busy)
        {
            return;
        }


        _busy = true;


        try
        {
            var currencies =
                await LoadAllPagesAsync<CurrencyDto>(
                    request => Accounting.GetCurrenciesPageAsync(request),
                    "Code",
                    SortDirection.Ascending);


            var rates =
                await LoadAllPagesAsync<ExchangeRateDto>(
                    request => Accounting.GetExchangeRatesPageAsync(request),
                    "RateDate",
                    SortDirection.Descending);


            /*
             * تحميل جميع الحسابات على عدة صفحات.
             *
             * هذا مهم لأن MaximumPageSize = 200.
             */
            var accounts =
                await LoadAllAccountsAsync();


            var employees =
                await LoadAllPagesAsync<EmployeeDto>(
                    request => Employees.GetPageAsync(request),
                    "EmployeeCode",
                    SortDirection.Ascending);


            var employeeAccounts =
                await LoadAllPagesAsync<EmployeeAccountDto>(
                    request => Accounting.GetEmployeeAccountsPageAsync(request));


            _currencies = currencies;


            _rates = rates;


            _accounts =
                accounts;


            _employees = employees;


            _employeeAccounts = employeeAccounts;


            _settings =
                await Accounting
                    .GetAccountingSettingsAsync();


            ApplySettings(
                _settings);
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
        }
        catch
        {
            ApiFeedback
                .ShowUnexpected();
        }
        finally
        {
            _busy = false;
        }
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
                if (!_settingsBaseCurrencyId
                    .HasValue)
                {
                    Snackbar.Warning(
                        "اختر العملة الأساسية أولاً.");

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
                        _settingsSalesRevenueAccountId,
                        AccountingAccountEligibilityContext.SalesRevenue,
                        "حساب إيرادات المبيعات"))
                {
                    return;
                }

                if (!await ValidateSettingsAccountAsync(
                        _settingsTaxPayableAccountId,
                        AccountingAccountEligibilityContext.TaxPayable,
                        "حساب الضرائب المستحقة"))
                {
                    return;
                }

                if (!await ValidateSettingsAccountAsync(
                        _settingsCustomerAdvancesAccountId,
                        AccountingAccountEligibilityContext.CustomerAdvances,
                        "حساب دفعات مقدمة من العملاء"))
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

                if (!await ValidateSettingsAccountAsync(
                        _settingsCogsAccountId,
                        AccountingAccountEligibilityContext.CostOfGoodsSold,
                        "حساب تكلفة البضاعة المباعة"))
                {
                    return;
                }

                if (!await ValidateSettingsAccountAsync(_settingsGrniAccountId, AccountingAccountEligibilityContext.GoodsReceivedNotInvoiced, "حساب بضاعة مستلمة غير مفوترة")) return;
                if (!await ValidateSettingsAccountAsync(_settingsPurchaseTaxAccountId, AccountingAccountEligibilityContext.PurchaseTax, "حساب ضريبة المشتريات")) return;
                if (!await ValidateSettingsAccountAsync(_settingsPurchasePriceVarianceAccountId, AccountingAccountEligibilityContext.PurchasePriceVariance, "حساب فرق سعر المشتريات")) return;

                var hasPurchasingReceiptAccount = _settingsInventoryAccountId.HasValue || _settingsGrniAccountId.HasValue;
                if (hasPurchasingReceiptAccount && (!_settingsInventoryAccountId.HasValue || !_settingsGrniAccountId.HasValue))
                {
                    Snackbar.Warning("لترحيل استلامات المشتريات حدد حساب المخزون وحساب بضاعة مستلمة غير مفوترة (GRNI).");
                    return;
                }

                if ((_settingsPurchaseTaxAccountId.HasValue || _settingsPurchasePriceVarianceAccountId.HasValue) && !_settingsGrniAccountId.HasValue)
                {
                    Snackbar.Warning("حدد حساب GRNI قبل إعداد حساب ضريبة المشتريات أو فرق سعر المشتريات.");
                    return;
                }

                var salesPostingAccounts = new[]
                {
                    _settingsSalesRevenueAccountId,
                    _settingsTaxPayableAccountId,
                    _settingsInventoryAccountId,
                    _settingsCogsAccountId
                };

                var hasSalesSpecificAccount = _settingsSalesRevenueAccountId.HasValue || _settingsTaxPayableAccountId.HasValue || _settingsCogsAccountId.HasValue;
                if (hasSalesSpecificAccount && salesPostingAccounts.Any(x => !x.HasValue))
                {
                    Snackbar.Warning(
                        "حدد حسابات ترحيل المبيعات الأربعة: الإيرادات، الضرائب، المخزون، وتكلفة البضاعة المباعة.");
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
            Snackbar.Warning($"{fieldLabel}: الحساب المحدد لم يعد موجودًا.");
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

        Snackbar.Warning(
            $"{fieldLabel}: {eligibility.Reason ?? "الحساب المحدد غير صالح لهذه العملية."}");

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
                    Snackbar.Warning(
                        "كود العملة مطلوب.");

                    return;
                }


                if (code.Length < 3)
                {
                    Snackbar.Warning(
                        "كود العملة يجب ألا يقل عن 3 أحرف.");

                    return;
                }


                if (code.Length > 8)
                {
                    Snackbar.Warning(
                        "كود العملة يجب ألا يزيد عن 8 أحرف.");

                    return;
                }


                if (string.IsNullOrWhiteSpace(
                    name))
                {
                    Snackbar.Warning(
                        "اسم العملة بالعربي مطلوب.");

                    return;
                }


                if (name.Length > 100)
                {
                    Snackbar.Warning(
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
                    Snackbar.Warning(
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
                    Snackbar.Warning(
                        "رمز العملة يجب ألا يزيد عن 12 حرفًا.");

                    return;
                }


                if (_currencyDecimalPlaces
                    is < 0 or > 6)
                {
                    Snackbar.Warning(
                        "عدد الخانات العشرية يجب أن يكون بين 0 و6.");

                    return;
                }


                CurrencyDto? result;


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
                if (!_rateCurrencyId
                    .HasValue)
                {
                    Snackbar.Warning(
                        "اختر العملة.");

                    return;
                }


                if (!_rateDate
                    .HasValue)
                {
                    Snackbar.Warning(
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
                    Snackbar.Warning(
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

    private async Task
        ReloadReferenceListsAsync()
    {
        _currencies =
            await LoadAllPagesAsync<CurrencyDto>(
                request => Accounting.GetCurrenciesPageAsync(request),
                "Code",
                SortDirection.Ascending);


        _rates =
            await LoadAllPagesAsync<ExchangeRateDto>(
                request => Accounting.GetExchangeRatesPageAsync(request),
                "RateDate",
                SortDirection.Descending);
    }


    // ================================================================
    // إعادة تحميل الربط المحاسبي للموظفين
    // ================================================================

    private async Task
        ReloadEmployeeAccountsAsync()
    {
        _employeeAccounts =
            await LoadAllPagesAsync<EmployeeAccountDto>(
                request => Accounting.GetEmployeeAccountsPageAsync(request));
    }


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
            ShowAccountingError(ex.Error);
        }
        catch
        {
            ApiFeedback
                .ShowUnexpected();
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