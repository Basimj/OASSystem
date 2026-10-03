namespace OAS.Contracts.Features.Employees.Time;
public sealed record AttendanceRecordDto(Guid Id,Guid EmployeeId,string EmployeeCode,string EmployeeName,DateOnly AttendanceDate,Guid? WorkShiftId,string? ShiftCode,string? ShiftName,DateTimeOffset? CheckInAtUtc,DateTimeOffset? CheckOutAtUtc,byte Source,byte AttendanceStatus,byte ApprovalStatus,int ScheduledMinutes,int WorkedMinutes,int LateMinutes,int EarlyLeaveMinutes,int OvertimeMinutes,string? Notes,string RowVersion);
public sealed record GenerateAttendanceRequest(DateOnly FromDate,DateOnly ToDate,Guid? EmployeeId=null,Guid? DepartmentId=null);
public sealed record CreateAttendanceRequest(Guid EmployeeId,DateOnly AttendanceDate,DateTimeOffset? CheckInAtUtc,DateTimeOffset? CheckOutAtUtc,byte Source=1,string? Notes=null);
public sealed record UpdateAttendanceRequest(DateTimeOffset? CheckInAtUtc,DateTimeOffset? CheckOutAtUtc,byte Source,string? Notes,string? CorrectionReason,string RowVersion);
public sealed record AttendanceTransitionRequest(string RowVersion,string? Reason=null);
