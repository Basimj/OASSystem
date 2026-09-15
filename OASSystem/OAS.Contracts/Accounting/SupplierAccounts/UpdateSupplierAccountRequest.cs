namespace OAS.Contracts.Accounting.SupplierAccounts;

public sealed record UpdateSupplierAccountRequest(
    Guid AccountId,
    Guid ControlAccountId,
    bool IsActive,
    string RowVersion);