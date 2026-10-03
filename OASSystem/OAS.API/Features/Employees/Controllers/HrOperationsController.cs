using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Adjustments;
using OAS.Application.Features.Employees.Leave;
using OAS.Application.Features.Employees.Loans;
using OAS.Application.Features.Employees.Overtime;
using OAS.Application.Features.Employees.Settings;
using OAS.Application.Features.Employees.Time.Attendance;
using OAS.Application.Features.Employees.Time.Holidays;
using OAS.Application.Features.Employees.Time.Shifts;
using OAS.Contracts.Features.Employees.Adjustments;
using OAS.Contracts.Features.Employees.Leave;
using OAS.Contracts.Features.Employees.Loans;
using OAS.Contracts.Features.Employees.Overtime;
using OAS.Contracts.Features.Employees.Settings;
using OAS.Contracts.Features.Employees.Time;

namespace OAS.API.Features.Employees.Controllers;
[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class HrOperationsController(ISender sender) : ControllerBase
{
    [HttpGet("hr-settings")]
    public async Task<ActionResult<HrSettingsDto>> GetSettings(CancellationToken ct)=>Ok(await sender.Send(new GetHrSettingsQuery(), ct));
    [HttpPut("hr-settings")]
    public async Task<ActionResult<HrSettingsDto>> UpdateSettings(UpdateHrSettingsRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateHrSettingsCommand(r), ct));
    [HttpGet("shifts")]
    public async Task<ActionResult<IReadOnlyList<WorkShiftDto>>> Shifts([FromQuery]bool activeOnly, CancellationToken ct)=>Ok(await sender.Send(new GetWorkShiftsQuery(activeOnly), ct));
    [HttpPost("shifts")]
    public async Task<ActionResult<Guid>> CreateShift(CreateWorkShiftRequest r, CancellationToken ct)=>Ok(await sender.Send(new CreateWorkShiftCommand(r), ct));
    [HttpPut("shifts/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateShift(Guid id, UpdateWorkShiftRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateWorkShiftCommand(id, r), ct));
    [HttpPost("shifts/{id:guid}/status")]
    public async Task<ActionResult<Guid>> ShiftStatus(Guid id, SetWorkShiftStatusRequest r, CancellationToken ct)=>Ok(await sender.Send(new SetWorkShiftStatusCommand(id, r), ct));
    [HttpGet("{employeeId:guid}/shift-assignments")]
    public async Task<ActionResult<IReadOnlyList<EmployeeShiftAssignmentDto>>> Assignments(Guid employeeId, CancellationToken ct)=>Ok(await sender.Send(new GetEmployeeShiftAssignmentsQuery(employeeId), ct));
    [HttpPost("{employeeId:guid}/shift-assignments")]
    public async Task<ActionResult<Guid>> CreateAssignment(Guid employeeId, CreateEmployeeShiftAssignmentRequest r, CancellationToken ct)=>Ok(await sender.Send(new CreateEmployeeShiftAssignmentCommand(employeeId, r), ct));
    [HttpPut("shift-assignments/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateAssignment(Guid id, UpdateEmployeeShiftAssignmentRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateEmployeeShiftAssignmentCommand(id, r), ct));
    [HttpPost("shift-assignments/{id:guid}/status")]
    public async Task<ActionResult<Guid>> AssignmentStatus(Guid id, SetEmployeeShiftAssignmentStatusRequest r, CancellationToken ct)=>Ok(await sender.Send(new SetEmployeeShiftAssignmentStatusCommand(id, r), ct));
    [HttpGet("holidays")]
    public async Task<ActionResult<IReadOnlyList<HolidayDto>>> Holidays([FromQuery]bool activeOnly, CancellationToken ct)=>Ok(await sender.Send(new GetHolidaysQuery(activeOnly), ct));
    [HttpPost("holidays")]
    public async Task<ActionResult<Guid>> CreateHoliday(CreateHolidayRequest r, CancellationToken ct)=>Ok(await sender.Send(new CreateHolidayCommand(r), ct));
    [HttpPut("holidays/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateHoliday(Guid id, UpdateHolidayRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateHolidayCommand(id, r), ct));
    [HttpPost("holidays/{id:guid}/status")]
    public async Task<ActionResult<Guid>> HolidayStatus(Guid id, SetHolidayStatusRequest r, CancellationToken ct)=>Ok(await sender.Send(new SetHolidayStatusCommand(id, r), ct));
    [HttpGet("attendance")]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordDto>>> Attendance([FromQuery]DateOnly? from, [FromQuery]DateOnly? to, [FromQuery]Guid? employeeId, [FromQuery]Guid? departmentId, [FromQuery]byte? approvalStatus, CancellationToken ct)=>Ok(await sender.Send(new GetAttendanceQuery(from, to, employeeId, departmentId, approvalStatus), ct));
    [HttpPost("attendance/generate")]
    public async Task<ActionResult<int>> GenerateAttendance(GenerateAttendanceRequest r, CancellationToken ct)=>Ok(await sender.Send(new GenerateAttendanceCommand(r), ct));
    [HttpPost("attendance")]
    public async Task<ActionResult<Guid>> CreateAttendance(CreateAttendanceRequest r, CancellationToken ct)=>Ok(await sender.Send(new CreateAttendanceCommand(r), ct));
    [HttpPut("attendance/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateAttendance(Guid id, UpdateAttendanceRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateAttendanceCommand(id, r), ct));
    [HttpPost("attendance/{id:guid}/submit")]
    public async Task<ActionResult<Guid>> SubmitAttendance(Guid id, AttendanceTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new SubmitAttendanceCommand(id, r), ct));
    [HttpPost("attendance/{id:guid}/approve")]
    public async Task<ActionResult<Guid>> ApproveAttendance(Guid id, AttendanceTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new ApproveAttendanceCommand(id, r), ct));
    [HttpPost("attendance/{id:guid}/reject")]
    public async Task<ActionResult<Guid>> RejectAttendance(Guid id, AttendanceTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new RejectAttendanceCommand(id, r), ct));
    [HttpGet("leave/types")]
    public async Task<ActionResult<IReadOnlyList<LeaveTypeDto>>> LeaveTypes([FromQuery]bool activeOnly, CancellationToken ct)=>Ok(await sender.Send(new GetLeaveTypesQuery(activeOnly), ct));
    [HttpPost("leave/types")]
    public async Task<ActionResult<Guid>> CreateLeaveType(CreateLeaveTypeRequest r, CancellationToken ct)=>Ok(await sender.Send(new CreateLeaveTypeCommand(r), ct));
    [HttpPut("leave/types/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateLeaveType(Guid id, UpdateLeaveTypeRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateLeaveTypeCommand(id, r), ct));
    [HttpPost("leave/types/{id:guid}/status")]
    public async Task<ActionResult<Guid>> LeaveTypeStatus(Guid id, SetLeaveTypeStatusRequest r, CancellationToken ct)=>Ok(await sender.Send(new SetLeaveTypeStatusCommand(id, r), ct));
    [HttpGet("{employeeId:guid}/leave-balances")]
    public async Task<ActionResult<IReadOnlyList<EmployeeLeaveBalanceDto>>> Balances(Guid employeeId, CancellationToken ct)=>Ok(await sender.Send(new GetEmployeeLeaveBalancesQuery(employeeId), ct));
    [HttpPost("{employeeId:guid}/leave-balances")]
    public async Task<ActionResult<Guid>> SetBalance(Guid employeeId, SetEmployeeLeaveBalanceRequest r, CancellationToken ct)=>Ok(await sender.Send(new SetEmployeeLeaveBalanceCommand(employeeId, r), ct));
    [HttpGet("leave/requests")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> LeaveRequests([FromQuery]Guid? employeeId, [FromQuery]byte? status, CancellationToken ct)=>Ok(await sender.Send(new GetLeaveRequestsQuery(employeeId, status), ct));
    [HttpPost("leave/requests")]
    public async Task<ActionResult<Guid>> CreateLeave(CreateLeaveRequestRequest r, CancellationToken ct)=>Ok(await sender.Send(new CreateLeaveRequestCommand(r), ct));
    [HttpPut("leave/requests/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateLeave(Guid id, UpdateLeaveRequestRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateLeaveRequestCommand(id, r), ct));
    [HttpPost("leave/requests/{id:guid}/submit")]
    public async Task<ActionResult<Guid>> SubmitLeave(Guid id, LeaveTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new SubmitLeaveRequestCommand(id, r), ct));
    [HttpPost("leave/requests/{id:guid}/approve")]
    public async Task<ActionResult<Guid>> ApproveLeave(Guid id, LeaveTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new ApproveLeaveRequestCommand(id, r), ct));
    [HttpPost("leave/requests/{id:guid}/reject")]
    public async Task<ActionResult<Guid>> RejectLeave(Guid id, LeaveTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new RejectLeaveRequestCommand(id, r), ct));
    [HttpPost("leave/requests/{id:guid}/cancel")]
    public async Task<ActionResult<Guid>> CancelLeave(Guid id, LeaveTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new CancelLeaveRequestCommand(id, r), ct));
    [HttpGet("overtime")]
    public async Task<ActionResult<IReadOnlyList<OvertimeRecordDto>>> Overtime([FromQuery]Guid? employeeId, [FromQuery]byte? status, CancellationToken ct)=>Ok(await sender.Send(new GetOvertimeQuery(employeeId, status), ct));
    [HttpPost("overtime")]
    public async Task<ActionResult<Guid>> CreateOvertime(CreateOvertimeRequest r, CancellationToken ct)=>Ok(await sender.Send(new CreateOvertimeCommand(r), ct));
    [HttpPut("overtime/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateOvertime(Guid id, UpdateOvertimeRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateOvertimeCommand(id, r), ct));
    [HttpPost("overtime/{id:guid}/submit")]
    public async Task<ActionResult<Guid>> SubmitOvertime(Guid id, OvertimeTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new SubmitOvertimeCommand(id, r), ct));
    [HttpPost("overtime/{id:guid}/approve")]
    public async Task<ActionResult<Guid>> ApproveOvertime(Guid id, OvertimeTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new ApproveOvertimeCommand(id, r), ct));
    [HttpPost("overtime/{id:guid}/reject")]
    public async Task<ActionResult<Guid>> RejectOvertime(Guid id, OvertimeTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new RejectOvertimeCommand(id, r), ct));
    [HttpPost("overtime/{id:guid}/cancel")]
    public async Task<ActionResult<Guid>> CancelOvertime(Guid id, OvertimeTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new CancelOvertimeCommand(id, r), ct));
    [HttpGet("loans")]
    public async Task<ActionResult<IReadOnlyList<EmployeeLoanDto>>> Loans([FromQuery]Guid? employeeId, [FromQuery]byte? status, CancellationToken ct)=>Ok(await sender.Send(new GetEmployeeLoansQuery(employeeId, status), ct));
    [HttpGet("loans/{id:guid}")]
    public async Task<ActionResult<EmployeeLoanDto>> Loan(Guid id, CancellationToken ct)=>Ok(await sender.Send(new GetEmployeeLoanByIdQuery(id), ct));
    [HttpPost("loans")]
    public async Task<ActionResult<Guid>> CreateLoan(CreateEmployeeLoanRequest r, CancellationToken ct)=>Ok(await sender.Send(new CreateEmployeeLoanCommand(r), ct));
    [HttpPut("loans/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateLoan(Guid id, UpdateEmployeeLoanRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateEmployeeLoanCommand(id, r), ct));
    [HttpPost("loans/{id:guid}/submit")]
    public async Task<ActionResult<Guid>> SubmitLoan(Guid id, EmployeeLoanTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new SubmitEmployeeLoanCommand(id, r), ct));
    [HttpPost("loans/{id:guid}/approve")]
    public async Task<ActionResult<Guid>> ApproveLoan(Guid id, EmployeeLoanTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new ApproveEmployeeLoanCommand(id, r), ct));
    [HttpPost("loans/{id:guid}/reject")]
    public async Task<ActionResult<Guid>> RejectLoan(Guid id, EmployeeLoanTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new RejectEmployeeLoanCommand(id, r), ct));
    [HttpPost("loans/{id:guid}/cancel")]
    public async Task<ActionResult<Guid>> CancelLoan(Guid id, EmployeeLoanTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new CancelEmployeeLoanCommand(id, r), ct));
    [HttpPost("loans/{id:guid}/disburse")]
    public async Task<ActionResult<Guid>> DisburseLoan(Guid id, DisburseEmployeeLoanRequest r, CancellationToken ct)=>Ok(await sender.Send(new DisburseEmployeeLoanCommand(id, r), ct));
    [HttpPost("loans/installments/{id:guid}/external-payment")]
    public async Task<ActionResult<Guid>> PayInstallment(Guid id, PayLoanInstallmentExternallyRequest r, CancellationToken ct)=>Ok(await sender.Send(new PayLoanInstallmentExternallyCommand(id, r), ct));
    [HttpGet("adjustments")]
    public async Task<ActionResult<IReadOnlyList<EmployeeAdjustmentDto>>> Adjustments([FromQuery]Guid? employeeId, [FromQuery]byte? status, CancellationToken ct)=>Ok(await sender.Send(new GetEmployeeAdjustmentsQuery(employeeId, status), ct));
    [HttpPost("adjustments")]
    public async Task<ActionResult<Guid>> CreateAdjustment(CreateEmployeeAdjustmentRequest r, CancellationToken ct)=>Ok(await sender.Send(new CreateEmployeeAdjustmentCommand(r), ct));
    [HttpPut("adjustments/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateAdjustment(Guid id, UpdateEmployeeAdjustmentRequest r, CancellationToken ct)=>Ok(await sender.Send(new UpdateEmployeeAdjustmentCommand(id, r), ct));
    [HttpPost("adjustments/{id:guid}/submit")]
    public async Task<ActionResult<Guid>> SubmitAdjustment(Guid id, EmployeeAdjustmentTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new SubmitEmployeeAdjustmentCommand(id, r), ct));
    [HttpPost("adjustments/{id:guid}/approve")]
    public async Task<ActionResult<Guid>> ApproveAdjustment(Guid id, EmployeeAdjustmentTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new ApproveEmployeeAdjustmentCommand(id, r), ct));
    [HttpPost("adjustments/{id:guid}/reject")]
    public async Task<ActionResult<Guid>> RejectAdjustment(Guid id, EmployeeAdjustmentTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new RejectEmployeeAdjustmentCommand(id, r), ct));
    [HttpPost("adjustments/{id:guid}/cancel")]
    public async Task<ActionResult<Guid>> CancelAdjustment(Guid id, EmployeeAdjustmentTransitionRequest r, CancellationToken ct)=>Ok(await sender.Send(new CancelEmployeeAdjustmentCommand(id, r), ct));
}
