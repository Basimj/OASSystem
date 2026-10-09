namespace OAS.Domain.Sales.Enums;

public enum OpticalRemakeStatus : byte
{
    Open = 1,
    AwaitingMaterials = 2,
    Ready = 3,
    InProduction = 4,
    AwaitingQC = 5,
    Completed = 6,
    Cancelled = 7
}
