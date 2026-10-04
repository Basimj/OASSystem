namespace OAS.Contracts.Features.Employees.Adjustments;
public sealed record EmployeeAdjustmentDto(Guid Id,string AdjustmentCode,Guid EmployeeId,string EmployeeCode,string EmployeeName,Guid SalaryComponentId,string ComponentCode,string ComponentName,byte ComponentType,byte AdjustmentType,DateOnly EffectiveDate,Guid CurrencyId,string CurrencyCode,string? CurrencySymbol,byte CurrencyDecimalPlaces,decimal Amount,byte Status,string Reason,string RowVersion);
public sealed record CreateEmployeeAdjustmentRequest(Guid EmployeeId,Guid SalaryComponentId,DateOnly EffectiveDate,byte AdjustmentType,decimal Amount,string Reason);
public sealed record UpdateEmployeeAdjustmentRequest(byte AdjustmentType,decimal Amount,string Reason,string RowVersion);
public sealed record EmployeeAdjustmentTransitionRequest(string RowVersion,string? Reason=null);
