namespace OAS.Contracts.Sales.Production;

public enum OpticalProductionStatus : byte
{
    Draft = 1,
    Released = 2,
    InProgress = 3,
    QualityControl = 4,
    Completed = 5,
    Cancelled = 6,
    QualityControlFailed = 7
}

public enum OpticalProductionQcResult : byte
{
    Passed = 1,
    Failed = 2
}

public sealed record CreateOpticalProductionMaterialRequest(Guid ProductVariantId, decimal Quantity, string? Notes = null);

public sealed record CreateOpticalProductionJobRequest(
    Guid SalesInvoiceLineId,
    Guid WarehouseId,
    DateOnly JobDate,
    DateOnly? TargetDate,
    string? Notes,
    IReadOnlyList<CreateOpticalProductionMaterialRequest> Materials);

public sealed record OpticalProductionActionRequest(string RowVersion);

public sealed record SubmitOpticalProductionQcRequest(
    string RowVersion,
    bool Passed,
    string? Notes = null,
    bool IsBreakage = false);

public sealed record CreateOpticalProductionRemakeRequest(
    string RowVersion,
    DateOnly? TargetDate = null,
    string? Notes = null);

public sealed record OpticalProductionMaterialDto(
    Guid Id,
    Guid ProductVariantId,
    decimal Quantity,
    decimal? UnitCostSnapshot,
    decimal? TotalCostSnapshot,
    string? Notes);

public sealed record OpticalProductionJobDto(
    Guid Id,
    string JobCode,
    Guid SalesInvoiceId,
    Guid SalesInvoiceLineId,
    Guid CustomerId,
    Guid WarehouseId,
    DateOnly JobDate,
    DateOnly? TargetDate,
    OpticalProductionStatus Status,
    Guid? InventoryTransactionId,
    decimal MaterialCostBase,
    string? Notes,
    Guid? RemakeOfJobId,
    int RemakeNumber,
    OpticalProductionQcResult? LastQcResult,
    int QcAttemptCount,
    int FailedQcCount,
    string? LastQcNotes,
    bool LastQcWasBreakage,
    DateTimeOffset? ReleasedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? QcAtUtc,
    DateTimeOffset? FailedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RowVersion,
    IReadOnlyList<OpticalProductionMaterialDto> Materials);
