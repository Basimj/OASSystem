namespace OAS.Contracts.Accounting.EmployeeAccounts;
public sealed record EmployeeAccountDto(Guid Id,Guid EmployeeId,string EmployeeCode,string EmployeeName,Guid AccountId,string AccountCode,string AccountName,bool IsActive,string RowVersion);
