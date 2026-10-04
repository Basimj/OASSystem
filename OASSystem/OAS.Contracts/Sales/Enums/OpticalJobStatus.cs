namespace OAS.Contracts.Sales.Enums;

public enum OpticalJobStatus : byte
{
    Approved = 1,
    AwaitingMaterials = 2,
    MaterialsAvailable = 3,
    MaterialsIssued = 4,
    InProduction = 5,
    AwaitingQC = 6,
    QCPassed = 7,
    ReadyForDelivery = 8,
    Delivered = 9,
    Cancelled = 10
}
