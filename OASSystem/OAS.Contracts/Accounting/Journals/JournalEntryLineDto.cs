namespace OAS.Contracts.Accounting.Journals;

public sealed record JournalEntryLineDto(
    Guid Id,
    Guid JournalEntryId,
    int LineNumber,
    Guid AccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description,
    Guid? CustomerId,
    Guid? SupplierId,
    Guid? CostCenterId,
    Guid? ProductVariantId,
    Guid? WarehouseId);