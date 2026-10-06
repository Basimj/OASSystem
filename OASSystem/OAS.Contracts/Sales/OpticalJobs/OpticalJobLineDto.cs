using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record OpticalJobLineDto(
    Guid Id,
    Guid OpticalJobId,
    Guid CustomerOrderLineId,
    Guid? ProductVariantId,
    int LineNumber,
    SalesLineType LineType,
    EyeSide? Eye,
    string DescriptionSnapshot,
    decimal Quantity,
    string? Notes,
    string RowVersion,
    string? ProductCode = null,
    string? ProductName = null,
    CustomerOrderLineOpticalSnapshotDto? OpticalSnapshot = null);
