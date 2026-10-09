using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record CreateOpticalJobLineRequest(
    Guid CustomerOrderLineId,
    Guid? ProductVariantId,
    int LineNumber,
    OpticalJobLineType LineType,
    EyeSide? Eye,
    Guid? GroupId,
    string DescriptionSnapshot,
    decimal Quantity,
    bool RequiresProduction = true,
    string? Notes = null);
