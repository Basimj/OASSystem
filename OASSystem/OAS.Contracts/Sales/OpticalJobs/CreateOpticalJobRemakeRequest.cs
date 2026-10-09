namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record CreateOpticalJobRemakeRequest(
    Guid OpticalJobLineId,
    Guid? SourceQualityCheckId,
    Guid? SourceBreakageId,
    Guid? ProductVariantId,
    decimal Quantity,
    string Reason,
    Guid? WarehouseId,
    string RowVersion);
