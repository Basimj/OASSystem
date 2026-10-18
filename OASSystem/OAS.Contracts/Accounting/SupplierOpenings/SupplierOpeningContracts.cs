namespace OAS.Contracts.Accounting.SupplierOpenings;

public sealed record CreateSupplierOpeningBatchRequest(
    DateOnly AsOfDate,
    Guid BaseCurrencyId,
    string? Notes,
    IReadOnlyList<CreateSupplierOpeningItemRequest> Items);

public sealed record CreateSupplierOpeningItemRequest(
    Guid SupplierId,
    string? LegacyReference,
    DateOnly DocumentDate,
    DateOnly? DueDate,
    Guid CurrencyId,
    decimal OriginalOpenAmount,
    decimal ExchangeRateSnapshot);

public sealed record PostSupplierOpeningBatchRequest(string RowVersion);
public sealed record LinkSupplierOpeningJournalRequest(Guid JournalEntryId, string RowVersion);

public sealed record ApplySupplierOpeningRequest(
    Guid PaymentVoucherLineId,
    decimal AmountBase,
    string? IdempotencyKey = null);

public sealed record ReverseSupplierOpeningApplicationRequest(
    string Reason,
    string RowVersion);

public sealed record SupplierOpeningItemDto(
    Guid Id,
    Guid SupplierId,
    string? SupplierCode,
    string? SupplierName,
    string? LegacyReference,
    DateOnly DocumentDate,
    DateOnly? DueDate,
    byte DueDateProvenance,
    Guid CurrencyId,
    string CurrencyCode,
    byte CurrencyDecimalPlaces,
    decimal OriginalOpenAmount,
    decimal ExchangeRateSnapshot,
    decimal BaseOpenAmount,
    decimal AppliedBaseAmount,
    decimal OutstandingBaseAmount,
    string RowVersion);

public sealed record SupplierOpeningBatchDto(
    Guid Id,
    string BatchCode,
    DateOnly AsOfDate,
    Guid BaseCurrencyId,
    string? BaseCurrencyCode,
    byte Status,
    Guid? JournalEntryId,
    string? Notes,
    string? PostedBy,
    DateTime? PostedAtUtc,
    IReadOnlyList<SupplierOpeningItemDto> Items,
    decimal TotalBaseOpenAmount,
    decimal TotalOutstandingBaseAmount,
    string RowVersion);

public sealed record SupplierOpeningApplicationDto(
    Guid Id,
    Guid SupplierOpeningItemId,
    Guid PaymentVoucherLineId,
    Guid SupplierId,
    decimal AppliedSourceBaseAmount,
    decimal AppliedTargetBaseAmount,
    DateTime AppliedAtUtc,
    string AppliedBy,
    byte Status,
    string? IdempotencyKey,
    DateTime? ReversedAtUtc,
    string? ReversedBy,
    string? ReversalReason,
    string RowVersion);
