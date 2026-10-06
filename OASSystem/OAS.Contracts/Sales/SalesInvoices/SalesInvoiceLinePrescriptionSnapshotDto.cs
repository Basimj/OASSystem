using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record SalesInvoiceLinePrescriptionSnapshotDto(
    Guid Id,
    Guid SalesInvoiceLineId,
    Guid? PrescriptionRevisionId,
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
    bool IsActive,
    string RowVersion);
