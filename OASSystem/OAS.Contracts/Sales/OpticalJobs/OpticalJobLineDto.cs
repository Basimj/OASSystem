using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalJobLineDto(
    Guid Id,
    Guid OpticalJobId,
    Guid CustomerOrderLineId,
    Guid? ProductVariantId,
    int LineNumber,
    OpticalJobLineType LineType,
    EyeSide? Eye,
    Guid? GroupId,
    string DescriptionSnapshot,
    decimal Quantity,
    bool RequiresProduction,
    string? Notes,
    string RowVersion,
    string? ProductCode = null,
    string? ProductName = null,
    CustomerOrderLineOpticalSnapshotDto? OpticalSnapshot = null);
