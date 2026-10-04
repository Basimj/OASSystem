using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Adjustments;
using OAS.Contracts.Features.Employees.Leave;
using OAS.Contracts.Features.Employees.Loans;
using OAS.Contracts.Features.Employees.Overtime;
using OAS.Contracts.Features.Employees.Settings;
using OAS.Contracts.Features.Employees.Time;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Features.Employees.Pages.HrOperations;

public partial class HrOperations
{
    private string? _shiftNameAr, _shiftNameEn, _shiftStartText, _shiftEndText, _shiftBreakText, _shiftGraceLateText, _shiftGraceEarlyText, _shiftDaysMaskText, _shiftNotes;
    private bool _shiftFlexible, _shiftActive = true;

    private Guid? _assignmentEmployeeId, _assignmentShiftId;
    private string? _assignmentEmployeeName, _assignmentEmployeeCode;
    private DateOnly? _assignmentFrom, _assignmentTo;
    private bool _assignmentActive = true;

    private string? _holidayNameAr, _holidayNameEn, _holidayNotes;
    private DateOnly? _holidayFrom, _holidayTo;
    private bool _holidayPaid = true, _holidayActive = true;

    private DateOnly? _attendanceFrom, _attendanceTo, _attendanceDate;
    private Guid? _attendanceEmployeeId;
    private string? _attendanceEmployeeName, _attendanceEmployeeCode, _attendanceCheckInText, _attendanceCheckOutText, _attendanceNotes;

    private string? _leaveTypeNameAr, _leaveTypeNameEn, _leaveEntitlementText, _leaveCarryForwardText;
    private string _leaveAccrualText = "1", _leaveCountingText = "1";
    private bool _leavePaid = true, _leaveRequiresBalance = true, _leaveProrate, _leaveTypeActive = true, _leaveEncashableOnTermination;

    private Guid? _leaveEmployeeId, _leaveRequestTypeId;
    private string? _leaveEmployeeName, _leaveEmployeeCode, _leaveRequestReason;
    private DateOnly? _leaveRequestFrom, _leaveRequestTo;

    private Guid? _overtimeEmployeeId;
    private string? _overtimeEmployeeName, _overtimeEmployeeCode, _overtimeMinutesText, _overtimeMultiplierText = "1.5", _overtimeReason;
    private DateOnly? _overtimeDate;

    private Guid? _loanEmployeeId;
    private string? _loanEmployeeName, _loanEmployeeCode, _loanPrincipalText, _loanCountText, _loanReason;
    private DateOnly? _loanDate, _loanFirstInstallment;
    private string _loanRepaymentModeText = "1", _loanPaymentMethodText = "1";
    private string? _loanCashAccountValue, _loanBankAccountValue;

    private Guid? _adjustmentEmployeeId, _adjustmentComponentId;
    private string? _adjustmentEmployeeName, _adjustmentEmployeeCode, _adjustmentAmountText, _adjustmentReason;
    private string _adjustmentTypeText = "1";
    private DateOnly? _adjustmentDate;

    private bool _settingsEditing;
    private string? _settingsTimeZoneId, _settingsLeaveYearMonthText;
    private bool _settingsRequireAttendanceApproval = true;

    private string? AssignmentEmployeeValue => _assignmentEmployeeId?.ToString("D");
    private UiLookupItem? AssignmentEmployeeItem => Lookup(_assignmentEmployeeId, _assignmentEmployeeName, _assignmentEmployeeCode);
    private string? AssignmentShiftValue => _assignmentShiftId?.ToString("D");
    private string? AttendanceEmployeeValue => _attendanceEmployeeId?.ToString("D");
    private UiLookupItem? AttendanceEmployeeItem => Lookup(_attendanceEmployeeId, _attendanceEmployeeName, _attendanceEmployeeCode);
    private string? LeaveEmployeeValue => _leaveEmployeeId?.ToString("D");
    private UiLookupItem? LeaveEmployeeItem => Lookup(_leaveEmployeeId, _leaveEmployeeName, _leaveEmployeeCode);
    private string? LeaveTypeValue => _leaveRequestTypeId?.ToString("D");
    private string? OvertimeEmployeeValue => _overtimeEmployeeId?.ToString("D");
    private UiLookupItem? OvertimeEmployeeItem => Lookup(_overtimeEmployeeId, _overtimeEmployeeName, _overtimeEmployeeCode);
    private string? LoanEmployeeValue => _loanEmployeeId?.ToString("D");
    private UiLookupItem? LoanEmployeeItem => Lookup(_loanEmployeeId, _loanEmployeeName, _loanEmployeeCode);
    private string? AdjustmentEmployeeValue => _adjustmentEmployeeId?.ToString("D");
    private UiLookupItem? AdjustmentEmployeeItem => Lookup(_adjustmentEmployeeId, _adjustmentEmployeeName, _adjustmentEmployeeCode);
    private string? AdjustmentComponentValue => _adjustmentComponentId?.ToString("D");

    private async Task NewAsync(MouseEventArgs _)
    {
        if (!CanNew) return;
        _mode = EditorMode.Create;
        ClearFormForSection();
        await InvokeAsync(StateHasChanged);
    }

    private async Task EditAsync(MouseEventArgs _)
    {
        if (!CanEdit) return;
        if (_section == "settings") _settingsEditing = true;
        else _mode = EditorMode.Edit;
        await InvokeAsync(StateHasChanged);
    }

    private async Task CancelEditAsync(MouseEventArgs _)
    {
        if (_section == "settings")
        {
            _settingsEditing = false;
            LoadSettingsForm();
            return;
        }

        if (_mode == EditorMode.Create)
        {
            _mode = EditorMode.Empty;
            ClearFormForSection();
        }
        else
        {
            _mode = SelectedExists ? EditorMode.View : EditorMode.Empty;
            ReloadSelectedForm();
        }
        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveAsync(MouseEventArgs _)
    {
        if (!CanSave) return;
        _saving = true;
        try
        {
            var ok = _section switch
            {
                "shifts" => await SaveShiftAsync(),
                "assignments" => await SaveAssignmentAsync(),
                "holidays" => await SaveHolidayAsync(),
                "attendance" => await SaveAttendanceAsync(),
                "leave-types" => await SaveLeaveTypeAsync(),
                "leave-requests" => await SaveLeaveRequestAsync(),
                "overtime" => await SaveOvertimeAsync(),
                "loans" => await SaveLoanAsync(),
                "adjustments" => await SaveAdjustmentAsync(),
                "settings" => await SaveSettingsAsync(),
                _ => false
            };
            if (!ok) return;
            _mode = EditorMode.Empty;
            await LoadReferenceDataAsync();
            await LoadCurrentSectionAsync();
            Snackbar.Success("تم حفظ البيانات بنجاح.");
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
        finally { _saving = false; }
    }

    private void ClearFormForSection()
    {
        switch (_section)
        {
            case "shifts": _selectedShift = null; _shiftNameAr = _shiftNameEn = _shiftNotes = null; _shiftStartText = "08:00"; _shiftEndText = "16:00"; _shiftBreakText = "0"; _shiftGraceLateText = "0"; _shiftGraceEarlyText = "0"; _shiftDaysMaskText = "79"; _shiftFlexible = false; _shiftActive = true; break;
            case "assignments": _selectedAssignment = null; _assignmentShiftId = null; _assignmentFrom = DateOnly.FromDateTime(DateTime.Today); _assignmentTo = null; _assignmentActive = true; break;
            case "holidays": _selectedHoliday = null; _holidayNameAr = _holidayNameEn = _holidayNotes = null; _holidayFrom = _holidayTo = DateOnly.FromDateTime(DateTime.Today); _holidayPaid = _holidayActive = true; break;
            case "attendance": _selectedAttendance = null; _attendanceEmployeeId = null; _attendanceEmployeeName = _attendanceEmployeeCode = null; _attendanceDate = DateOnly.FromDateTime(DateTime.Today); _attendanceCheckInText = _attendanceCheckOutText = _attendanceNotes = null; break;
            case "leave-types": _selectedLeaveType = null; _leaveTypeNameAr = _leaveTypeNameEn = null; _leaveAccrualText = "1"; _leaveCountingText = "1"; _leaveEntitlementText = "0"; _leaveCarryForwardText = "0"; _leavePaid = _leaveRequiresBalance = _leaveTypeActive = true; _leaveProrate = false; _leaveEncashableOnTermination=false; break;
            case "leave-requests": _selectedLeaveRequest = null; _leaveEmployeeId = _leaveRequestTypeId = null; _leaveEmployeeName = _leaveEmployeeCode = _leaveRequestReason = null; _leaveRequestFrom = _leaveRequestTo = DateOnly.FromDateTime(DateTime.Today); break;
            case "overtime": _selectedOvertime = null; _overtimeEmployeeId = null; _overtimeEmployeeName = _overtimeEmployeeCode = _overtimeReason = null; _overtimeDate = DateOnly.FromDateTime(DateTime.Today); _overtimeMinutesText = "60"; _overtimeMultiplierText = "1.5"; break;
            case "loans": _selectedLoan = null; _loanEmployeeId = null; _loanEmployeeName = _loanEmployeeCode = _loanReason = null; _loanDate = DateOnly.FromDateTime(DateTime.Today); _loanPrincipalText = null; _loanCountText = "1"; _loanFirstInstallment = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)); _loanRepaymentModeText = "1"; break;
            case "adjustments": _selectedAdjustment = null; _adjustmentEmployeeId = _adjustmentComponentId = null; _adjustmentEmployeeName = _adjustmentEmployeeCode = _adjustmentReason = null; _adjustmentDate = DateOnly.FromDateTime(DateTime.Today); _adjustmentAmountText = null; _adjustmentTypeText = "1"; break;
        }
    }

    private void ReloadSelectedForm()
    {
        switch (_section)
        {
            case "shifts" when _selectedShift is not null: LoadShiftForm(_selectedShift); break;
            case "assignments" when _selectedAssignment is not null: LoadAssignmentForm(_selectedAssignment); break;
            case "holidays" when _selectedHoliday is not null: LoadHolidayForm(_selectedHoliday); break;
            case "attendance" when _selectedAttendance is not null: LoadAttendanceForm(_selectedAttendance); break;
            case "leave-types" when _selectedLeaveType is not null: LoadLeaveTypeForm(_selectedLeaveType); break;
            case "leave-requests" when _selectedLeaveRequest is not null: LoadLeaveRequestForm(_selectedLeaveRequest); break;
            case "overtime" when _selectedOvertime is not null: LoadOvertimeForm(_selectedOvertime); break;
            case "loans" when _selectedLoan is not null: LoadLoanForm(_selectedLoan); break;
            case "adjustments" when _selectedAdjustment is not null: LoadAdjustmentForm(_selectedAdjustment); break;
        }
    }

    private void LoadShiftForm(WorkShiftDto x) { _shiftNameAr=x.NameAr; _shiftNameEn=x.NameEn; _shiftStartText=x.StartTime.ToString("HH:mm"); _shiftEndText=x.EndTime.ToString("HH:mm"); _shiftBreakText=x.BreakMinutes.ToString(); _shiftGraceLateText=x.GraceLateMinutes.ToString(); _shiftGraceEarlyText=x.GraceEarlyLeaveMinutes.ToString(); _shiftDaysMaskText=x.WorkingDaysMask.ToString(); _shiftFlexible=x.IsFlexible; _shiftActive=x.IsActive; _shiftNotes=x.Notes; }
    private void LoadAssignmentForm(EmployeeShiftAssignmentDto x) { _assignmentEmployeeId=x.EmployeeId; _assignmentShiftId=x.WorkShiftId; _assignmentFrom=x.EffectiveFrom; _assignmentTo=x.EffectiveTo; _assignmentActive=x.IsActive; }
    private void LoadHolidayForm(HolidayDto x) { _holidayNameAr=x.NameAr; _holidayNameEn=x.NameEn; _holidayFrom=x.StartDate; _holidayTo=x.EndDate; _holidayPaid=x.IsPaid; _holidayActive=x.IsActive; _holidayNotes=x.Notes; }
    private void LoadAttendanceForm(AttendanceRecordDto x) { _attendanceEmployeeId=x.EmployeeId; _attendanceEmployeeName=x.EmployeeName; _attendanceEmployeeCode=x.EmployeeCode; _attendanceDate=x.AttendanceDate; _attendanceCheckInText=x.CheckInAtUtc?.ToString("yyyy-MM-dd HH:mm"); _attendanceCheckOutText=x.CheckOutAtUtc?.ToString("yyyy-MM-dd HH:mm"); _attendanceNotes=x.Notes; }
    private void LoadLeaveTypeForm(LeaveTypeDto x) { _leaveTypeNameAr=x.NameAr; _leaveTypeNameEn=x.NameEn; _leavePaid=x.IsPaid; _leaveRequiresBalance=x.RequiresBalance; _leaveAccrualText=x.AccrualMethod.ToString(); _leaveCountingText=x.DayCountingMethod.ToString(); _leaveEntitlementText=x.AnnualEntitlementDays.ToString(CultureInfo.InvariantCulture); _leaveCarryForwardText=x.MaximumCarryForwardDays.ToString(CultureInfo.InvariantCulture); _leaveProrate=x.ProrateOnHire; _leaveTypeActive=x.IsActive; _leaveEncashableOnTermination=x.IsEncashableOnTermination; }
    private void LoadLeaveRequestForm(LeaveRequestDto x) { _leaveEmployeeId=x.EmployeeId; _leaveEmployeeName=x.EmployeeName; _leaveEmployeeCode=x.EmployeeCode; _leaveRequestTypeId=x.LeaveTypeId; _leaveRequestFrom=x.StartDate; _leaveRequestTo=x.EndDate; _leaveRequestReason=x.Reason; }
    private void LoadOvertimeForm(OvertimeRecordDto x) { _overtimeEmployeeId=x.EmployeeId; _overtimeEmployeeName=x.EmployeeName; _overtimeEmployeeCode=x.EmployeeCode; _overtimeDate=x.WorkDate; _overtimeMinutesText=x.RequestedMinutes.ToString(); _overtimeMultiplierText=x.RateMultiplier.ToString(CultureInfo.InvariantCulture); _overtimeReason=x.Reason; }
    private void LoadLoanForm(EmployeeLoanDto x) { _loanEmployeeId=x.EmployeeId; _loanEmployeeName=x.EmployeeName; _loanEmployeeCode=x.EmployeeCode; _loanDate=x.LoanDate; _loanPrincipalText=x.PrincipalAmount.ToString(CultureInfo.InvariantCulture); _loanCountText=x.InstallmentCount.ToString(); _loanFirstInstallment=x.FirstInstallmentDate; _loanRepaymentModeText=x.RepaymentMode.ToString(); _loanReason=x.Reason; }
    private void LoadAdjustmentForm(EmployeeAdjustmentDto x) { _adjustmentEmployeeId=x.EmployeeId; _adjustmentEmployeeName=x.EmployeeName; _adjustmentEmployeeCode=x.EmployeeCode; _adjustmentComponentId=x.SalaryComponentId; _adjustmentDate=x.EffectiveDate; _adjustmentTypeText=x.AdjustmentType.ToString(); _adjustmentAmountText=x.Amount.ToString(CultureInfo.InvariantCulture); _adjustmentReason=x.Reason; }

    private async Task<bool> SaveShiftAsync()
    {
        if (string.IsNullOrWhiteSpace(_shiftNameAr) || !TryTime(_shiftStartText,out var start) || !TryTime(_shiftEndText,out var end) || !TryInt(_shiftBreakText,out var breakMin) || !TryInt(_shiftGraceLateText,out var late) || !TryInt(_shiftGraceEarlyText,out var early) || !TryByte(_shiftDaysMaskText,out var mask)) { Snackbar.Error("تحقق من بيانات الوردية."); return false; }
        ApiCallResult<Guid> result = _mode == EditorMode.Create
            ? await Hr.CreateShiftAsync(new CreateWorkShiftRequest(_shiftNameAr.Trim(),NullIfEmpty(_shiftNameEn),start,end,breakMin,late,early,mask,_shiftFlexible,_shiftActive,NullIfEmpty(_shiftNotes)))
            : await Hr.UpdateShiftAsync(_selectedShift!.Id,new UpdateWorkShiftRequest(_shiftNameAr.Trim(),NullIfEmpty(_shiftNameEn),start,end,breakMin,late,early,mask,_shiftFlexible,_shiftActive,NullIfEmpty(_shiftNotes),_selectedShift.RowVersion));
        return Handle(result);
    }

    private async Task<bool> SaveAssignmentAsync()
    {
        if (_assignmentEmployeeId is not Guid employeeId || _assignmentShiftId is not Guid shiftId || _assignmentFrom is not DateOnly from) { Snackbar.Error("الموظف والوردية وتاريخ البداية مطلوبة."); return false; }
        ApiCallResult<Guid> result = _mode == EditorMode.Create
            ? await Hr.CreateShiftAssignmentAsync(employeeId,new CreateEmployeeShiftAssignmentRequest(shiftId,from,_assignmentTo,_assignmentActive))
            : await Hr.UpdateShiftAssignmentAsync(_selectedAssignment!.Id,new UpdateEmployeeShiftAssignmentRequest(shiftId,from,_assignmentTo,_assignmentActive,_selectedAssignment.RowVersion));
        return Handle(result);
    }

    private async Task<bool> SaveHolidayAsync()
    {
        if (string.IsNullOrWhiteSpace(_holidayNameAr) || _holidayFrom is not DateOnly from || _holidayTo is not DateOnly to) { Snackbar.Error("اسم العطلة وفترتها مطلوبة."); return false; }
        ApiCallResult<Guid> result = _mode == EditorMode.Create
            ? await Hr.CreateHolidayAsync(new CreateHolidayRequest(_holidayNameAr.Trim(),NullIfEmpty(_holidayNameEn),from,to,_holidayPaid,_holidayActive,NullIfEmpty(_holidayNotes)))
            : await Hr.UpdateHolidayAsync(_selectedHoliday!.Id,new UpdateHolidayRequest(_holidayNameAr.Trim(),NullIfEmpty(_holidayNameEn),from,to,_holidayPaid,_holidayActive,NullIfEmpty(_holidayNotes),_selectedHoliday.RowVersion));
        return Handle(result);
    }

    private async Task<bool> SaveAttendanceAsync()
    {
        if (_attendanceEmployeeId is not Guid employeeId || _attendanceDate is not DateOnly date || !TryUtc(_attendanceCheckInText,out var checkIn) || !TryUtc(_attendanceCheckOutText,out var checkOut)) { Snackbar.Error("تحقق من الموظف والتاريخ وأوقات الحضور."); return false; }
        ApiCallResult<Guid> result = _mode == EditorMode.Create
            ? await Hr.CreateAttendanceAsync(new CreateAttendanceRequest(employeeId,date,checkIn,checkOut,1,NullIfEmpty(_attendanceNotes)))
            : await Hr.UpdateAttendanceAsync(_selectedAttendance!.Id,new UpdateAttendanceRequest(checkIn,checkOut,1,NullIfEmpty(_attendanceNotes),_selectedAttendance.ApprovalStatus==3?"تصحيح حضور معتمد":"تعديل يدوي",_selectedAttendance.RowVersion));
        return Handle(result);
    }

    private async Task<bool> SaveLeaveTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(_leaveTypeNameAr) || !TryByte(_leaveAccrualText,out var accrual) || !TryByte(_leaveCountingText,out var counting) || !TryDecimal(_leaveEntitlementText,out var entitlement) || !TryDecimal(_leaveCarryForwardText,out var carry)) { Snackbar.Error("تحقق من بيانات نوع الإجازة."); return false; }
        ApiCallResult<Guid> result = _mode == EditorMode.Create
            ? await Hr.CreateLeaveTypeAsync(new CreateLeaveTypeRequest(_leaveTypeNameAr.Trim(),NullIfEmpty(_leaveTypeNameEn),_leavePaid,_leaveRequiresBalance,accrual,counting,entitlement,carry,_leaveProrate,_leaveTypeActive,_leaveEncashableOnTermination))
            : await Hr.UpdateLeaveTypeAsync(_selectedLeaveType!.Id,new UpdateLeaveTypeRequest(_leaveTypeNameAr.Trim(),NullIfEmpty(_leaveTypeNameEn),_leavePaid,_leaveRequiresBalance,accrual,counting,entitlement,carry,_leaveProrate,_leaveTypeActive,_selectedLeaveType.RowVersion,_leaveEncashableOnTermination));
        return Handle(result);
    }

    private async Task<bool> SaveLeaveRequestAsync()
    {
        if (_leaveEmployeeId is not Guid employeeId || _leaveRequestTypeId is not Guid typeId || _leaveRequestFrom is not DateOnly from || _leaveRequestTo is not DateOnly to) { Snackbar.Error("الموظف ونوع الإجازة والفترة مطلوبة."); return false; }
        ApiCallResult<Guid> result = _mode == EditorMode.Create
            ? await Hr.CreateLeaveRequestAsync(new CreateLeaveRequestRequest(employeeId,typeId,from,to,NullIfEmpty(_leaveRequestReason)))
            : await Hr.UpdateLeaveRequestAsync(_selectedLeaveRequest!.Id,new UpdateLeaveRequestRequest(from,to,NullIfEmpty(_leaveRequestReason),_selectedLeaveRequest.RowVersion));
        return Handle(result);
    }

    private async Task<bool> SaveOvertimeAsync()
    {
        if (_overtimeEmployeeId is not Guid employeeId || _overtimeDate is not DateOnly date || !TryInt(_overtimeMinutesText,out var minutes) || !TryDecimal(_overtimeMultiplierText,out var multiplier)) { Snackbar.Error("تحقق من بيانات العمل الإضافي."); return false; }
        ApiCallResult<Guid> result = _mode == EditorMode.Create
            ? await Hr.CreateOvertimeAsync(new CreateOvertimeRequest(employeeId,null,date,minutes,multiplier,NullIfEmpty(_overtimeReason)))
            : await Hr.UpdateOvertimeAsync(_selectedOvertime!.Id,new UpdateOvertimeRequest(minutes,multiplier,NullIfEmpty(_overtimeReason),_selectedOvertime.RowVersion));
        return Handle(result);
    }

    private async Task<bool> SaveLoanAsync()
    {
        if (_loanEmployeeId is not Guid employeeId || _loanDate is not DateOnly loanDate || _loanFirstInstallment is not DateOnly first || !TryDecimal(_loanPrincipalText,out var principal) || !TryInt(_loanCountText,out var count) || !TryByte(_loanRepaymentModeText,out var mode)) { Snackbar.Error("تحقق من بيانات السلفة."); return false; }
        ApiCallResult<Guid> result = _mode == EditorMode.Create
            ? await Hr.CreateLoanAsync(new CreateEmployeeLoanRequest(employeeId,loanDate,principal,count,first,mode,NullIfEmpty(_loanReason)))
            : await Hr.UpdateLoanAsync(_selectedLoan!.Id,new UpdateEmployeeLoanRequest(principal,count,first,mode,NullIfEmpty(_loanReason),_selectedLoan.RowVersion));
        return Handle(result);
    }

    private async Task<bool> SaveAdjustmentAsync()
    {
        if (_adjustmentEmployeeId is not Guid employeeId || _adjustmentComponentId is not Guid componentId || _adjustmentDate is not DateOnly date || !TryByte(_adjustmentTypeText,out var type) || !TryDecimal(_adjustmentAmountText,out var amount) || string.IsNullOrWhiteSpace(_adjustmentReason)) { Snackbar.Error("تحقق من بيانات التعديل."); return false; }
        ApiCallResult<Guid> result = _mode == EditorMode.Create
            ? await Hr.CreateAdjustmentAsync(new CreateEmployeeAdjustmentRequest(employeeId,componentId,date,type,amount,_adjustmentReason.Trim()))
            : await Hr.UpdateAdjustmentAsync(_selectedAdjustment!.Id,new UpdateEmployeeAdjustmentRequest(type,amount,_adjustmentReason.Trim(),_selectedAdjustment.RowVersion));
        return Handle(result);
    }

    private async Task LoadSettingsAsync() { _settings = await Hr.GetSettingsAsync(); LoadSettingsForm(); }
    private void LoadSettingsForm() { _settingsTimeZoneId=_settings?.TimeZoneId; _settingsLeaveYearMonthText=(_settings?.LeaveYearStartMonth ?? 1).ToString(); _settingsRequireAttendanceApproval=_settings?.RequireAttendanceApproval ?? true; }
    private async Task<bool> SaveSettingsAsync()
    {
        if (!TryByte(_settingsLeaveYearMonthText,out var month)) { Snackbar.Error("شهر بداية سنة الإجازة غير صالح."); return false; }
        var result=await Hr.UpdateSettingsAsync(new UpdateHrSettingsRequest(NullIfEmpty(_settingsTimeZoneId),month,_settingsRequireAttendanceApproval,_settings?.RowVersion));
        if (!result.Succeeded) { if(result.Error is not null)ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected(); return false; }
        _settings=result.Value; _settingsEditing=false; return true;
    }

    private bool Handle<T>(ApiCallResult<T> result)
    {
        if (result.Succeeded) return true;
        if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected();
        return false;
    }

    private async Task SetAssignmentEmployeeAsync(string? value) { _assignmentEmployeeId=Guid.TryParse(value,out var id)?id:null; if(_assignmentEmployeeId.HasValue){var e=await Employees.GetByIdAsync(_assignmentEmployeeId.Value);_assignmentEmployeeName=e.DisplayName;_assignmentEmployeeCode=e.EmployeeCode;_assignments=await Hr.GetShiftAssignmentsAsync(e.Id);}else{_assignmentEmployeeName=_assignmentEmployeeCode=null;_assignments=[];} _selectedAssignment=null; _mode=EditorMode.Empty; }
    private Task SetAssignmentShiftAsync(string? value){_assignmentShiftId=Guid.TryParse(value,out var id)?id:null;return Task.CompletedTask;}
    private async Task SetAttendanceEmployeeAsync(string? value){_attendanceEmployeeId=Guid.TryParse(value,out var id)?id:null;if(_attendanceEmployeeId.HasValue){var e=await Employees.GetByIdAsync(_attendanceEmployeeId.Value);_attendanceEmployeeName=e.DisplayName;_attendanceEmployeeCode=e.EmployeeCode;}return;}
    private Task SetLeaveAccrualAsync(string? v){_leaveAccrualText=v??"1";return Task.CompletedTask;}
    private Task SetLeaveCountingAsync(string? v){_leaveCountingText=v??"1";return Task.CompletedTask;}
    private async Task SetLeaveEmployeeAsync(string? value){_leaveEmployeeId=Guid.TryParse(value,out var id)?id:null;if(_leaveEmployeeId.HasValue){var e=await Employees.GetByIdAsync(_leaveEmployeeId.Value);_leaveEmployeeName=e.DisplayName;_leaveEmployeeCode=e.EmployeeCode;}return;}
    private Task SetLeaveRequestTypeAsync(string? v){_leaveRequestTypeId=Guid.TryParse(v,out var id)?id:null;return Task.CompletedTask;}
    private async Task SetOvertimeEmployeeAsync(string? value){_overtimeEmployeeId=Guid.TryParse(value,out var id)?id:null;if(_overtimeEmployeeId.HasValue){var e=await Employees.GetByIdAsync(_overtimeEmployeeId.Value);_overtimeEmployeeName=e.DisplayName;_overtimeEmployeeCode=e.EmployeeCode;}return;}
    private async Task SetLoanEmployeeAsync(string? value){_loanEmployeeId=Guid.TryParse(value,out var id)?id:null;if(_loanEmployeeId.HasValue){var e=await Employees.GetByIdAsync(_loanEmployeeId.Value);_loanEmployeeName=e.DisplayName;_loanEmployeeCode=e.EmployeeCode;}return;}
    private Task SetLoanRepaymentModeAsync(string? v){_loanRepaymentModeText=v??"1";return Task.CompletedTask;}
    private Task SetLoanPaymentMethodAsync(string? v){_loanPaymentMethodText=v??"1";return Task.CompletedTask;}
    private async Task SetAdjustmentEmployeeAsync(string? value){_adjustmentEmployeeId=Guid.TryParse(value,out var id)?id:null;if(_adjustmentEmployeeId.HasValue){var e=await Employees.GetByIdAsync(_adjustmentEmployeeId.Value);_adjustmentEmployeeName=e.DisplayName;_adjustmentEmployeeCode=e.EmployeeCode;}return;}
    private Task SetAdjustmentComponentAsync(string? v){_adjustmentComponentId=Guid.TryParse(v,out var id)?id:null;return Task.CompletedTask;}
    private Task SetAdjustmentTypeAsync(string? v){_adjustmentTypeText=v??"1";return Task.CompletedTask;}
}
