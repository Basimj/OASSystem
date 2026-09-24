namespace OAS.Contracts.Accounting.SupplierAccounts;

public sealed record SetSupplierAccountStatusRequest(
    bool IsActive,
    string RowVersion);