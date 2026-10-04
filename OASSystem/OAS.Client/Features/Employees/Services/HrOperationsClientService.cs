using System.Globalization;
using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Adjustments;
using OAS.Contracts.Features.Employees.Leave;
using OAS.Contracts.Features.Employees.Loans;
using OAS.Contracts.Features.Employees.Overtime;
using OAS.Contracts.Features.Employees.Settings;
using OAS.Contracts.Features.Employees.Time;

namespace OAS.Client.Features.Employees.Services;

public sealed class HrOperationsClientService(OasApiClient api) : IHrOperationsClientService
{
    public Task<HrSettingsDto?> GetSettingsAsync(CancellationToken ct = default) => api.GetAsync<HrSettingsDto>("api/employees/hr-settings", ct);
    public Task<ApiCallResult<HrSettingsDto>> UpdateSettingsAsync(UpdateHrSettingsRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateHrSettingsRequest, HrSettingsDto>("api/employees/hr-settings", r, ct);

    public async Task<IReadOnlyList<WorkShiftDto>> GetShiftsAsync(bool activeOnly = false, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<WorkShiftDto>>($"api/employees/shifts?activeOnly={activeOnly.ToString().ToLowerInvariant()}", ct) ?? [];
    public Task<ApiCallResult<Guid>> CreateShiftAsync(CreateWorkShiftRequest r, CancellationToken ct = default) => api.PostResultAsync<CreateWorkShiftRequest, Guid>("api/employees/shifts", r, ct);
    public Task<ApiCallResult<Guid>> UpdateShiftAsync(Guid id, UpdateWorkShiftRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateWorkShiftRequest, Guid>($"api/employees/shifts/{id:D}", r, ct);
    public Task<ApiCallResult<Guid>> SetShiftStatusAsync(Guid id, SetWorkShiftStatusRequest r, CancellationToken ct = default) => api.PostResultAsync<SetWorkShiftStatusRequest, Guid>($"api/employees/shifts/{id:D}/status", r, ct);
    public async Task<IReadOnlyList<EmployeeShiftAssignmentDto>> GetShiftAssignmentsAsync(Guid employeeId, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<EmployeeShiftAssignmentDto>>($"api/employees/{employeeId:D}/shift-assignments", ct) ?? [];
    public Task<ApiCallResult<Guid>> CreateShiftAssignmentAsync(Guid employeeId, CreateEmployeeShiftAssignmentRequest r, CancellationToken ct = default) => api.PostResultAsync<CreateEmployeeShiftAssignmentRequest, Guid>($"api/employees/{employeeId:D}/shift-assignments", r, ct);
    public Task<ApiCallResult<Guid>> UpdateShiftAssignmentAsync(Guid id, UpdateEmployeeShiftAssignmentRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateEmployeeShiftAssignmentRequest, Guid>($"api/employees/shift-assignments/{id:D}", r, ct);
    public Task<ApiCallResult<Guid>> SetShiftAssignmentStatusAsync(Guid id, SetEmployeeShiftAssignmentStatusRequest r, CancellationToken ct = default) => api.PostResultAsync<SetEmployeeShiftAssignmentStatusRequest, Guid>($"api/employees/shift-assignments/{id:D}/status", r, ct);

    public async Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(bool activeOnly = false, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<HolidayDto>>($"api/employees/holidays?activeOnly={activeOnly.ToString().ToLowerInvariant()}", ct) ?? [];
    public Task<ApiCallResult<Guid>> CreateHolidayAsync(CreateHolidayRequest r, CancellationToken ct = default) => api.PostResultAsync<CreateHolidayRequest, Guid>("api/employees/holidays", r, ct);
    public Task<ApiCallResult<Guid>> UpdateHolidayAsync(Guid id, UpdateHolidayRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateHolidayRequest, Guid>($"api/employees/holidays/{id:D}", r, ct);
    public Task<ApiCallResult<Guid>> SetHolidayStatusAsync(Guid id, SetHolidayStatusRequest r, CancellationToken ct = default) => api.PostResultAsync<SetHolidayStatusRequest, Guid>($"api/employees/holidays/{id:D}/status", r, ct);

    public async Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(DateOnly? from = null, DateOnly? to = null, Guid? employeeId = null, Guid? departmentId = null, byte? approvalStatus = null, CancellationToken ct = default)
        => await api.GetAsync<IReadOnlyList<AttendanceRecordDto>>("api/employees/attendance" + Query(("from", Date(from)), ("to", Date(to)), ("employeeId", Id(employeeId)), ("departmentId", Id(departmentId)), ("approvalStatus", approvalStatus?.ToString(CultureInfo.InvariantCulture))), ct) ?? [];
    public Task<ApiCallResult<int>> GenerateAttendanceAsync(GenerateAttendanceRequest r, CancellationToken ct = default) => api.PostResultAsync<GenerateAttendanceRequest, int>("api/employees/attendance/generate", r, ct);
    public Task<ApiCallResult<Guid>> CreateAttendanceAsync(CreateAttendanceRequest r, CancellationToken ct = default) => api.PostResultAsync<CreateAttendanceRequest, Guid>("api/employees/attendance", r, ct);
    public Task<ApiCallResult<Guid>> UpdateAttendanceAsync(Guid id, UpdateAttendanceRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateAttendanceRequest, Guid>($"api/employees/attendance/{id:D}", r, ct);
    public Task<ApiCallResult<Guid>> SubmitAttendanceAsync(Guid id, AttendanceTransitionRequest r, CancellationToken ct = default) => api.PostResultAsync<AttendanceTransitionRequest, Guid>($"api/employees/attendance/{id:D}/submit", r, ct);
    public Task<ApiCallResult<Guid>> ApproveAttendanceAsync(Guid id, AttendanceTransitionRequest r, CancellationToken ct = default) => api.PostResultAsync<AttendanceTransitionRequest, Guid>($"api/employees/attendance/{id:D}/approve", r, ct);
    public Task<ApiCallResult<Guid>> RejectAttendanceAsync(Guid id, AttendanceTransitionRequest r, CancellationToken ct = default) => api.PostResultAsync<AttendanceTransitionRequest, Guid>($"api/employees/attendance/{id:D}/reject", r, ct);

    public async Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(bool activeOnly = false, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<LeaveTypeDto>>($"api/employees/leave/types?activeOnly={activeOnly.ToString().ToLowerInvariant()}", ct) ?? [];
    public Task<ApiCallResult<Guid>> CreateLeaveTypeAsync(CreateLeaveTypeRequest r, CancellationToken ct = default) => api.PostResultAsync<CreateLeaveTypeRequest, Guid>("api/employees/leave/types", r, ct);
    public Task<ApiCallResult<Guid>> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateLeaveTypeRequest, Guid>($"api/employees/leave/types/{id:D}", r, ct);
    public Task<ApiCallResult<Guid>> SetLeaveTypeStatusAsync(Guid id, SetLeaveTypeStatusRequest r, CancellationToken ct = default) => api.PostResultAsync<SetLeaveTypeStatusRequest, Guid>($"api/employees/leave/types/{id:D}/status", r, ct);
    public async Task<IReadOnlyList<EmployeeLeaveBalanceDto>> GetLeaveBalancesAsync(Guid employeeId, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<EmployeeLeaveBalanceDto>>($"api/employees/{employeeId:D}/leave-balances", ct) ?? [];
    public Task<ApiCallResult<Guid>> SetLeaveBalanceAsync(Guid employeeId, SetEmployeeLeaveBalanceRequest r, CancellationToken ct = default) => api.PostResultAsync<SetEmployeeLeaveBalanceRequest, Guid>($"api/employees/{employeeId:D}/leave-balances", r, ct);
    public async Task<IReadOnlyList<LeaveRequestDto>> GetLeaveRequestsAsync(Guid? employeeId = null, byte? status = null, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<LeaveRequestDto>>("api/employees/leave/requests" + Query(("employeeId", Id(employeeId)), ("status", status?.ToString(CultureInfo.InvariantCulture))), ct) ?? [];
    public Task<ApiCallResult<Guid>> CreateLeaveRequestAsync(CreateLeaveRequestRequest r, CancellationToken ct = default) => api.PostResultAsync<CreateLeaveRequestRequest, Guid>("api/employees/leave/requests", r, ct);
    public Task<ApiCallResult<Guid>> UpdateLeaveRequestAsync(Guid id, UpdateLeaveRequestRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateLeaveRequestRequest, Guid>($"api/employees/leave/requests/{id:D}", r, ct);
    public Task<ApiCallResult<Guid>> SubmitLeaveAsync(Guid id, LeaveTransitionRequest r, CancellationToken ct = default) => Transition($"api/employees/leave/requests/{id:D}/submit", r, ct);
    public Task<ApiCallResult<Guid>> ApproveLeaveAsync(Guid id, LeaveTransitionRequest r, CancellationToken ct = default) => Transition($"api/employees/leave/requests/{id:D}/approve", r, ct);
    public Task<ApiCallResult<Guid>> RejectLeaveAsync(Guid id, LeaveTransitionRequest r, CancellationToken ct = default) => Transition($"api/employees/leave/requests/{id:D}/reject", r, ct);
    public Task<ApiCallResult<Guid>> CancelLeaveAsync(Guid id, LeaveTransitionRequest r, CancellationToken ct = default) => Transition($"api/employees/leave/requests/{id:D}/cancel", r, ct);

    public async Task<IReadOnlyList<OvertimeRecordDto>> GetOvertimeAsync(Guid? employeeId = null, byte? status = null, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<OvertimeRecordDto>>("api/employees/overtime" + Query(("employeeId", Id(employeeId)), ("status", status?.ToString(CultureInfo.InvariantCulture))), ct) ?? [];
    public Task<ApiCallResult<Guid>> CreateOvertimeAsync(CreateOvertimeRequest r, CancellationToken ct = default) => api.PostResultAsync<CreateOvertimeRequest, Guid>("api/employees/overtime", r, ct);
    public Task<ApiCallResult<Guid>> UpdateOvertimeAsync(Guid id, UpdateOvertimeRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateOvertimeRequest, Guid>($"api/employees/overtime/{id:D}", r, ct);
    public Task<ApiCallResult<Guid>> SubmitOvertimeAsync(Guid id, OvertimeTransitionRequest r, CancellationToken ct = default) => api.PostResultAsync<OvertimeTransitionRequest, Guid>($"api/employees/overtime/{id:D}/submit", r, ct);
    public Task<ApiCallResult<Guid>> ApproveOvertimeAsync(Guid id, OvertimeTransitionRequest r, CancellationToken ct = default) => api.PostResultAsync<OvertimeTransitionRequest, Guid>($"api/employees/overtime/{id:D}/approve", r, ct);
    public Task<ApiCallResult<Guid>> RejectOvertimeAsync(Guid id, OvertimeTransitionRequest r, CancellationToken ct = default) => api.PostResultAsync<OvertimeTransitionRequest, Guid>($"api/employees/overtime/{id:D}/reject", r, ct);
    public Task<ApiCallResult<Guid>> CancelOvertimeAsync(Guid id, OvertimeTransitionRequest r, CancellationToken ct = default) => api.PostResultAsync<OvertimeTransitionRequest, Guid>($"api/employees/overtime/{id:D}/cancel", r, ct);

    public async Task<IReadOnlyList<EmployeeLoanDto>> GetLoansAsync(Guid? employeeId = null, byte? status = null, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<EmployeeLoanDto>>("api/employees/loans" + Query(("employeeId", Id(employeeId)), ("status", status?.ToString(CultureInfo.InvariantCulture))), ct) ?? [];
    public Task<EmployeeLoanDto?> GetLoanAsync(Guid id, CancellationToken ct = default) => api.GetAsync<EmployeeLoanDto>($"api/employees/loans/{id:D}", ct);
    public Task<ApiCallResult<Guid>> CreateLoanAsync(CreateEmployeeLoanRequest r, CancellationToken ct = default) => api.PostResultAsync<CreateEmployeeLoanRequest, Guid>("api/employees/loans", r, ct);
    public Task<ApiCallResult<Guid>> UpdateLoanAsync(Guid id, UpdateEmployeeLoanRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateEmployeeLoanRequest, Guid>($"api/employees/loans/{id:D}", r, ct);
    public Task<ApiCallResult<Guid>> SubmitLoanAsync(Guid id, EmployeeLoanTransitionRequest r, CancellationToken ct = default) => LoanTransition(id, "submit", r, ct);
    public Task<ApiCallResult<Guid>> ApproveLoanAsync(Guid id, EmployeeLoanTransitionRequest r, CancellationToken ct = default) => LoanTransition(id, "approve", r, ct);
    public Task<ApiCallResult<Guid>> RejectLoanAsync(Guid id, EmployeeLoanTransitionRequest r, CancellationToken ct = default) => LoanTransition(id, "reject", r, ct);
    public Task<ApiCallResult<Guid>> CancelLoanAsync(Guid id, EmployeeLoanTransitionRequest r, CancellationToken ct = default) => LoanTransition(id, "cancel", r, ct);
    public Task<ApiCallResult<Guid>> DisburseLoanAsync(Guid id, DisburseEmployeeLoanRequest r, CancellationToken ct = default) => api.PostResultAsync<DisburseEmployeeLoanRequest, Guid>($"api/employees/loans/{id:D}/disburse", r, ct);
    public Task<ApiCallResult<Guid>> PayInstallmentExternallyAsync(Guid installmentId, PayLoanInstallmentExternallyRequest r, CancellationToken ct = default) => api.PostResultAsync<PayLoanInstallmentExternallyRequest, Guid>($"api/employees/loans/installments/{installmentId:D}/external-payment", r, ct);

    public async Task<IReadOnlyList<EmployeeAdjustmentDto>> GetAdjustmentsAsync(Guid? employeeId = null, byte? status = null, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<EmployeeAdjustmentDto>>("api/employees/adjustments" + Query(("employeeId", Id(employeeId)), ("status", status?.ToString(CultureInfo.InvariantCulture))), ct) ?? [];
    public Task<ApiCallResult<Guid>> CreateAdjustmentAsync(CreateEmployeeAdjustmentRequest r, CancellationToken ct = default) => api.PostResultAsync<CreateEmployeeAdjustmentRequest, Guid>("api/employees/adjustments", r, ct);
    public Task<ApiCallResult<Guid>> UpdateAdjustmentAsync(Guid id, UpdateEmployeeAdjustmentRequest r, CancellationToken ct = default) => api.PutResultAsync<UpdateEmployeeAdjustmentRequest, Guid>($"api/employees/adjustments/{id:D}", r, ct);
    public Task<ApiCallResult<Guid>> SubmitAdjustmentAsync(Guid id, EmployeeAdjustmentTransitionRequest r, CancellationToken ct = default) => AdjustmentTransition(id, "submit", r, ct);
    public Task<ApiCallResult<Guid>> ApproveAdjustmentAsync(Guid id, EmployeeAdjustmentTransitionRequest r, CancellationToken ct = default) => AdjustmentTransition(id, "approve", r, ct);
    public Task<ApiCallResult<Guid>> RejectAdjustmentAsync(Guid id, EmployeeAdjustmentTransitionRequest r, CancellationToken ct = default) => AdjustmentTransition(id, "reject", r, ct);
    public Task<ApiCallResult<Guid>> CancelAdjustmentAsync(Guid id, EmployeeAdjustmentTransitionRequest r, CancellationToken ct = default) => AdjustmentTransition(id, "cancel", r, ct);

    private Task<ApiCallResult<Guid>> Transition(string uri, LeaveTransitionRequest r, CancellationToken ct) => api.PostResultAsync<LeaveTransitionRequest, Guid>(uri, r, ct);
    private Task<ApiCallResult<Guid>> LoanTransition(Guid id, string action, EmployeeLoanTransitionRequest r, CancellationToken ct) => api.PostResultAsync<EmployeeLoanTransitionRequest, Guid>($"api/employees/loans/{id:D}/{action}", r, ct);
    private Task<ApiCallResult<Guid>> AdjustmentTransition(Guid id, string action, EmployeeAdjustmentTransitionRequest r, CancellationToken ct) => api.PostResultAsync<EmployeeAdjustmentTransitionRequest, Guid>($"api/employees/adjustments/{id:D}/{action}", r, ct);
    private static string? Id(Guid? id) => id?.ToString("D");
    private static string? Date(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string Query(params (string Key, string? Value)[] values)
    {
        var parts = values.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}").ToArray();
        return parts.Length == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
