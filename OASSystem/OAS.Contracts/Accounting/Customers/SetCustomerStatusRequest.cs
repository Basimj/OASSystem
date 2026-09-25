namespace OAS.Contracts.Accounting.Customers;
public sealed record SetCustomerStatusRequest(bool IsActive,string RowVersion);
