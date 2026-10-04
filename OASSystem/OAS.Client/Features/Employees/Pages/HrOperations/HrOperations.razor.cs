using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;
using OAS.Contracts.Features.Employees.Adjustments;
using OAS.Contracts.Features.Employees.Compensation;
using OAS.Contracts.Features.Employees.Leave;
using OAS.Contracts.Features.Employees.Loans;
using OAS.Contracts.Features.Employees.Overtime;
using OAS.Contracts.Features.Employees.Settings;
using OAS.Contracts.Features.Employees.Time;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Features.Employees.Pages.HrOperations;

public partial class HrOperations
{
    private enum EditorMode { Empty, View, Create, Edit }

    [Inject] private IHrOperationsClientService Hr { get; set; } = default!;
    [Inject] private IEmployeeClientService Employees { get; set; } = default!;
    [Inject] private IEmployeeCompensationClientService Compensation { get; set; } = default!;
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    private string _section = "shifts";
    private EditorMode _mode = EditorMode.Empty;
    private bool _loading = true;
    private bool _saving;

    private IReadOnlyList<WorkShiftDto> _shifts = [];
    private IReadOnlyList<EmployeeShiftAssignmentDto> _assignments = [];
    private IReadOnlyList<HolidayDto> _holidays = [];
    private IReadOnlyList<AttendanceRecordDto> _attendance = [];
    private IReadOnlyList<LeaveTypeDto> _leaveTypes = [];
    private IReadOnlyList<LeaveRequestDto> _leaveRequests = [];
    private IReadOnlyList<OvertimeRecordDto> _overtime = [];
    private IReadOnlyList<EmployeeLoanDto> _loans = [];
    private IReadOnlyList<EmployeeAdjustmentDto> _adjustments = [];
    private IReadOnlyList<SalaryComponentDto> _salaryComponents = [];
    private IReadOnlyList<CashAccountDto> _cashAccounts = [];
    private IReadOnlyList<BankAccountDto> _bankAccounts = [];
    private HrSettingsDto? _settings;

    private WorkShiftDto? _selectedShift;
    private EmployeeShiftAssignmentDto? _selectedAssignment;
    private HolidayDto? _selectedHoliday;
    private AttendanceRecordDto? _selectedAttendance;
    private LeaveTypeDto? _selectedLeaveType;
    private LeaveRequestDto? _selectedLeaveRequest;
    private OvertimeRecordDto? _selectedOvertime;
    private EmployeeLoanDto? _selectedLoan;
    private EmployeeAdjustmentDto? _selectedAdjustment;

    private static readonly IReadOnlyList<string> ShiftHeaders = ["الكود", "الوردية", "الوقت", "الدقائق", "الحالة"];
    private static readonly IReadOnlyList<string> HolidayHeaders = ["الكود", "العطلة", "من", "إلى", "الحالة"];
    private static readonly IReadOnlyList<string> AttendanceHeaders = ["الموظف", "التاريخ", "الوردية", "دخول", "خروج", "تأخير/إضافي", "الاعتماد"];
    private static readonly IReadOnlyList<string> LeaveTypeHeaders = ["الكود", "النوع", "الأجر", "الاستحقاق", "الحالة"];
    private static readonly IReadOnlyList<string> LeaveRequestHeaders = ["الكود", "الموظف", "النوع", "الفترة", "الأيام", "الحالة"];
    private static readonly IReadOnlyList<string> OvertimeHeaders = ["الكود", "الموظف", "التاريخ", "المطلوب", "المعتمد", "الحالة"];
    private static readonly IReadOnlyList<string> LoanHeaders = ["الكود", "الموظف", "الأصل", "المتبقي", "الأقساط", "الحالة"];
    private static readonly IReadOnlyList<string> AdjustmentHeaders = ["الكود", "الموظف", "المكون", "التاريخ", "المبلغ", "الحالة"];

    private IReadOnlyList<UiSectionTabItem> SectionTabs =>
    [
        new("shifts", "الورديات", _section == "shifts"),
        new("assignments", "تعيين الورديات", _section == "assignments"),
        new("holidays", "العطل", _section == "holidays"),
        new("attendance", "الحضور", _section == "attendance"),
        new("leave-types", "أنواع الإجازات", _section == "leave-types"),
        new("leave-requests", "طلبات الإجازات", _section == "leave-requests"),
        new("overtime", "العمل الإضافي", _section == "overtime"),
        new("loans", "السلف", _section == "loans"),
        new("adjustments", "التعديلات", _section == "adjustments"),
        new("settings", "الإعدادات", _section == "settings")
    ];

    private bool IsEditing => _mode is EditorMode.Create or EditorMode.Edit || (_section == "settings" && _settingsEditing);
    private bool ShowAside => _section != "settings";
    private bool CanNew => !_saving && !IsEditing && _section != "settings";
    private bool CanEdit => !_saving && (_section == "settings" ? !_settingsEditing : !IsEditing && SelectedExists && SelectedIsEditable);
    private bool CanSave => !_saving && IsEditing;
    private bool CanCancelEdit => !_saving && IsEditing;
    private bool CanGenerateAttendance => _section == "attendance" && !_saving && !IsEditing;
    private bool CanSubmit => !_saving && !IsEditing && SelectedWorkflowStatus == 1 && _section is "attendance" or "leave-requests" or "overtime" or "loans" or "adjustments";
    private bool CanApprove => !_saving && !IsEditing && SelectedWorkflowStatus == 2 && _section is "attendance" or "leave-requests" or "overtime" or "loans" or "adjustments";
    private bool CanReject => CanApprove;
    private bool CanCancelRecord => !_saving && !IsEditing && _section is "leave-requests" or "overtime" or "loans" or "adjustments" && SelectedWorkflowStatus is 1 or 2 or 3;
    private bool CanDisburseLoan => _section == "loans" && _selectedLoan?.Status == 3 && !_saving && !IsEditing;
    private bool CanPayNextLoanInstallment => _section == "loans" && _selectedLoan?.Status == 4 && _selectedLoan.Installments.Any(x => x.Status == 2) && !_saving && !IsEditing;

    private bool SelectedExists => _section switch
    {
        "shifts" => _selectedShift is not null,
        "assignments" => _selectedAssignment is not null,
        "holidays" => _selectedHoliday is not null,
        "attendance" => _selectedAttendance is not null,
        "leave-types" => _selectedLeaveType is not null,
        "leave-requests" => _selectedLeaveRequest is not null,
        "overtime" => _selectedOvertime is not null,
        "loans" => _selectedLoan is not null,
        "adjustments" => _selectedAdjustment is not null,
        _ => false
    };

    private bool SelectedIsEditable => _section switch
    {
        "attendance" => _selectedAttendance?.ApprovalStatus is 1 or 4,
        "leave-requests" => _selectedLeaveRequest?.Status == 1,
        "overtime" => _selectedOvertime?.Status == 1,
        "loans" => _selectedLoan?.Status == 1,
        "adjustments" => _selectedAdjustment?.Status == 1,
        _ => true
    };

    private byte? SelectedWorkflowStatus => _section switch
    {
        "attendance" => _selectedAttendance?.ApprovalStatus,
        "leave-requests" => _selectedLeaveRequest?.Status,
        "overtime" => _selectedOvertime?.Status,
        "loans" => _selectedLoan?.Status,
        "adjustments" => _selectedAdjustment?.Status,
        _ => null
    };

    private string EditorTitle => _mode switch
    {
        EditorMode.Create => "سجل جديد",
        EditorMode.Edit => "تعديل السجل",
        EditorMode.View => "تفاصيل السجل",
        _ => "التفاصيل"
    };

    protected override async Task OnInitializedAsync()
    {
        _attendanceFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        _attendanceTo = DateOnly.FromDateTime(DateTime.Today);
        await LoadReferenceDataAsync();
        await LoadCurrentSectionAsync();
    }

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            var shiftsTask = Hr.GetShiftsAsync();
            var leaveTypesTask = Hr.GetLeaveTypesAsync();
            var salaryTask = Compensation.GetComponentsAsync(activeOnly: true);
            var cashTask = Accounting.GetCashAccountsPageAsync(new PageRequest { PageNumber = 1, PageSize = 200, SortBy = "Code" });
            var bankTask = Accounting.GetBankAccountsPageAsync(new PageRequest { PageNumber = 1, PageSize = 200, SortBy = "Code" });
            await Task.WhenAll(shiftsTask, leaveTypesTask, salaryTask, cashTask, bankTask);
            _shifts = await shiftsTask;
            _leaveTypes = await leaveTypesTask;
            _salaryComponents = await salaryTask;
            _cashAccounts = (await cashTask).Items.Where(x => x.IsActive).ToArray();
            _bankAccounts = (await bankTask).Items.Where(x => x.IsActive).ToArray();
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
    }

    private async Task LoadCurrentSectionAsync()
    {
        _loading = true;
        try
        {
            switch (_section)
            {
                case "shifts": _shifts = await Hr.GetShiftsAsync(); break;
                case "assignments": if (_assignmentEmployeeId is Guid employeeId) _assignments = await Hr.GetShiftAssignmentsAsync(employeeId); else _assignments = []; break;
                case "holidays": _holidays = await Hr.GetHolidaysAsync(); break;
                case "attendance": _attendance = await Hr.GetAttendanceAsync(_attendanceFrom, _attendanceTo); break;
                case "leave-types": _leaveTypes = await Hr.GetLeaveTypesAsync(); break;
                case "leave-requests": _leaveRequests = await Hr.GetLeaveRequestsAsync(); break;
                case "overtime": _overtime = await Hr.GetOvertimeAsync(); break;
                case "loans": _loans = await Hr.GetLoansAsync(); break;
                case "adjustments": _adjustments = await Hr.GetAdjustmentsAsync(); break;
                case "settings": await LoadSettingsAsync(); break;
            }
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _loading = false; }
    }

    private async Task ChangeSectionAsync(string key)
    {
        if (_section == key) return;
        if (IsEditing) { Snackbar.Info("احفظ أو ألغِ التعديلات الحالية قبل الانتقال إلى قسم آخر."); return; }
        _section = key;
        ClearSelection();
        await LoadCurrentSectionAsync();
    }

    private async Task RefreshAsync(MouseEventArgs _)
    {
        if (IsEditing) return;
        await LoadReferenceDataAsync();
        await LoadCurrentSectionAsync();
    }

    private async Task ApplyAttendanceFilterAsync(MouseEventArgs _) => await LoadCurrentSectionAsync();

    private void ClearSelection()
    {
        _mode = EditorMode.Empty;
        _selectedShift = null; _selectedAssignment = null; _selectedHoliday = null; _selectedAttendance = null;
        _selectedLeaveType = null; _selectedLeaveRequest = null; _selectedOvertime = null; _selectedLoan = null; _selectedAdjustment = null;
    }

    private void SelectShift(WorkShiftDto item) { if (IsEditing) return; _selectedShift = item; _mode = EditorMode.View; LoadShiftForm(item); }
    private void SelectAssignment(EmployeeShiftAssignmentDto item) { if (IsEditing) return; _selectedAssignment = item; _mode = EditorMode.View; LoadAssignmentForm(item); }
    private void SelectHoliday(HolidayDto item) { if (IsEditing) return; _selectedHoliday = item; _mode = EditorMode.View; LoadHolidayForm(item); }
    private void SelectAttendance(AttendanceRecordDto item) { if (IsEditing) return; _selectedAttendance = item; _mode = EditorMode.View; LoadAttendanceForm(item); }
    private void SelectLeaveType(LeaveTypeDto item) { if (IsEditing) return; _selectedLeaveType = item; _mode = EditorMode.View; LoadLeaveTypeForm(item); }
    private void SelectLeaveRequest(LeaveRequestDto item) { if (IsEditing) return; _selectedLeaveRequest = item; _mode = EditorMode.View; LoadLeaveRequestForm(item); }
    private void SelectOvertime(OvertimeRecordDto item) { if (IsEditing) return; _selectedOvertime = item; _mode = EditorMode.View; LoadOvertimeForm(item); }
    private async Task SelectLoanAsync(EmployeeLoanDto item) { if (IsEditing) return; _selectedLoan = await Hr.GetLoanAsync(item.Id) ?? item; _mode = EditorMode.View; LoadLoanForm(_selectedLoan); }
    private void SelectAdjustment(EmployeeAdjustmentDto item) { if (IsEditing) return; _selectedAdjustment = item; _mode = EditorMode.View; LoadAdjustmentForm(item); }

    private static string RowCss(bool selected) => selected ? "hr-grid-row hr-grid-row--selected" : "hr-grid-row";
    private static string Time(DateTimeOffset? value) => value?.ToString("HH:mm", CultureInfo.CurrentCulture) ?? "—";
    private static string Money(decimal value, string currency) => $"{value:N2} {currency}";
    private static string WorkflowName(byte status) => status switch { 1 => "مسودة", 2 => "بانتظار الاعتماد", 3 => "معتمد", 4 => "مرفوض", 5 => "مطبق", 6 => "ملغي", _ => status.ToString(CultureInfo.InvariantCulture) };
    private static AlertTone WorkflowTone(byte status) => status switch { 2 => AlertTone.Warning, 3 or 5 => AlertTone.Success, 4 or 6 => AlertTone.Danger, _ => AlertTone.Info };
    private static string AttendanceApprovalName(byte status) => status switch { 1 => "مسودة", 2 => "بانتظار الاعتماد", 3 => "معتمد", 4 => "مرفوض", _ => "غير معروف" };
    private static AlertTone ApprovalTone(byte status) => status switch { 2 => AlertTone.Warning, 3 => AlertTone.Success, 4 => AlertTone.Danger, _ => AlertTone.Info };
    private static string LoanStatusName(byte status) => status switch { 1 => "مسودة", 2 => "بانتظار الاعتماد", 3 => "معتمدة", 4 => "نشطة", 5 => "مكتملة", 6 => "مرفوضة", 7 => "ملغاة", _ => "غير معروف" };
    private static AlertTone LoanTone(byte status) => status switch { 2 => AlertTone.Warning, 3 or 4 or 5 => AlertTone.Success, 6 or 7 => AlertTone.Danger, _ => AlertTone.Info };
    private static string InstallmentStatusName(byte status) => status switch { 1 => "معلق", 2 => "مجدول", 3 => "مخصوم", 4 => "مدفوع خارجيًا", 5 => "ملغي", _ => "غير معروف" };

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool TryInt(string? value, out int result) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    private static bool TryByte(string? value, out byte result) => byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    private static bool TryDecimal(string? value, out decimal result) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result) || decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result);
    private static bool TryTime(string? value, out TimeOnly result) => TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out result) || TimeOnly.TryParse(value, out result);
    private static bool TryUtc(string? value, out DateTimeOffset? result)
    {
        if (string.IsNullOrWhiteSpace(value)) { result = null; return true; }
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto)) { result = dto; return true; }
        result = null; return false;
    }

    private async Task<IReadOnlyList<UiLookupItem>> SearchEmployeesAsync(
        string search,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = await Employees.GetPageAsync(
                new PageRequest
                {
                    PageNumber = 1,
                    PageSize = 50,
                    Search = search,
                    SortBy = "EmployeeCode"
                },
                cancellationToken);

            return page.Items
                .Where(x => x.IsActive)
                .Select(x => new UiLookupItem(
                    x.Id.ToString("D"),
                    x.DisplayName,
                    x.EmployeeCode,
                    "fa-solid fa-user-tie"))
                .ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return [];
        }
        catch
        {
            return [];
        }
    }

    private static UiLookupItem? Lookup(Guid? id, string? name, string? code) => id.HasValue && !string.IsNullOrWhiteSpace(name) ? new UiLookupItem(id.Value.ToString("D"), name!, code, "fa-solid fa-user-tie") : null;

    private IReadOnlyList<UiSelectOption> ShiftOptions => _shifts.Where(x => x.IsActive).OrderBy(x => x.NameAr).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.ShiftCode} - {x.NameAr}")).ToArray();
    private IReadOnlyList<UiSelectOption> ActiveLeaveTypeOptions => _leaveTypes.Where(x => x.IsActive).OrderBy(x => x.NameAr).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.LeaveTypeCode} - {x.NameAr}")).ToArray();
    private IReadOnlyList<UiSelectOption> SalaryComponentOptions => _salaryComponents.Where(x => x.IsActive && x.ComponentType != 3).OrderBy(x => x.DisplayOrder).Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.ComponentCode} - {x.NameAr}")).ToArray();
    private IReadOnlyList<UiSelectOption> CashAccountOptions => _cashAccounts.Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} - {x.Name}")).ToArray();
    private IReadOnlyList<UiSelectOption> BankAccountOptions => _bankAccounts.Select(x => new UiSelectOption(x.Id.ToString("D"), $"{x.Code} - {x.BankName} / {x.AccountName}")).ToArray();
    private static readonly IReadOnlyList<UiSelectOption> LeaveAccrualOptions = [new("1", "منحة سنوية"), new("2", "استحقاق شهري"), new("3", "يدوي")];
    private static readonly IReadOnlyList<UiSelectOption> LeaveCountingOptions = [new("1", "أيام العمل"), new("2", "أيام تقويمية")];
    private static readonly IReadOnlyList<UiSelectOption> LoanRepaymentOptions = [new("1", "من الراتب"), new("2", "سداد خارجي"), new("3", "مختلط")];
    private static readonly IReadOnlyList<UiSelectOption> AdjustmentTypeOptions = [new("1", "استحقاق"), new("2", "خصم")];
    private static readonly IReadOnlyList<UiSelectOption> PaymentMethodOptions = [new(((byte)PaymentMethod.Cash).ToString(), "نقدًا"), new(((byte)PaymentMethod.BankTransfer).ToString(), "تحويل بنكي")];
}
