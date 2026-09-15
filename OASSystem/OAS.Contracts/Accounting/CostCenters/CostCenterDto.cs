namespace OAS.Contracts.Accounting.CostCenters;

public sealed record CostCenterDto(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    Guid? ParentCostCenterId,
    bool IsActive,
    string RowVersion);