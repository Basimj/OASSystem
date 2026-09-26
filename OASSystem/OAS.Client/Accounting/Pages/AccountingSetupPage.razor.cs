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


    private bool _busy;

    private List<CurrencyDto> _currencies = [];
    private List<ExchangeRateDto> _rates = [];
    private List<AccountDto> _accounts = [];
    private List<EmployeeDto> _employees = [];
    private List<EmployeeAccountDto> _employeeAccounts = [];

    private AccountingSettingsDto? _settings;


    private Guid? _settingsBaseCurrencyId;
    private Guid? _settingsEmployeeParentAccountId;
    private Guid? _settingsCashParentAccountId;
    private Guid? _settingsBankParentAccountId;
    private Guid? _settingsExchangeGainAccountId;
    private Guid? _settingsExchangeLossAccountId;


    private Guid? _editingCurrencyId;
    private string _editingCurrencyRowVersion = string.Empty;

    private string _currencyCode = string.Empty;
    private string _currencyNameAr = string.Empty;
    private string? _currencyNameEn;
    private string? _currencySymbol;

    private int _currencyDecimalPlaces = 2;
    private bool _currencyIsActive = true;


    private Guid? _editingRateId;
    private string _editingRateRowVersion = string.Empty;

    private Guid? _rateCurrencyId;

    private DateOnly? _rateDate =
        DateOnly.FromDateTime(DateTime.Today);

    private decimal _rateValue = 1m;

    private ExchangeRateType _rateType =
        ExchangeRateType.Accounting;

    private bool _rateIsActive = true;


    private bool IsSelectedRateBaseCurrency =>
        _rateCurrencyId.HasValue &&
        IsBaseCurrency(_rateCurrencyId.Value);


    private Guid? _employeeToActivateId;


    private IEnumerable<AccountDto> ParentAccountOptions =>
        _accounts
            .Where(x =>
                x.IsActive &&
                x.IsControlAccount &&
                x.AccountClass == AccountClass.Asset &&
                x.NormalBalance == NormalBalance.Debit)
            .OrderBy(x => x.Code);


    private IEnumerable<AccountDto> PostingAccountOptions =>
        _accounts
            .Where(x =>
                x.IsActive &&
                x.IsPostingAccount)
            .OrderBy(x => x.Code);


    private IEnumerable<EmployeeDto> AvailableEmployees
    {
        get
        {
            var mapped = _employeeAccounts
                .Select(x => x.EmployeeId)
                .ToHashSet();

            return _employees
                .Where(x =>
                    x.IsActive &&
                    !mapped.Contains(x.Id))
                .OrderBy(x => x.EmployeeCode);
        }
    }


    protected override Task OnInitializedAsync()
        => LoadAsync();


    private async Task LoadAsync()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;

        try
        {
            var currencies =
                await Accounting.GetCurrenciesPageAsync(
                    new PageRequest
                    {
                        PageNumber = 1,
                        PageSize = 500,
                        SortBy = "Code"
                    });

            var rates =
                await Accounting.GetExchangeRatesPageAsync(
                    new PageRequest
                    {
                        PageNumber = 1,
                        PageSize = 500,
                        SortBy = "RateDate",
                        SortDirection = SortDirection.Descending
                    });

            var accounts =
                await Accounting.GetAccountsPageAsync(
                    new PageRequest
                    {
                        PageNumber = 1,
                        PageSize = 1000,
                        SortBy = "Code"
                    });

            var employees =
                await Employees.GetPageAsync(
                    new PageRequest
                    {
                        PageNumber = 1,
                        PageSize = 1000,
                        SortBy = "EmployeeCode"
                    });

            var employeeAccounts =
                await Accounting.GetEmployeeAccountsPageAsync(
                    new PageRequest
                    {
                        PageNumber = 1,
                        PageSize = 1000
                    });

            _currencies = currencies.Items.ToList();
            _rates = rates.Items.ToList();
            _accounts = accounts.Items.ToList();
            _employees = employees.Items.ToList();
            _employeeAccounts = employeeAccounts.Items.ToList();

            _settings =
                await Accounting.GetAccountingSettingsAsync();

            ApplySettings(_settings);
        }
        catch (ApiClientException ex)
        {
            ApiFeedback.Show(ex.Error);
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


    private void ApplySettings(
        AccountingSettingsDto? settings)
    {
        _settingsBaseCurrencyId =
            settings?.BaseCurrencyId;

        _settingsEmployeeParentAccountId =
            settings?.EmployeeParentAccountId;

        _settingsCashParentAccountId =
            settings?.CashParentAccountId;

        _settingsBankParentAccountId =
            settings?.BankParentAccountId;

        _settingsExchangeGainAccountId =
            settings?.ExchangeGainAccountId;

        _settingsExchangeLossAccountId =
            settings?.ExchangeLossAccountId;
    }


    private async Task SaveSettingsAsync()
        => await RunAsync(async () =>
        {
            if (!_settingsBaseCurrencyId.HasValue)
            {
                Snackbar.Warning(
                    "اختر العملة الأساسية أولاً.");

                return;
            }

            var updated =
                await Accounting.UpdateAccountingSettingsAsync(
                    new UpdateAccountingSettingsRequest(
                        _settingsBaseCurrencyId.Value,
                        _settingsEmployeeParentAccountId,
                        _settingsCashParentAccountId,
                        _settingsBankParentAccountId,
                        _settingsExchangeGainAccountId,
                        _settingsExchangeLossAccountId,
                        ExchangeRateType.Accounting,
                        _settings?.RowVersion));

            if (updated is not null)
            {
                _settings = updated;

                ApplySettings(updated);

                if (IsSelectedRateBaseCurrency)
                {
                    _rateValue = 1m;
                }

                Snackbar.Success(
                    "تم حفظ إعدادات المحاسبة.");
            }
        });


    private async Task SaveCurrencyAsync()
        => await RunAsync(async () =>
        {
            var code =
                _currencyCode
                    .Trim()
                    .ToUpperInvariant();

            var name =
                _currencyNameAr.Trim();


            if (string.IsNullOrWhiteSpace(code))
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


            if (string.IsNullOrWhiteSpace(name))
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


            if (!string.IsNullOrWhiteSpace(_currencyNameEn) &&
                _currencyNameEn.Trim().Length > 100)
            {
                Snackbar.Warning(
                    "اسم العملة بالإنجليزي يجب ألا يزيد عن 100 حرف.");

                return;
            }


            if (!string.IsNullOrWhiteSpace(_currencySymbol) &&
                _currencySymbol.Trim().Length > 12)
            {
                Snackbar.Warning(
                    "رمز العملة يجب ألا يزيد عن 12 حرفًا.");

                return;
            }


            if (_currencyDecimalPlaces is < 0 or > 6)
            {
                Snackbar.Warning(
                    "عدد الخانات العشرية يجب أن يكون بين 0 و6.");

                return;
            }


            CurrencyDto? result;


            if (_editingCurrencyId.HasValue)
            {
                result =
                    await Accounting.UpdateCurrencyAsync(
                        _editingCurrencyId.Value,
                        new UpdateCurrencyRequest(
                            name,
                            N(_currencyNameEn),
                            N(_currencySymbol),
                            checked((byte)_currencyDecimalPlaces),
                            _currencyIsActive,
                            _editingCurrencyRowVersion));
            }
            else
            {
                result =
                    await Accounting.CreateCurrencyAsync(
                        new CreateCurrencyRequest(
                            code,
                            name,
                            N(_currencyNameEn),
                            N(_currencySymbol),
                            checked((byte)_currencyDecimalPlaces),
                            _currencyIsActive));
            }


            if (result is not null)
            {
                Snackbar.Success(
                    "تم حفظ العملة.");

                await CancelCurrencyEdit();

                await ReloadReferenceListsAsync();
            }
        });


    private void EditCurrency(
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


    private Task CancelCurrencyEdit()
    {
        _editingCurrencyId = null;
        _editingCurrencyRowVersion = string.Empty;

        _currencyCode = string.Empty;
        _currencyNameAr = string.Empty;
        _currencyNameEn = null;
        _currencySymbol = null;

        _currencyDecimalPlaces = 2;
        _currencyIsActive = true;

        return Task.CompletedTask;
    }


    private async Task SaveRateAsync()
        => await RunAsync(async () =>
        {
            if (!_rateCurrencyId.HasValue)
            {
                Snackbar.Warning(
                    "اختر العملة.");

                return;
            }


            if (!_rateDate.HasValue)
            {
                Snackbar.Warning(
                    "تاريخ سعر الصرف مطلوب.");

                return;
            }


            /*
             * العملة الأساسية سعرها دائمًا 1.
             * حتى لو حاولت الواجهة أو المستخدم وضع قيمة أخرى
             * نعيدها إلى 1 قبل إرسال الطلب.
             */
            if (IsBaseCurrency(_rateCurrencyId.Value))
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
                    await Accounting.UpdateExchangeRateAsync(
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
                    await Accounting.CreateExchangeRateAsync(
                        new CreateExchangeRateRequest(
                            _rateCurrencyId.Value,
                            _rateDate.Value,
                            _rateValue,
                            _rateType,
                            _rateIsActive));
            }


            if (result is not null)
            {
                Snackbar.Success(
                    IsSelectedRateBaseCurrency
                        ? "تم حفظ سعر العملة الأساسية بقيمة 1."
                        : "تم حفظ سعر الصرف.");

                await CancelRateEdit();

                await ReloadReferenceListsAsync();
            }
        });


    private void EditRate(
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

        /*
         * إذا كان هناك سجل قديم بقيمة غير صحيحة
         * للعملة الأساسية نعيد عرضه مباشرة بالقيمة الصحيحة.
         */
        _rateValue =
            IsBaseCurrency(item.CurrencyId)
                ? 1m
                : item.Rate;

        _rateType =
            item.RateType;

        _rateIsActive =
            item.IsActive;
    }


    private Task CancelRateEdit()
    {
        _editingRateId = null;
        _editingRateRowVersion = string.Empty;

        _rateCurrencyId = null;

        _rateDate =
            DateOnly.FromDateTime(
                DateTime.Today);

        _rateValue = 1m;

        _rateType =
            ExchangeRateType.Accounting;

        _rateIsActive = true;

        return Task.CompletedTask;
    }


    private void OnRateCurrencyChanged()
    {
        if (IsSelectedRateBaseCurrency)
        {
            _rateValue = 1m;
        }
    }


    private bool IsBaseCurrency(
        Guid currencyId)
    {
        return
            _settingsBaseCurrencyId.HasValue &&
            _settingsBaseCurrencyId.Value == currencyId;
    }


    private static string FormatExchangeRate(
        decimal value)
    {
        /*
         * يمنع:
         * 1.00000000
         * 550.00000000
         *
         * ويعرض:
         * 1
         * 550
         * 146.67
         * 1.23456789
         */
        return value.ToString("0.########");
    }


    private async Task ActivateEmployeeAsync()
        => await RunAsync(async () =>
        {
            if (!_employeeToActivateId.HasValue)
            {
                return;
            }

            var result =
                await Accounting.ActivateEmployeeAccountAsync(
                    new ActivateEmployeeAccountRequest(
                        _employeeToActivateId.Value,
                        true));

            if (result is not null)
            {
                Snackbar.Success(
                    "تم تفعيل الموظف محاسبيًا وإنشاء حسابه المرتبط.");

                _employeeToActivateId = null;

                await ReloadEmployeeAccountsAsync();
            }
        });


    private async Task ToggleEmployeeAccountAsync(
        EmployeeAccountDto item)
        => await RunAsync(async () =>
        {
            var result =
                await Accounting.SetEmployeeAccountStatusAsync(
                    item.Id,
                    new SetEmployeeAccountStatusRequest(
                        !item.IsActive,
                        item.RowVersion));

            if (result is not null)
            {
                Snackbar.Success(
                    result.IsActive
                        ? "تم تفعيل الربط المحاسبي."
                        : "تم تعطيل الربط المحاسبي.");

                await ReloadEmployeeAccountsAsync();
            }
        });


    private async Task ReloadReferenceListsAsync()
    {
        var currencies =
            await Accounting.GetCurrenciesPageAsync(
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 500,
                    SortBy = "Code"
                });

        var rates =
            await Accounting.GetExchangeRatesPageAsync(
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 500,
                    SortBy = "RateDate",
                    SortDirection = SortDirection.Descending
                });

        _currencies =
            currencies.Items.ToList();

        _rates =
            rates.Items.ToList();
    }


    private async Task ReloadEmployeeAccountsAsync()
    {
        var page =
            await Accounting.GetEmployeeAccountsPageAsync(
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 1000
                });

        _employeeAccounts =
            page.Items.ToList();
    }


    private async Task RunAsync(
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
            ApiFeedback.Show(ex.Error);
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


    private static string? N(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}