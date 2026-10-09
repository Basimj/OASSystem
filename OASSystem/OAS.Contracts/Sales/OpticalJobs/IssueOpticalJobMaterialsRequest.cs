namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record IssueOpticalJobMaterialsRequest(
    Guid WarehouseId,
    IReadOnlyList<IssueOpticalJobMaterialLineRequest> Lines,
    string RowVersion,
    Guid? RequestId = null);

public sealed record IssueOpticalJobMaterialLineRequest(Guid ProductVariantId, decimal Quantity);
