namespace OAS.Contracts.Accounting.CostCenters;

public sealed record UpdateCostCenterRequest(
    string Code,
    string NameAr,
    string? NameEn,
    Guid? ParentCostCenterId,
    bool IsActive,
    string RowVersion);