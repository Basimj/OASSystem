using OAS.Contracts.Purchasing.Enums;

namespace OAS.Contracts.Purchasing.PurchaseRequests;

public sealed record PurchaseRequestDto(
    Guid Id,
    string RequestCode,
    PurchaseRequestType RequestType,
    PurchaseRequestStatus Status,
    Guid WarehouseId,
    string? WarehouseCode,
    string? WarehouseName,
    Guid? CustomerOrderId,
    DateOnly RequestDate,
    DateOnly? RequiredDate,
    string? Reason,
    string? Notes,
    string? RequestedBy,
    string? SubmittedBy,
    DateTimeOffset? SubmittedAt,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    string? RejectedBy,
    DateTimeOffset? RejectedAt,
    string? RejectionReason,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<PurchaseRequestLineDto> Lines,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy);
