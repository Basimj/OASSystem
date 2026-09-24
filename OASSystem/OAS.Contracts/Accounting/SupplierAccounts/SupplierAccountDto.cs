namespace OAS.Contracts.Accounting.SupplierAccounts;

public sealed record SupplierAccountDto(
    Guid Id,
    Guid SupplierId,
    Guid AccountId,
    Guid ControlAccountId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    string RowVersion);