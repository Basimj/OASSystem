namespace OAS.Contracts.Features.Employees.Overtime;
public sealed record OvertimeRecordDto(Guid Id,string OvertimeCode,Guid EmployeeId,string EmployeeCode,string EmployeeName,Guid? AttendanceRecordId,DateOnly WorkDate,int RequestedMinutes,int ApprovedMinutes,decimal RateMultiplier,byte Status,string? Reason,string RowVersion);
public sealed record CreateOvertimeRequest(Guid EmployeeId,Guid? AttendanceRecordId,DateOnly WorkDate,int RequestedMinutes,decimal RateMultiplier,string? Reason);
public sealed record UpdateOvertimeRequest(int RequestedMinutes,decimal RateMultiplier,string? Reason,string RowVersion);
public sealed record OvertimeTransitionRequest(string RowVersion,string? Reason=null,int? ApprovedMinutes=null);
