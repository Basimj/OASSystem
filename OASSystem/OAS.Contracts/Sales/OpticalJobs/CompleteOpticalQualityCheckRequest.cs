using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OpticalJobs;

public sealed record CompleteOpticalQualityCheckRequest(
    string RowVersion,
    IReadOnlyList<OpticalQualityCheckItemRequest> Items,
    OpticalQcFailureAction? FailureAction = null,
    string? Reason = null,
    string? Notes = null,
    Guid? RemakeLineId = null,
    decimal RemakeQuantity = 1m,
    string? QualityCheckRowVersion = null);

public sealed record OpticalQualityCheckItemRequest(
    string CheckCode,
    OpticalQualityCheckItemResult Result,
    string? Notes = null);
