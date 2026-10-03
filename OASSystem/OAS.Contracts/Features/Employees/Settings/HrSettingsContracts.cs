namespace OAS.Contracts.Features.Employees.Settings;
public sealed record HrSettingsDto(Guid Id,string? TimeZoneId,byte LeaveYearStartMonth,bool RequireAttendanceApproval,string RowVersion);
public sealed record UpdateHrSettingsRequest(string? TimeZoneId,byte LeaveYearStartMonth,bool RequireAttendanceApproval,string? RowVersion);
