using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.PriceOverrides;

public sealed record SalesPriceOverrideDto(
    Guid Id,
    Guid SalesInvoiceId,
    Guid SalesInvoiceLineId,
    decimal OriginalPrice,
    decimal OverridePrice,
    string Reason,
    SalesPriceOverrideStatus Status,
    string RequestedBy,
    DateTimeOffset RequestedAtUtc,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAtUtc,
    bool IsActive,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy);
