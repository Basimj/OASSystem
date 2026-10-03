namespace OAS.Contracts.Features.Employees.Time;
public sealed record HolidayDto(Guid Id,string HolidayCode,string NameAr,string? NameEn,DateOnly StartDate,DateOnly EndDate,bool IsPaid,bool IsActive,string? Notes,string RowVersion);
public sealed record CreateHolidayRequest(string NameAr,string? NameEn,DateOnly StartDate,DateOnly EndDate,bool IsPaid=true,bool IsActive=true,string? Notes=null);
public sealed record UpdateHolidayRequest(string NameAr,string? NameEn,DateOnly StartDate,DateOnly EndDate,bool IsPaid,bool IsActive,string? Notes,string RowVersion);
public sealed record SetHolidayStatusRequest(bool IsActive,string RowVersion);
