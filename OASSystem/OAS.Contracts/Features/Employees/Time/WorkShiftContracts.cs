namespace OAS.Contracts.Features.Employees.Time;
public sealed record WorkShiftDto(Guid Id,string ShiftCode,string NameAr,string? NameEn,TimeOnly StartTime,TimeOnly EndTime,int BreakMinutes,int GraceLateMinutes,int GraceEarlyLeaveMinutes,byte WorkingDaysMask,bool IsFlexible,bool IsActive,string? Notes,int ScheduledMinutes,bool CrossesMidnight,string RowVersion);
public sealed record CreateWorkShiftRequest(string NameAr,string? NameEn,TimeOnly StartTime,TimeOnly EndTime,int BreakMinutes,int GraceLateMinutes,int GraceEarlyLeaveMinutes,byte WorkingDaysMask,bool IsFlexible,bool IsActive=true,string? Notes=null);
public sealed record UpdateWorkShiftRequest(string NameAr,string? NameEn,TimeOnly StartTime,TimeOnly EndTime,int BreakMinutes,int GraceLateMinutes,int GraceEarlyLeaveMinutes,byte WorkingDaysMask,bool IsFlexible,bool IsActive,string? Notes,string RowVersion);
public sealed record SetWorkShiftStatusRequest(bool IsActive,string RowVersion);
public sealed record EmployeeShiftAssignmentDto(Guid Id,Guid EmployeeId,Guid WorkShiftId,string ShiftCode,string ShiftName,DateOnly EffectiveFrom,DateOnly? EffectiveTo,bool IsActive,string RowVersion);
public sealed record CreateEmployeeShiftAssignmentRequest(Guid WorkShiftId,DateOnly EffectiveFrom,DateOnly? EffectiveTo,bool IsActive=true);
public sealed record UpdateEmployeeShiftAssignmentRequest(Guid WorkShiftId,DateOnly EffectiveFrom,DateOnly? EffectiveTo,bool IsActive,string RowVersion);
public sealed record SetEmployeeShiftAssignmentStatusRequest(bool IsActive,string RowVersion);
