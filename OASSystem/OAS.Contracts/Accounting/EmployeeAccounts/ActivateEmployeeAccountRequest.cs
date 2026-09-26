namespace OAS.Contracts.Accounting.EmployeeAccounts;
public sealed record ActivateEmployeeAccountRequest(Guid EmployeeId,bool IsActive=true);
