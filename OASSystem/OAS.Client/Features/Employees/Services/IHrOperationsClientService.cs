using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Adjustments;
using OAS.Contracts.Features.Employees.Leave;
using OAS.Contracts.Features.Employees.Loans;
using OAS.Contracts.Features.Employees.Overtime;
using OAS.Contracts.Features.Employees.Settings;
using OAS.Contracts.Features.Employees.Time;

namespace OAS.Client.Features.Employees.Services;

public interface IHrOperationsClientService
{
    Task<HrSettingsDto?> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<HrSettingsDto>> UpdateSettingsAsync(UpdateHrSettingsRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkShiftDto>> GetShiftsAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateShiftAsync(CreateWorkShiftRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateShiftAsync(Guid id, UpdateWorkShiftRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SetShiftStatusAsync(Guid id, SetWorkShiftStatusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeShiftAssignmentDto>> GetShiftAssignmentsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateShiftAssignmentAsync(Guid employeeId, CreateEmployeeShiftAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateShiftAssignmentAsync(Guid id, UpdateEmployeeShiftAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SetShiftAssignmentStatusAsync(Guid id, SetEmployeeShiftAssignmentStatusRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateHolidayAsync(CreateHolidayRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateHolidayAsync(Guid id, UpdateHolidayRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SetHolidayStatusAsync(Guid id, SetHolidayStatusRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecordDto>> GetAttendanceAsync(DateOnly? from = null, DateOnly? to = null, Guid? employeeId = null, Guid? departmentId = null, byte? approvalStatus = null, CancellationToken cancellationToken = default);
    Task<ApiCallResult<int>> GenerateAttendanceAsync(GenerateAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateAttendanceAsync(CreateAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateAttendanceAsync(Guid id, UpdateAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SubmitAttendanceAsync(Guid id, AttendanceTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> ApproveAttendanceAsync(Guid id, AttendanceTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> RejectAttendanceAsync(Guid id, AttendanceTransitionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateLeaveTypeAsync(CreateLeaveTypeRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SetLeaveTypeStatusAsync(Guid id, SetLeaveTypeStatusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeLeaveBalanceDto>> GetLeaveBalancesAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SetLeaveBalanceAsync(Guid employeeId, SetEmployeeLeaveBalanceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequestDto>> GetLeaveRequestsAsync(Guid? employeeId = null, byte? status = null, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateLeaveRequestAsync(CreateLeaveRequestRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateLeaveRequestAsync(Guid id, UpdateLeaveRequestRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SubmitLeaveAsync(Guid id, LeaveTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> ApproveLeaveAsync(Guid id, LeaveTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> RejectLeaveAsync(Guid id, LeaveTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CancelLeaveAsync(Guid id, LeaveTransitionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OvertimeRecordDto>> GetOvertimeAsync(Guid? employeeId = null, byte? status = null, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateOvertimeAsync(CreateOvertimeRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateOvertimeAsync(Guid id, UpdateOvertimeRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SubmitOvertimeAsync(Guid id, OvertimeTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> ApproveOvertimeAsync(Guid id, OvertimeTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> RejectOvertimeAsync(Guid id, OvertimeTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CancelOvertimeAsync(Guid id, OvertimeTransitionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeLoanDto>> GetLoansAsync(Guid? employeeId = null, byte? status = null, CancellationToken cancellationToken = default);
    Task<EmployeeLoanDto?> GetLoanAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateLoanAsync(CreateEmployeeLoanRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateLoanAsync(Guid id, UpdateEmployeeLoanRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SubmitLoanAsync(Guid id, EmployeeLoanTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> ApproveLoanAsync(Guid id, EmployeeLoanTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> RejectLoanAsync(Guid id, EmployeeLoanTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CancelLoanAsync(Guid id, EmployeeLoanTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> DisburseLoanAsync(Guid id, DisburseEmployeeLoanRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> PayInstallmentExternallyAsync(Guid installmentId, PayLoanInstallmentExternallyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeAdjustmentDto>> GetAdjustmentsAsync(Guid? employeeId = null, byte? status = null, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CreateAdjustmentAsync(CreateEmployeeAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UpdateAdjustmentAsync(Guid id, UpdateEmployeeAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> SubmitAdjustmentAsync(Guid id, EmployeeAdjustmentTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> ApproveAdjustmentAsync(Guid id, EmployeeAdjustmentTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> RejectAdjustmentAsync(Guid id, EmployeeAdjustmentTransitionRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> CancelAdjustmentAsync(Guid id, EmployeeAdjustmentTransitionRequest request, CancellationToken cancellationToken = default);
}
