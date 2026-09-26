namespace OAS.Contracts.Accounting.EmployeeAccounts;
public sealed record SetEmployeeAccountStatusRequest(bool IsActive,string RowVersion);
