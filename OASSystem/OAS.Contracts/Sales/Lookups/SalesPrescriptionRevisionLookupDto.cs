namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesPrescriptionRevisionLookupDto(
    Guid Id,
    Guid PrescriptionId,
    string PrescriptionCode,
    int RevisionNumber,
    DateOnly EffectiveDate,
    bool IsCurrent,
    bool IsActive);
