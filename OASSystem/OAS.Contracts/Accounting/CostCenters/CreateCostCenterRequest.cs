namespace OAS.Contracts.Accounting.CostCenters;

public sealed record CreateCostCenterRequest(
    string Code,
    string NameAr,
    string? NameEn,
    Guid? ParentCostCenterId,
    bool IsActive = true);