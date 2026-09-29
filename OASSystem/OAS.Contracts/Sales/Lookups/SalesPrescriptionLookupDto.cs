using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Lookups;

public sealed record SalesPrescriptionLookupDto(
    Guid Id,
    string PrescriptionCode,
    Guid CustomerId,
    DateOnly PrescriptionDate,
    PrescriptionStatus Status,
    Guid? CurrentRevisionId,
    int? CurrentRevisionNumber,
    bool IsActive);
