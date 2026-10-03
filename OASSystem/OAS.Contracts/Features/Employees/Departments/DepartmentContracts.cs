namespace OAS.Contracts.Features.Employees.Departments;

public sealed record DepartmentDto(Guid Id, string DepartmentCode, string NameAr, string? NameEn, Guid? ParentDepartmentId, string? ParentDepartmentName, Guid? ManagerEmployeeId, string? ManagerEmployeeName, bool IsActive, string? Notes, string RowVersion, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastModifiedAtUtc);
public sealed record CreateDepartmentRequest(string NameAr, string? NameEn, Guid? ParentDepartmentId, Guid? ManagerEmployeeId, bool IsActive = true, string? Notes = null);
public sealed record UpdateDepartmentRequest(string NameAr, string? NameEn, Guid? ParentDepartmentId, Guid? ManagerEmployeeId, bool IsActive, string? Notes, string RowVersion);
public sealed record SetDepartmentStatusRequest(bool IsActive, string RowVersion);
