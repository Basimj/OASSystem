using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Printing.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees.Compensation;
using OAS.Contracts.Features.Employees.EndOfService;
using OAS.Contracts.Features.Employees.Payroll;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Features.Employees.Pages.Payroll;

public partial class Payroll
{
    private enum EditorMode { Empty, View, Create, Edit }

    [Inject] private IPayrollClientService Payrolls { get; set; } = default!;
    [Inject] private IEmployeeClientService Employees { get; set; } = default!;
    [Inject] private IEmployeeCompensationClientService Compensation { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IPrintingClientService Printing { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    private string _section = "runs";
    private EditorMode _mode = EditorMode.Empty;
    private bool _loading = true, _saving, _printing;

    private IReadOnlyList<PayrollPolicyDto> _policies = [];
    private IReadOnlyList<PayrollPeriodDto> _periods = [];
    private IReadOnlyList<PayrollRunDto> _runs = [];
    private IReadOnlyList<EmployeePayrollDto> _employeePayrolls = [];
    private IReadOnlyList<EndOfServiceSettlementDto> _eos = [];
    private IReadOnlyList<SalaryComponentDto> _salaryComponents = [];
    private IReadOnlyList<CurrencyDto> _currencies = [];
    private IReadOnlyList<CashAccountDto> _cashAccounts = [];
    private IReadOnlyList<BankAccountDto> _bankAccounts = [];

    private PayrollPolicyDto? _selectedPolicy;
    private PayrollPeriodDto? _selectedPeriod;
    private PayrollRunDto? _selectedRun;
    private EmployeePayrollDto? _selectedEmployeePayroll;
    private EndOfServiceSettlementDto? _selectedEos;
    private PayrollPrevalidationDto? _prevalidation;
    private PayslipDto? _payslip;

    // Period form
    private string _periodYearText = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture);
    private string _periodMonthText = DateTime.Today.Month.ToString(CultureInfo.InvariantCulture);
    private DateOnly? _periodStart = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateOnly? _periodEnd = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));

    // Run form
    private string? _runPeriodValue, _runPolicyValue, _runCurrencyValue;
    private string _runTypeText = "1";
    private DateOnly? _runCalculationDate = DateOnly.FromDateTime(DateTime.Today);
    private DateOnly? _runPostingDate = DateOnly.FromDateTime(DateTime.Today);
    private string? _runNotes;

    // Policy form
    private string? _policyName, _policyNotes;
    private DateOnly? _policyFrom = DateOnly.FromDateTime(DateTime.Today), _policyTo;
    private string _policyProrationText = "2", _policyDailyText = "1", _policyHourlyText = "1";
    private bool _policyRequireAttendance = true, _policyRequireFullPayment;
    private string? _policyAbsenceComponentValue, _policyLateComponentValue, _policyEarlyComponentValue, _policyUnpaidLeaveComponentValue, _policyOvertimeComponentValue;

    // Payment form
    private string _paymentMethodText = ((byte)PaymentMethod.Cash).ToString(CultureInfo.InvariantCulture);
    private string? _cashAccountValue, _bankAccountValue, _paymentAmountText, _paymentExchangeRateText, _paymentReference;
    private DateOnly? _paymentDate = DateOnly.FromDateTime(DateTime.Today);

    // EOS form
    private string? _eosEmployeeValue, _eosFinalPayrollValue, _eosReason;
    private UiLookupItem? _eosEmployeeItem;
    private DateOnly? _eosLastWorkingDate = DateOnly.FromDateTime(DateTime.Today);
    private IReadOnlyList<EmployeePayrollDto> _eosEmployeePayrollHistory = [];
    private string _eosBenefitText = "0", _eosLeaveDailyRateText = "", _eosOtherEarningsText = "0", _eosOtherDeductionsText = "0";
    private bool _eosSettleLeave = true, _eosSettleLoans = true;

    private static readonly IReadOnlyList<string> PeriodHeaders = ["الفترة", "من", "إلى", "الحالة"];
    private static readonly IReadOnlyList<string> RunHeaders = ["الكشف", "الفترة", "النوع", "العملة", "الصافي", "الحالة"];
    private static readonly IReadOnlyList<string> EmployeePayrollHeaders = ["الكود", "الموظف", "القسم", "الإجمالي", "الخصومات", "الصافي", "الدفع", "الحالة"];
    private static readonly IReadOnlyList<string> EosHeaders = ["التسوية", "الكود", "الموظف", "آخر يوم", "الصافي", "الحالة"];
    private static readonly IReadOnlyList<string> PolicyHeaders = ["الكود", "السياسة", "من", "إلى", "الحالة"];

    private IReadOnlyList<UiSectionTabItem> SectionTabs =>
    [
        new("periods", "الفترات", _section == "periods"),
        new("runs", "كشوف الرواتب", _section == "runs"),
        new("eos", "نهاية الخدمة", _section == "eos"),
        new("policies", "سياسات الرواتب", _section == "policies")
    ];

    private bool IsEditing => _mode is EditorMode.Create or EditorMode.Edit;
    private bool CanNew => !_saving && !IsEditing;
    private bool CanEdit => _section == "policies" && _selectedPolicy is not null && _selectedPolicy.Status == 1 && _mode == EditorMode.View;
    private bool CanSave => IsEditing && !_saving;
    private bool CanCancelEdit => IsEditing && !_saving;

    private static readonly IReadOnlyList<UiSelectOption> MonthOptions = Enumerable.Range(1, 12).Select(x => new UiSelectOption(x.ToString(CultureInfo.InvariantCulture), new DateTime(2000, x, 1).ToString("MMMM", new CultureInfo("ar-YE")))).ToArray();
    private static readonly IReadOnlyList<UiSelectOption> RunTypeOptions = [new("1", "دوري"), new("2", "نهائي")];
    private static readonly IReadOnlyList<UiSelectOption> ProrationOptions = [new("1", "بدون توزيع"), new("2", "أيام تقويمية"), new("3", "أيام العمل المجدولة")];
    private static readonly IReadOnlyList<UiSelectOption> DailyRateOptions = [new("1", "30 يوم ثابت"), new("2", "أيام الشهر"), new("3", "أيام العمل المجدولة")];
    private static readonly IReadOnlyList<UiSelectOption> HourlyRateOptions = [new("1", "الأجر اليومي ÷ ساعات العقد"), new("2", "الأساسي ÷ الساعات المجدولة")];
    private static readonly IReadOnlyList<UiSelectOption> PaymentMethodOptions = [new(((byte)PaymentMethod.Cash).ToString(), "نقدًا"), new(((byte)PaymentMethod.BankTransfer).ToString(), "تحويل بنكي")];

    private IReadOnlyList<UiSelectOption> PeriodOptions => _periods.Where(x => x.Status == 1).OrderByDescending(x => x.StartDate).Select(x => new UiSelectOption(x.Id.ToString("D"), x.PeriodCode)).ToArray();
    private IReadOnlyList<UiSelectOption> PolicyOptions => _policies.Where(x => x.Status == 2).OrderByDescending(x => x.EffectiveFrom).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.PolicyCode} - {x.NameAr}")).ToArray();
    private IReadOnlyList<UiSelectOption> CurrencyOptions => _currencies.Where(x => x.IsActive).OrderBy(x => x.Code).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} - {x.NameAr}")).ToArray();
    private IReadOnlyList<UiSelectOption> DeductionComponentOptions => WithEmpty(_salaryComponents.Where(x => x.IsActive && x.ComponentType == 2).OrderBy(x => x.DisplayOrder).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.ComponentCode} - {x.NameAr}")));
    private IReadOnlyList<UiSelectOption> EarningComponentOptions => WithEmpty(_salaryComponents.Where(x => x.IsActive && x.ComponentType == 1).OrderBy(x => x.DisplayOrder).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.ComponentCode} - {x.NameAr}")));
    private IReadOnlyList<UiSelectOption> CashAccountOptions => _cashAccounts.Where(x => x.IsActive && PaymentCurrencyMatches(x.CurrencyId)).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} - {x.Name}")).ToArray();
    private IReadOnlyList<UiSelectOption> BankAccountOptions => _bankAccounts.Where(x => x.IsActive && PaymentCurrencyMatches(x.CurrencyId)).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} - {x.BankName} / {x.AccountName}")).ToArray();
    private IReadOnlyList<UiSelectOption> FinalPayrollOptions
    {
        get
        {
            var finalRunIds = _runs.Where(x => x.RunType == 2 && x.Status is 5 or 6).Select(x => x.Id).ToHashSet();
            return _eosEmployeePayrollHistory.Where(x => x.Status == 4 && finalRunIds.Contains(x.PayrollRunId)).OrderByDescending(x => x.CoverageTo).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.CoverageFrom:yyyy-MM-dd} → {x.CoverageTo:yyyy-MM-dd} | {x.NetPay:N2} {x.CurrencyCode}")).ToArray();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadReferenceDataAsync();
        await LoadSectionAsync();
    }

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            var policiesTask = Payrolls.GetPoliciesAsync();
            var periodsTask = Payrolls.GetPeriodsAsync();
            var runsTask = Payrolls.GetRunsAsync();
            var componentsTask = Compensation.GetComponentsAsync(activeOnly: true);
            var currenciesTask = Accounting.GetCurrenciesPageAsync(new PageRequest { PageNumber = 1, PageSize = 200, SortBy = "Code" });
            var cashTask = Accounting.GetCashAccountsPageAsync(new PageRequest { PageNumber = 1, PageSize = 200, SortBy = "Code" });
            var bankTask = Accounting.GetBankAccountsPageAsync(new PageRequest { PageNumber = 1, PageSize = 200, SortBy = "Code" });
            await Task.WhenAll(policiesTask, periodsTask, runsTask, componentsTask, currenciesTask, cashTask, bankTask);
            _policies = await policiesTask;
            _periods = await periodsTask;
            _runs = await runsTask;
            _salaryComponents = await componentsTask;
            _currencies = (await currenciesTask).Items;
            _cashAccounts = (await cashTask).Items;
            _bankAccounts = (await bankTask).Items;
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
    }

    private async Task LoadSectionAsync()
    {
        _loading = true;
        try
        {
            switch (_section)
            {
                case "periods": _periods = await Payrolls.GetPeriodsAsync(); break;
                case "runs": _runs = await Payrolls.GetRunsAsync(); break;
                case "eos": _eos = await Payrolls.GetEndOfServiceAsync(); break;
                case "policies": _policies = await Payrolls.GetPoliciesAsync(); break;
            }
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _loading = false; }
    }

    private async Task ChangeSectionAsync(string key)
    {
        if (_section == key) return;
        if (IsEditing) { Snackbar.Info("احفظ أو ألغِ التعديلات الحالية قبل الانتقال."); return; }
        _section = key; ClearSelection(); await LoadSectionAsync();
    }

    private void ClearSelection()
    {
        _mode = EditorMode.Empty; _selectedPolicy = null; _selectedPeriod = null; _selectedRun = null; _selectedEmployeePayroll = null; _selectedEos = null; _prevalidation = null; _payslip = null;
    }

    private async Task RefreshAsync(MouseEventArgs _)
    {
        if (IsEditing) return;
        await LoadReferenceDataAsync();
        await LoadSectionAsync();
        if (_selectedRun is not null) await ReloadSelectedRunAsync();
    }

    private Task NewAsync(MouseEventArgs _)
    {
        if (!CanNew) return Task.CompletedTask;
        _mode = EditorMode.Create;
        switch (_section)
        {
            case "periods": ClearPeriodForm(); break;
            case "runs": ClearRunForm(); break;
            case "policies": _selectedPolicy = null; ClearPolicyForm(); break;
            case "eos": _selectedEos = null; ClearEosForm(); break;
        }
        return Task.CompletedTask;
    }

    private Task EditAsync(MouseEventArgs _)
    {
        if (!CanEdit || _selectedPolicy is null) return Task.CompletedTask;
        _mode = EditorMode.Edit; LoadPolicyForm(_selectedPolicy); return Task.CompletedTask;
    }

    private Task CancelEditAsync(MouseEventArgs _)
    {
        if (_section == "policies" && _selectedPolicy is not null) { _mode = EditorMode.View; LoadPolicyForm(_selectedPolicy); }
        else _mode = EditorMode.Empty;
        return Task.CompletedTask;
    }

    private async Task SaveAsync(MouseEventArgs _)
    {
        if (!CanSave) return;
        _saving = true;
        try
        {
            switch (_section)
            {
                case "periods": await SavePeriodAsync(); break;
                case "runs": await SaveRunAsync(); break;
                case "policies": await SavePolicyAsync(); break;
                case "eos": await SaveEosAsync(); break;
            }
        }
        finally { _saving = false; }
    }

    private async Task SavePeriodAsync()
    {
        if (!short.TryParse(_periodYearText, out var year) || !byte.TryParse(_periodMonthText, out var month) || _periodStart is not DateOnly from || _periodEnd is not DateOnly to) { Snackbar.Error("تحقق من السنة والشهر وتواريخ الفترة."); return; }
        var result = await Payrolls.CreatePeriodAsync(new CreatePayrollPeriodRequest(year, month, from, to));
        if (!Handle(result)) return;
        _mode = EditorMode.Empty; _periods = await Payrolls.GetPeriodsAsync(); _selectedPeriod = _periods.FirstOrDefault(x => x.Id == result.Value); Snackbar.Success("تم إنشاء فترة الرواتب.");
    }

    private async Task SaveRunAsync()
    {
        if (!Guid.TryParse(_runPeriodValue, out var periodId) || !Guid.TryParse(_runPolicyValue, out var policyId) || !Guid.TryParse(_runCurrencyValue, out var currencyId) || !byte.TryParse(_runTypeText, out var type) || _runCalculationDate is not DateOnly calculationDate) { Snackbar.Error("حدد الفترة والسياسة والعملة ونوع الكشف."); return; }
        var result = await Payrolls.CreateRunAsync(new CreatePayrollRunRequest(periodId, type, calculationDate, _runPostingDate, policyId, currencyId, Null(_runNotes)));
        if (!Handle(result)) return;
        _mode = EditorMode.Empty; _runs = await Payrolls.GetRunsAsync(); var run = _runs.FirstOrDefault(x => x.Id == result.Value); if (run is not null) await SelectRunAsync(run); Snackbar.Success("تم إنشاء كشف الرواتب.");
    }

    private async Task SavePolicyAsync()
    {
        if (string.IsNullOrWhiteSpace(_policyName) || _policyFrom is not DateOnly from || !byte.TryParse(_policyProrationText, out var p) || !byte.TryParse(_policyDailyText, out var d) || !byte.TryParse(_policyHourlyText, out var h)) { Snackbar.Error("تحقق من بيانات سياسة الرواتب."); return; }
        ApiCallResult<Guid> result;
        if (_mode == EditorMode.Create)
            result = await Payrolls.CreatePolicyAsync(new CreatePayrollPolicyRequest(_policyName.Trim(), from, _policyTo, p, d, h, _policyRequireAttendance, _policyRequireFullPayment, GuidOrNull(_policyAbsenceComponentValue), GuidOrNull(_policyLateComponentValue), GuidOrNull(_policyEarlyComponentValue), GuidOrNull(_policyUnpaidLeaveComponentValue), GuidOrNull(_policyOvertimeComponentValue), Null(_policyNotes)));
        else if (_selectedPolicy is not null)
            result = await Payrolls.UpdatePolicyAsync(_selectedPolicy.Id, new UpdatePayrollPolicyRequest(_policyName.Trim(), from, _policyTo, p, d, h, _policyRequireAttendance, _policyRequireFullPayment, GuidOrNull(_policyAbsenceComponentValue), GuidOrNull(_policyLateComponentValue), GuidOrNull(_policyEarlyComponentValue), GuidOrNull(_policyUnpaidLeaveComponentValue), GuidOrNull(_policyOvertimeComponentValue), Null(_policyNotes), _selectedPolicy.RowVersion));
        else return;
        if (!Handle(result)) return;
        _policies = await Payrolls.GetPoliciesAsync(); _selectedPolicy = _policies.FirstOrDefault(x => x.Id == result.Value) ?? (_selectedPolicy is null ? null : _policies.FirstOrDefault(x => x.Id == _selectedPolicy.Id)); _mode = _selectedPolicy is null ? EditorMode.Empty : EditorMode.View; if (_selectedPolicy is not null) LoadPolicyForm(_selectedPolicy); Snackbar.Success("تم حفظ سياسة الرواتب.");
    }

    private async Task SaveEosAsync()
    {
        if (!Guid.TryParse(_eosEmployeeValue, out var employeeId) || _eosLastWorkingDate is not DateOnly lastWorking) { Snackbar.Error("حدد الموظف وآخر يوم عمل."); return; }
        var finalId = GuidOrNull(_eosFinalPayrollValue);
        var result = await Payrolls.CreateEndOfServiceAsync(new CreateEndOfServiceSettlementRequest(employeeId, null, finalId, lastWorking, Null(_eosReason)));
        if (!Handle(result)) return;
        _mode = EditorMode.Empty; _eos = await Payrolls.GetEndOfServiceAsync(); var row = _eos.FirstOrDefault(x => x.Id == result.Value); if (row is not null) await SelectEosAsync(row); Snackbar.Success("تم إنشاء تسوية نهاية الخدمة.");
    }

    private void SelectPeriod(PayrollPeriodDto item) { if (IsEditing) return; _selectedPeriod = item; _mode = EditorMode.View; }
    private void SelectPolicy(PayrollPolicyDto item) { if (IsEditing) return; _selectedPolicy = item; _mode = EditorMode.View; LoadPolicyForm(item); }
    private async Task SelectRunAsync(PayrollRunDto item)
    {
        if (IsEditing) return;
        _selectedRun = item; _selectedEmployeePayroll = null; _payslip = null; _prevalidation = null; _mode = EditorMode.View;
        _employeePayrolls = await Payrolls.GetRunEmployeesAsync(item.Id);
    }
    private void SelectEmployeePayroll(EmployeePayrollDto item)
    {
        if (IsEditing) return;
        _selectedEmployeePayroll = item; _payslip = null; ResetPayment(item.OutstandingAmount);
    }
    private async Task SelectEosAsync(EndOfServiceSettlementDto item)
    {
        if (IsEditing) return;
        _selectedEos = await Payrolls.GetEndOfServiceAsync(item.Id) ?? item; _mode = EditorMode.View; _eosBenefitText = _selectedEos.EndOfServiceBenefitAmount.ToString(CultureInfo.InvariantCulture); _eosOtherEarningsText = _selectedEos.OtherEarningsAmount.ToString(CultureInfo.InvariantCulture); _eosOtherDeductionsText = _selectedEos.OtherDeductionsAmount.ToString(CultureInfo.InvariantCulture); ResetPayment(_selectedEos.OutstandingAmount);
    }

    private async Task PrevalidateRunAsync(MouseEventArgs _) { if (_selectedRun is null) return; _prevalidation = await Payrolls.GetPrevalidationAsync(_selectedRun.Id); if (_prevalidation?.CanCalculate == true) Snackbar.Success("الكشف جاهز للاحتساب."); }
    private async Task CalculateRunAsync(MouseEventArgs _) => await RunTransition(Payrolls.CalculateRunAsync, "تم احتساب كشف الرواتب.");
    private async Task ReviewRunAsync(MouseEventArgs _) => await RunTransition(Payrolls.ReviewRunAsync, "تمت مراجعة كشف الرواتب.");
    private async Task ApproveRunAsync(MouseEventArgs _) => await RunTransition(Payrolls.ApproveRunAsync, "تم اعتماد كشف الرواتب.");
    private async Task ReopenRunAsync(MouseEventArgs _) => await RunTransition(Payrolls.ReopenRunAsync, "تمت إعادة فتح كشف الرواتب.");
    private async Task PostRunAsync(MouseEventArgs _) => await RunTransition(Payrolls.PostRunAsync, "تم ترحيل الرواتب إلى المحاسبة.");
    private async Task CloseRunAsync(MouseEventArgs _) => await RunTransition(Payrolls.CloseRunAsync, "تم إغلاق كشف الرواتب.");
    private async Task CancelRunAsync(MouseEventArgs _) => await RunTransition(Payrolls.CancelRunAsync, "تم إلغاء كشف الرواتب.");

    private async Task RunTransition(Func<Guid, PayrollLifecycleRequest, CancellationToken, Task<ApiCallResult<Guid>>> action, string success)
    {
        if (_selectedRun is null || _saving) return; _saving = true;
        try { var result = await action(_selectedRun.Id, new PayrollLifecycleRequest(_selectedRun.RowVersion), CancellationToken.None); if (!Handle(result)) return; Snackbar.Success(success); await ReloadSelectedRunAsync(); }
        finally { _saving = false; }
    }

    private async Task ReviewEmployeeAsync(MouseEventArgs _)
    {
        if (_selectedEmployeePayroll is null) return; _saving = true;
        try { var result = await Payrolls.ReviewEmployeePayrollAsync(_selectedEmployeePayroll.Id, new PayrollLifecycleRequest(_selectedEmployeePayroll.RowVersion)); if (!Handle(result)) return; Snackbar.Success("تمت مراجعة راتب الموظف."); await ReloadSelectedRunAsync(_selectedEmployeePayroll.Id); }
        finally { _saving = false; }
    }

    private async Task ReloadSelectedRunAsync(Guid? employeePayrollId = null)
    {
        if (_selectedRun is null) return;
        _runs = await Payrolls.GetRunsAsync(); _selectedRun = _runs.FirstOrDefault(x => x.Id == _selectedRun.Id);
        if (_selectedRun is null) { _employeePayrolls = []; _selectedEmployeePayroll = null; return; }
        _employeePayrolls = await Payrolls.GetRunEmployeesAsync(_selectedRun.Id);
        var id = employeePayrollId ?? _selectedEmployeePayroll?.Id; _selectedEmployeePayroll = id.HasValue ? _employeePayrolls.FirstOrDefault(x => x.Id == id.Value) : null;
        if (_selectedEmployeePayroll is not null) ResetPayment(_selectedEmployeePayroll.OutstandingAmount);
    }

    private async Task PayEmployeeAsync(MouseEventArgs _)
    {
        if (_selectedEmployeePayroll is null || !TryPayment(_selectedEmployeePayroll.CurrencyId, out var payment)) return;
        _saving = true;
        try
        {
            var result = await Payrolls.PayEmployeePayrollAsync(_selectedEmployeePayroll.Id, new SalaryPaymentRequest(payment.Date, payment.Amount, payment.Method, payment.CashId, payment.BankId, payment.SettlementAccountId, payment.ExchangeRate, (byte)ExchangeRateType.Accounting, Null(_paymentReference), $"راتب {_selectedEmployeePayroll.EmployeeName}"));
            if (!Handle(result)) return; Snackbar.Success("تم دفع الراتب وترحيل سند الصرف."); await ReloadSelectedRunAsync(_selectedEmployeePayroll.Id);
        }
        finally { _saving = false; }
    }

    private async Task LoadPayslipAsync(MouseEventArgs _)
    {
        if (_selectedEmployeePayroll is null) return; _payslip = await Payrolls.GetPayslipAsync(_selectedEmployeePayroll.Id); if (_payslip is not null) Snackbar.Success("تم تحميل قسيمة الراتب.");
    }

    private async Task PrintPayslipAsync(MouseEventArgs _)
    {
        if (_selectedEmployeePayroll is null || _printing) return;
        _printing = true;
        try
        {
            var job = await Printing.PrintPayslipAsync(_selectedEmployeePayroll.Id);
            if (job is null) { Snackbar.Error("تعذر إرسال قسيمة الراتب إلى الطباعة."); return; }
            Snackbar.Success("تم إرسال قسيمة الراتب إلى عميل الطباعة.");
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _printing = false; }
    }

    private async Task LockPeriodAsync(MouseEventArgs _) => await PeriodTransition(Payrolls.LockPeriodAsync, "تم قفل الفترة.");
    private async Task ReopenPeriodAsync(MouseEventArgs _) => await PeriodTransition(Payrolls.ReopenPeriodAsync, "تمت إعادة فتح الفترة.");
    private async Task ClosePeriodAsync(MouseEventArgs _) => await PeriodTransition(Payrolls.ClosePeriodAsync, "تم إغلاق الفترة.");
    private async Task PeriodTransition(Func<Guid, PayrollLifecycleRequest, CancellationToken, Task<ApiCallResult<Guid>>> action, string success)
    {
        if (_selectedPeriod is null) return; var result = await action(_selectedPeriod.Id, new PayrollLifecycleRequest(_selectedPeriod.RowVersion), CancellationToken.None); if (!Handle(result)) return; _periods = await Payrolls.GetPeriodsAsync(); _selectedPeriod = _periods.FirstOrDefault(x => x.Id == _selectedPeriod.Id); Snackbar.Success(success);
    }

    private async Task ActivatePolicyAsync(MouseEventArgs _)
    {
        if (_selectedPolicy is null) return; var result = await Payrolls.ActivatePolicyAsync(_selectedPolicy.Id, new PayrollLifecycleRequest(_selectedPolicy.RowVersion)); if (!Handle(result)) return; await ReloadPoliciesAsync(_selectedPolicy.Id); Snackbar.Success("تم تفعيل سياسة الرواتب.");
    }
    private async Task CancelPolicyAsync(MouseEventArgs _)
    {
        if (_selectedPolicy is null) return; var result = await Payrolls.CancelPolicyAsync(_selectedPolicy.Id, new PayrollLifecycleRequest(_selectedPolicy.RowVersion)); if (!Handle(result)) return; await ReloadPoliciesAsync(_selectedPolicy.Id); Snackbar.Success("تم إلغاء سياسة الرواتب.");
    }
    private async Task ReloadPoliciesAsync(Guid id) { _policies = await Payrolls.GetPoliciesAsync(); _selectedPolicy = _policies.FirstOrDefault(x => x.Id == id); if (_selectedPolicy is not null) LoadPolicyForm(_selectedPolicy); }

    private async Task CalculateEosAsync(MouseEventArgs _)
    {
        if (_selectedEos is null || !TryDecimal(_eosBenefitText, out var benefit) || !TryDecimal(_eosOtherEarningsText, out var earnings) || !TryDecimal(_eosOtherDeductionsText, out var deductions)) { Snackbar.Error("تحقق من مبالغ تسوية نهاية الخدمة."); return; }
        decimal? leaveDailyRate=null;if(_eosSettleLeave){if(!TryDecimal(_eosLeaveDailyRateText,out var parsedLeaveRate)||parsedLeaveRate<=0){Snackbar.Error("أدخل سعر يوم تسوية الإجازة المعتمد.");return;}leaveDailyRate=parsedLeaveRate;}
        var result = await Payrolls.CalculateEndOfServiceAsync(_selectedEos.Id, new CalculateEndOfServiceRequest(_selectedEos.RowVersion, benefit, earnings, deductions, _eosSettleLeave, _eosSettleLoans, leaveDailyRate)); if (!Handle(result)) return; await ReloadEosAsync(_selectedEos.Id); Snackbar.Success("تم احتساب تسوية نهاية الخدمة.");
    }
    private async Task ReviewEosAsync(MouseEventArgs _) => await EosTransition(Payrolls.ReviewEndOfServiceAsync, "تمت مراجعة التسوية.");
    private async Task ApproveEosAsync(MouseEventArgs _) => await EosTransition(Payrolls.ApproveEndOfServiceAsync, "تم اعتماد التسوية.");
    private async Task PostEosAsync(MouseEventArgs _) => await EosTransition(Payrolls.PostEndOfServiceAsync, "تم ترحيل تسوية نهاية الخدمة.");
    private async Task CompleteEosAsync(MouseEventArgs _) => await EosTransition(Payrolls.CompleteEndOfServiceAsync, "تم إكمال إنهاء الخدمة وتعطيل الموظف.");
    private async Task EosTransition(Func<Guid, EndOfServiceTransitionRequest, CancellationToken, Task<ApiCallResult<Guid>>> action, string success)
    {
        if (_selectedEos is null) return; var result = await action(_selectedEos.Id, new EndOfServiceTransitionRequest(_selectedEos.RowVersion), CancellationToken.None); if (!Handle(result)) return; await ReloadEosAsync(_selectedEos.Id); Snackbar.Success(success);
    }

    private async Task PayEosAsync(MouseEventArgs _)
    {
        if (_selectedEos is null || !TryPayment(_selectedEos.CurrencyId, out var payment)) return;
        var result = await Payrolls.PayEndOfServiceAsync(_selectedEos.Id, new EndOfServicePaymentRequest(payment.Date, payment.Amount, payment.Method, payment.CashId, payment.BankId, payment.SettlementAccountId, payment.ExchangeRate, (byte)ExchangeRateType.Accounting, Null(_paymentReference), $"تسوية نهاية خدمة {_selectedEos.EmployeeName}")); if (!Handle(result)) return; await ReloadEosAsync(_selectedEos.Id); Snackbar.Success("تم دفع تسوية نهاية الخدمة.");
    }
    private async Task ReloadEosAsync(Guid id) { _eos = await Payrolls.GetEndOfServiceAsync(); _selectedEos = await Payrolls.GetEndOfServiceAsync(id); if (_selectedEos is null) _selectedEos = _eos.FirstOrDefault(x => x.Id == id); if (_selectedEos is not null) ResetPayment(_selectedEos.OutstandingAmount); }

    private async Task<IReadOnlyList<UiLookupItem>> SearchEmployeesAsync(string search, CancellationToken ct)
    {
        try
        {
            var page = await Employees.GetPageAsync(new PageRequest { PageNumber = 1, PageSize = 30, Search = search, SortBy = "EmployeeCode" }, ct);
            return page.Items.Select(x => new UiLookupItem(x.Id.ToString("D"), x.DisplayName, x.EmployeeCode, "fa-solid fa-user-tie")).ToArray();
        }
        catch { return []; }
    }

    private async Task SetEosEmployeeAsync(string? value)
    {
        _eosEmployeeValue = value; _eosFinalPayrollValue = null; _eosEmployeePayrollHistory = [];
        if (!Guid.TryParse(value, out var id)) { _eosEmployeeItem = null; return; }
        try
        {
            var e = await Employees.GetByIdAsync(id); _eosEmployeeItem = new UiLookupItem(id.ToString("D"), e.DisplayName, e.EmployeeCode, "fa-solid fa-user-tie"); _eosEmployeePayrollHistory = await Payrolls.GetEmployeeHistoryAsync(id);
        }
        catch { _eosEmployeeItem = null; }
    }

    private bool TryPayment(Guid currencyId, out PaymentInput payment)
    {
        payment = default;
        if (_paymentDate is not DateOnly date || !TryDecimal(_paymentAmountText, out var amount) || amount <= 0) { Snackbar.Error("حدد تاريخ الدفع ومبلغًا صحيحًا."); return false; }
        if (!byte.TryParse(_paymentMethodText, out var method) || (method != (byte)PaymentMethod.Cash && method != (byte)PaymentMethod.BankTransfer)) { Snackbar.Error("اختر طريقة الدفع."); return false; }
        Guid? cashId = null, bankId = null; Guid settlementAccountId;
        if (method == (byte)PaymentMethod.Cash)
        {
            if (!Guid.TryParse(_cashAccountValue, out var id)) { Snackbar.Error("اختر الصندوق."); return false; }
            var cash = _cashAccounts.FirstOrDefault(x => x.Id == id && x.CurrencyId == currencyId); if (cash is null) { Snackbar.Error("عملة الصندوق يجب أن تطابق عملة المستند."); return false; } cashId = cash.Id; settlementAccountId = cash.AccountId;
        }
        else
        {
            if (!Guid.TryParse(_bankAccountValue, out var id)) { Snackbar.Error("اختر الحساب البنكي."); return false; }
            var bank = _bankAccounts.FirstOrDefault(x => x.Id == id && x.CurrencyId == currencyId); if (bank is null) { Snackbar.Error("عملة الحساب البنكي يجب أن تطابق عملة المستند."); return false; } bankId = bank.Id; settlementAccountId = bank.AccountId;
        }
        decimal? exchange = null; if (!string.IsNullOrWhiteSpace(_paymentExchangeRateText)) { if (!TryDecimal(_paymentExchangeRateText, out var rate) || rate <= 0) { Snackbar.Error("سعر الصرف غير صالح."); return false; } exchange = rate; }
        payment = new(date, amount, method, cashId, bankId, settlementAccountId, exchange); return true;
    }

    private bool PaymentCurrencyMatches(Guid? accountCurrencyId)
    {
        var currency = _selectedEmployeePayroll?.CurrencyId ?? _selectedEos?.CurrencyId; return !currency.HasValue || accountCurrencyId == currency.Value;
    }

    private void ResetPayment(decimal amount) { _paymentDate = DateOnly.FromDateTime(DateTime.Today); _paymentAmountText = Math.Max(0, amount).ToString(CultureInfo.InvariantCulture); _paymentExchangeRateText = null; _paymentReference = null; _cashAccountValue = null; _bankAccountValue = null; }

    private void LoadPolicyForm(PayrollPolicyDto x)
    {
        _policyName=x.NameAr;_policyFrom=x.EffectiveFrom;_policyTo=x.EffectiveTo;_policyProrationText=x.ProrationMethod.ToString();_policyDailyText=x.DailyRateMethod.ToString();_policyHourlyText=x.HourlyRateMethod.ToString();_policyRequireAttendance=x.RequireApprovedAttendance;_policyRequireFullPayment=x.RequireFullPaymentBeforeRunClose;_policyAbsenceComponentValue=x.AbsenceDeductionComponentId?.ToString("D");_policyLateComponentValue=x.LateDeductionComponentId?.ToString("D");_policyEarlyComponentValue=x.EarlyLeaveDeductionComponentId?.ToString("D");_policyUnpaidLeaveComponentValue=x.UnpaidLeaveComponentId?.ToString("D");_policyOvertimeComponentValue=x.OvertimeComponentId?.ToString("D");_policyNotes=x.Notes;
    }
    private void ClearPolicyForm(){_policyName="سياسة الرواتب";_policyFrom=DateOnly.FromDateTime(DateTime.Today);_policyTo=null;_policyProrationText="2";_policyDailyText="1";_policyHourlyText="1";_policyRequireAttendance=true;_policyRequireFullPayment=false;_policyAbsenceComponentValue=null;_policyLateComponentValue=null;_policyEarlyComponentValue=null;_policyUnpaidLeaveComponentValue=null;_policyOvertimeComponentValue=null;_policyNotes=null;}
    private void ClearPeriodForm(){var now=DateTime.Today;_periodYearText=now.Year.ToString(CultureInfo.InvariantCulture);_periodMonthText=now.Month.ToString(CultureInfo.InvariantCulture);_periodStart=new DateOnly(now.Year,now.Month,1);_periodEnd=new DateOnly(now.Year,now.Month,DateTime.DaysInMonth(now.Year,now.Month));}
    private void ClearRunForm(){_runPeriodValue=_periods.FirstOrDefault(x=>x.Status==1)?.Id.ToString("D");_runPolicyValue=_policies.FirstOrDefault(x=>x.Status==2)?.Id.ToString("D");_runCurrencyValue=_currencies.FirstOrDefault(x=>x.IsActive)?.Id.ToString("D");_runTypeText="1";_runCalculationDate=DateOnly.FromDateTime(DateTime.Today);_runPostingDate=DateOnly.FromDateTime(DateTime.Today);_runNotes=null;}
    private void ClearEosForm(){_eosEmployeeValue=null;_eosEmployeeItem=null;_eosFinalPayrollValue=null;_eosEmployeePayrollHistory=[];_eosLastWorkingDate=DateOnly.FromDateTime(DateTime.Today);_eosReason=null;_eosBenefitText="0";_eosLeaveDailyRateText="";_eosOtherEarningsText="0";_eosOtherDeductionsText="0";_eosSettleLeave=true;_eosSettleLoans=true;}

    private Task SetPeriodMonthAsync(string? value){_periodMonthText=value??"1";if(int.TryParse(_periodYearText,out var y)&&int.TryParse(_periodMonthText,out var m)&&m is >=1 and <=12){_periodStart=new DateOnly(y,m,1);_periodEnd=new DateOnly(y,m,DateTime.DaysInMonth(y,m));}return Task.CompletedTask;}
    private Task SetRunPeriodAsync(string? v){_runPeriodValue=v;return Task.CompletedTask;} private Task SetRunTypeAsync(string? v){_runTypeText=v??"1";return Task.CompletedTask;} private Task SetRunPolicyAsync(string? v){_runPolicyValue=v;return Task.CompletedTask;} private Task SetRunCurrencyAsync(string? v){_runCurrencyValue=v;return Task.CompletedTask;}
    private Task SetPolicyProrationAsync(string? v){_policyProrationText=v??"2";return Task.CompletedTask;} private Task SetPolicyDailyAsync(string? v){_policyDailyText=v??"1";return Task.CompletedTask;} private Task SetPolicyHourlyAsync(string? v){_policyHourlyText=v??"1";return Task.CompletedTask;}
    private Task SetPolicyAbsenceComponentAsync(string? v){_policyAbsenceComponentValue=v;return Task.CompletedTask;} private Task SetPolicyLateComponentAsync(string? v){_policyLateComponentValue=v;return Task.CompletedTask;} private Task SetPolicyEarlyComponentAsync(string? v){_policyEarlyComponentValue=v;return Task.CompletedTask;} private Task SetPolicyUnpaidComponentAsync(string? v){_policyUnpaidLeaveComponentValue=v;return Task.CompletedTask;} private Task SetPolicyOvertimeComponentAsync(string? v){_policyOvertimeComponentValue=v;return Task.CompletedTask;}
    private Task SetPaymentMethodAsync(string? v){_paymentMethodText=v??"1";_cashAccountValue=null;_bankAccountValue=null;return Task.CompletedTask;} private Task SetCashAccountAsync(string? v){_cashAccountValue=v;return Task.CompletedTask;} private Task SetBankAccountAsync(string? v){_bankAccountValue=v;return Task.CompletedTask;} private Task SetEosFinalPayrollAsync(string? v){_eosFinalPayrollValue=v;return Task.CompletedTask;}

    private bool Handle<T>(ApiCallResult<T> result){if(result.Succeeded)return true;if(result.Error is not null)ApiFeedback.Show(result.Error);else ApiFeedback.ShowUnexpected();return false;}
    private static IReadOnlyList<UiSelectOption> WithEmpty(IEnumerable<UiSelectOption> rows)=>[new UiSelectOption(string.Empty,"— بدون —"),.. rows];
    private static Guid? GuidOrNull(string? v)=>Guid.TryParse(v,out var id)&&id!=Guid.Empty?id:null;
    private static string? Null(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
    private static bool TryDecimal(string? v,out decimal result)=>decimal.TryParse(v,NumberStyles.Number,CultureInfo.InvariantCulture,out result)||decimal.TryParse(v,NumberStyles.Number,CultureInfo.CurrentCulture,out result);
    private static string RowCss(bool selected,string layout)=>selected?$"payroll-grid-row {layout} payroll-grid-row--selected":$"payroll-grid-row {layout}";
    private static string PeriodStatusName(byte v)=>v switch{1=>"مفتوحة",2=>"مقفلة",3=>"مغلقة",_=>"غير معروف"};
    private static string RunStatusName(byte v)=>v switch{1=>"مسودة",2=>"محتسب",3=>"مراجع",4=>"معتمد",5=>"مرحل",6=>"مغلق",7=>"ملغي",_=>"غير معروف"};
    private static string EmployeePayrollStatusName(byte v)=>v switch{1=>"محتسب",2=>"مراجع",3=>"معتمد",4=>"مرحل",5=>"ملغي",_=>"غير معروف"};
    private static string PolicyStatusName(byte v)=>v switch{1=>"مسودة",2=>"فعالة",3=>"مستبدلة",4=>"ملغاة",_=>"غير معروف"};
    private static string EosStatusName(byte v)=>v switch{1=>"مسودة",2=>"محتسبة",3=>"مراجعة",4=>"معتمدة",5=>"مرحلة",6=>"مدفوعة",7=>"ملغاة",_=>"غير معروف"};
    private static string RunTypeName(byte v)=>v==2?"نهائي":"دوري";
    private static string MoneyText(decimal v)=>v.ToString("N2", CultureInfo.CurrentCulture);
    private static AlertTone IssueTone(byte v)=>v switch { 3=>AlertTone.Danger, 2=>AlertTone.Warning, _=>AlertTone.Info };
    private static AlertTone StatusTone(byte v)=>v switch{2 or 3=>AlertTone.Warning,4 or 5 or 6=>AlertTone.Success,7=>AlertTone.Danger,_=>AlertTone.Info};

    private readonly record struct PaymentInput(DateOnly Date, decimal Amount, byte Method, Guid? CashId, Guid? BankId, Guid SettlementAccountId, decimal? ExchangeRate);
}
