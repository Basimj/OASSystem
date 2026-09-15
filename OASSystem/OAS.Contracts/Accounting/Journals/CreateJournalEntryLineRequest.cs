namespace OAS.Contracts.Accounting.Journals;

public sealed record CreateJournalEntryLineRequest(
    Guid AccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description = null,
    Guid? CustomerId = null,
    Guid? SupplierId = null,
    Guid? CostCenterId = null,
    Guid? ProductVariantId = null,
    Guid? WarehouseId = null);