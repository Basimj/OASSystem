namespace OAS.Domain.Sales.Production;

public enum OpticalProductionStatus : byte
{
    Draft = 1,
    Released = 2,
    InProgress = 3,
    QualityControl = 4,
    Completed = 5,
    Cancelled = 6,
    QualityControlFailed = 7
}

public enum OpticalProductionQcResult : byte
{
    Passed = 1,
    Failed = 2
}
