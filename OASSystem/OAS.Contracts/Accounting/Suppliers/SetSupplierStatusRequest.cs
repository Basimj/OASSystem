namespace OAS.Contracts.Accounting.Suppliers;
public sealed record SetSupplierStatusRequest(bool IsActive,string RowVersion);
