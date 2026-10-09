using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record RecordOpticalJobBreakageRequest(
    Guid OpticalJobLineId,
    Guid ProductVariantId,
    EyeSide? Eye,
    decimal Quantity,
    string ReasonCode,
    string? ReasonText,
    Guid? TechnicianId,
    bool RequiresReplacement,
    Guid? WarehouseId,
    string RowVersion,
    string? IdempotencyKey = null);
