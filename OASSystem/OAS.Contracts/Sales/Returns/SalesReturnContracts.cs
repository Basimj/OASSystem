using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Returns;

public enum SalesReturnStatus : byte
{
    Draft = 1,
    Confirmed = 2,
    Posted = 3,
    Cancelled = 4
}

public sealed record CreateSalesReturnLineRequest(
    Guid SalesInvoiceLineId,
    decimal Quantity,
    Guid? ReturnWarehouseId = null,
    SalesReturnCondition? ReturnCondition = null,
    SalesReturnStockDisposition? StockDisposition = null);

public sealed record CreateSalesReturnRequest(
    Guid SalesInvoiceId,
    DateOnly ReturnDate,
    DateOnly PostingDate,
    string? Reason,
    IReadOnlyList<CreateSalesReturnLineRequest> Lines,
    bool OverrideReturnWindow = false,
    string? ReturnWindowOverrideReason = null);

public sealed record SalesReturnActionRequest(string RowVersion);
public sealed record CancelSalesReturnRequest(string RowVersion, string? Reason);
public sealed record InspectSalesReturnLineRequest(
    string RowVersion,
    SalesReturnCondition Condition,
    SalesReturnStockDisposition Disposition,
    Guid? DestinationWarehouseId = null);

public sealed record SalesReturnLineDto(
    Guid Id,
    int LineNumber,
    Guid SalesInvoiceLineId,
    SalesLineType LineType,
    Guid? ProductVariantId,
    Guid? WarehouseId,
    string? ProductCode,
    string ProductName,
    decimal Quantity,
    decimal NetAmount,
    decimal TaxAmount,
    decimal FinalAmount,
    decimal BaseNetAmount,
    decimal BaseTaxAmount,
    decimal BaseFinalAmount,
    decimal? UnitCostSnapshot,
    decimal? TotalCostSnapshot,
    SalesReturnCondition? ReturnCondition,
    SalesReturnStockDisposition? StockDisposition,
    DateTimeOffset? InspectedAtUtc,
    string? InspectedBy,
    string RowVersion);

public sealed record SalesReturnDto(
    Guid Id,
    string ReturnCode,
    Guid SalesInvoiceId,
    Guid CustomerId,
    DateOnly ReturnDate,
    DateOnly PostingDate,
    SalesReturnStatus Status,
    Guid CurrencyId,
    string CurrencyCode,
    byte CurrencyDecimalPlaces,
    decimal ExchangeRate,
    Guid BaseCurrencyId,
    string BaseCurrencyCode,
    byte BaseCurrencyDecimalPlaces,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal BaseNetAmount,
    decimal BaseTaxAmount,
    decimal BaseTotalAmount,
    string? Reason,
    DateOnly? ReturnPolicyReferenceDate,
    int? ReturnWindowDaysSnapshot,
    bool ReturnWindowOverridden,
    string? ReturnWindowOverrideReason,
    string? ReturnWindowOverriddenBy,
    Guid? JournalEntryId,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? PostedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string RowVersion,
    IReadOnlyList<SalesReturnLineDto> Lines);

public sealed record SalesReturnPostingResultDto(
    Guid SalesReturnId,
    Guid JournalEntryId,
    IReadOnlyList<Guid> InventoryTransactionIds);
