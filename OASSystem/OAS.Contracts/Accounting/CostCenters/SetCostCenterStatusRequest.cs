namespace OAS.Contracts.Accounting.CostCenters;

public sealed record SetCostCenterStatusRequest(
    bool IsActive,
    string RowVersion);