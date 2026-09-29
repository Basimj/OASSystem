using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.Prescriptions;

public sealed record PrescriptionEyeDetailRequest(
    EyeSide Eye,
    decimal? SPH,
    decimal? CYL,
    short? Axis,
    decimal? ADD,
    decimal? Prism,
    PrismBaseDirection? PrismBase,
    decimal? PD,
    decimal? MonocularPD,
    string? VA,
    decimal? FittingHeight,
    string? Notes);
