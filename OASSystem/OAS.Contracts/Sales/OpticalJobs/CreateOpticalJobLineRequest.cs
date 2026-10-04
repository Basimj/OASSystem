using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record CreateOpticalJobLineRequest(
    Guid CustomerOrderLineId,
    Guid? ProductVariantId,
    int LineNumber,
    SalesLineType LineType,
    EyeSide? Eye,
    string DescriptionSnapshot,
    decimal Quantity,
    string? Notes = null);
