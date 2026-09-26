using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.PaymentAllocations;
public sealed record PaymentAllocationDto(
    Guid Id,
    Guid? ReceiptVoucherLineId,
    Guid? PaymentVoucherLineId,
    PaymentSourceType PaymentSourceType,
    Guid PaymentSourceId,
    AllocationTargetDocumentType TargetDocumentType,
    Guid TargetDocumentId,
    Guid? CurrencyId,
    string? CurrencyCodeSnapshot,
    decimal AllocatedAmount,
    decimal? ExchangeRate,
    decimal? BaseAllocatedAmount,
    DateTimeOffset AllocatedAtUtc,
    string? CreatedBy);
