using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
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


    // ================================================================
    // الموظفين
    // ================================================================

    private Guid? _employeeToActivateId;


    // ================================================================
    // الحساب المختار لذمم الموظفين
    // ================================================================

    private UiLookupItem? EmployeeParentAccountLookupItem =>
        GetAccountLookupItem(
            _settingsEmployeeParentAccountId);


    // ================================================================
    // الحساب المختار للصناديق
    // ================================================================

    private UiLookupItem? CashParentAccountLookupItem =>
        GetAccountLookupItem(
            _settingsCashParentAccountId);


    // ================================================================
    // الحساب المختار للبنوك
    // ================================================================

    private UiLookupItem? BankParentAccountLookupItem =>
        GetAccountLookupItem(
            _settingsBankParentAccountId);


    // ================================================================
    // البحث في جميع الحسابات
    // ================================================================

    private Task<IReadOnlyList<UiLookupItem>>
        SearchAllAccountsAsync(
            string search,
            CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        var query =
            search?.Trim() ??
            string.Empty;

        IEnumerable<AccountDto> accounts =
            _accounts;


        if (!string.IsNullOrWhiteSpace(query))
        {
            accounts =
                accounts.Where(
                    account =>
                        account.Code.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        account.NameAr.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        (
                            !string.IsNullOrWhiteSpace(
                                account.NameEn)
                            &&
                            account.NameEn.Contains(
                                query,
                                StringComparison.OrdinalIgnoreCase)
                        ));
        }


        IReadOnlyList<UiLookupItem> result =
            accounts
                .OrderBy(x => x.Code)
                .Select(
                    CreateAccountLookupItem)
                .ToArray();


        return Task.FromResult(
            result);
    }


    // ================================================================
    // الحصول على الحساب المحدد
    // ================================================================

    private UiLookupItem?
        GetAccountLookupItem(
            Guid? accountId)
    {
        if (!accountId.HasValue)
        {
            return null;
        }


        var account =
            _accounts.FirstOrDefault(
                x =>
                    x.Id ==
                    accountId.Value);


        if (account is null)
        {
            return null;
        }


        return CreateAccountLookupItem(
            account);
    }


    // ================================================================
    // تحويل الحساب إلى عنصر Lookup
    // ================================================================

    private static UiLookupItem
        CreateAccountLookupItem(
            AccountDto account)
    {
        var eligible =
            IsEligibleParentAccount(
                account);


        var eligibilityText =
            eligible
                ? "مؤهل كحساب رئيسي"
                : GetParentAccountEligibilityMessage(
                    account);


        var secondary =
            !string.IsNullOrWhiteSpace(
                account.NameEn)
                ? $"{account.NameEn} • {eligibilityText}"
                : eligibilityText;


        return new UiLookupItem(
            account.Id.ToString("D"),
            $"{account.Code} - {account.NameAr}",
            secondary,
            GetAccountIcon(account),
            Disabled: !eligible);
    }


    // ================================================================
    // شروط أهلية الحساب الرئيسي
    // ================================================================

    private static bool
        IsEligibleParentAccount(
            AccountDto account)
    {
        return
            account.IsActive
            &&
            account.IsControlAccount
            &&
            account.AccountType ==
            AccountType.Control
            &&
            account.AccountClass ==
            AccountClass.Asset
            &&
            account.NormalBalance ==
            NormalBalance.Debit;
    }


    // ================================================================
    // سبب عدم أهلية الحساب
    // ================================================================

    private static string
        GetParentAccountEligibilityMessage(
            AccountDto account)
    {
        if (!account.IsActive)
        {
            return "غير نشط";
        }


        if (account.AccountClass !=
            AccountClass.Asset)
        {
            return "ليس من حسابات الأصول";
        }


        if (account.NormalBalance !=
            NormalBalance.Debit)
        {
            return "طبيعته ليست مدينة";
        }


        if (account.AccountType !=
            AccountType.Control)
        {
            return "ليس من نوع حساب تحكم";
        }


        if (!account.IsControlAccount)
        {
            return "غير معرف كحساب تحكم";
        }


        return "غير مؤهل";
    }


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
    // حسابات الترحيل
    // ================================================================

    private IEnumerable<AccountDto>
        PostingAccountOptions =>
            _accounts
                .Where(
                    x =>
                        x.IsActive &&
                        x.IsPostingAccount)
                .OrderBy(
                    x => x.Code);


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
                await Accounting
                    .GetCurrenciesPageAsync(
                        new PageRequest
                        {
                            PageNumber = 1,
                            PageSize = 500,
                            SortBy = "Code"
                        });


            var rates =
                await Accounting
                    .GetExchangeRatesPageAsync(
                        new PageRequest
                        {
                            PageNumber = 1,
                            PageSize = 500,
                            SortBy = "RateDate",
                            SortDirection =
                                SortDirection.Descending
                        });


            /*
             * تحميل جميع الحسابات على عدة صفحات.
             *
             * هذا مهم لأن MaximumPageSize = 200.
             */
            var accounts =
                await LoadAllAccountsAsync();


            var employees =
                await Employees
                    .GetPageAsync(
                        new PageRequest
                        {
                            PageNumber = 1,
                            PageSize = 1000,
                            SortBy = "EmployeeCode"
                        });


            var employeeAccounts =
                await Accounting
                    .GetEmployeeAccountsPageAsync(
                        new PageRequest
                        {
                            PageNumber = 1,
                            PageSize = 1000
                        });


            _currencies =
                currencies.Items
                    .ToList();


            _rates =
                rates.Items
                    .ToList();


            _accounts =
                accounts;


            _employees =
                employees.Items
                    .ToList();


            _employeeAccounts =
                employeeAccounts.Items
                    .ToList();


            _settings =
                await Accounting
                    .GetAccountingSettingsAsync();


            ApplySettings(
                _settings);
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(
                ex.Error);
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

                                ExchangeRateType.Accounting,

                                _settings?
                                    .RowVersion));


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
        var currencies =
            await Accounting
                .GetCurrenciesPageAsync(
                    new PageRequest
                    {
                        PageNumber = 1,
                        PageSize = 500,
                        SortBy = "Code"
                    });


        var rates =
            await Accounting
                .GetExchangeRatesPageAsync(
                    new PageRequest
                    {
                        PageNumber = 1,
                        PageSize = 500,
                        SortBy = "RateDate",
                        SortDirection =
                            SortDirection.Descending
                    });


        _currencies =
            currencies.Items
                .ToList();


        _rates =
            rates.Items
                .ToList();
    }


    // ================================================================
    // إعادة تحميل الربط المحاسبي للموظفين
    // ================================================================

    private async Task
        ReloadEmployeeAccountsAsync()
    {
        var page =
            await Accounting
                .GetEmployeeAccountsPageAsync(
                    new PageRequest
                    {
                        PageNumber = 1,
                        PageSize = 1000
                    });


        _employeeAccounts =
            page.Items
                .ToList();
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
            ApiFeedback.Show(
                ex.Error);
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
}