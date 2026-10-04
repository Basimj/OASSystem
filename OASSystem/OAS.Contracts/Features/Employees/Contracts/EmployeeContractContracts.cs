namespace OAS.Contracts.Features.Employees.Contracts;

public sealed record EmployeeContractDto(Guid Id, string ContractCode, Guid EmployeeId, byte ContractType, byte Status, DateOnly StartDate, DateOnly? EndDate, DateOnly? ProbationEndDate, decimal? WorkingHoursPerDay, decimal? WorkingDaysPerWeek, Guid CurrencyId, string CurrencyCode, string? Notes, string? ActivatedBy, DateTimeOffset? ActivatedAtUtc, string? TerminatedBy, DateTimeOffset? TerminatedAtUtc, DateOnly? TerminationEffectiveDate, string? TerminationReason, string RowVersion);
public sealed record CreateEmployeeContractRequest(byte ContractType, DateOnly StartDate, DateOnly? EndDate, DateOnly? ProbationEndDate, decimal? WorkingHoursPerDay, decimal? WorkingDaysPerWeek, Guid CurrencyId, string? Notes);
public sealed record UpdateEmployeeContractRequest(byte ContractType, DateOnly StartDate, DateOnly? EndDate, DateOnly? ProbationEndDate, decimal? WorkingHoursPerDay, decimal? WorkingDaysPerWeek, Guid CurrencyId, string? Notes, string RowVersion);
public sealed record TerminateEmployeeContractRequest(string Reason, DateOnly TerminationEffectiveDate, string RowVersion);
public sealed record ContractLifecycleRequest(string RowVersion);
