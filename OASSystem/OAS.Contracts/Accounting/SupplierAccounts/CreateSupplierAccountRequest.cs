namespace OAS.Contracts.Accounting.SupplierAccounts;

public sealed record CreateSupplierAccountRequest(
    Guid SupplierId,
    Guid AccountId,
    Guid ControlAccountId,
    bool IsActive = true);