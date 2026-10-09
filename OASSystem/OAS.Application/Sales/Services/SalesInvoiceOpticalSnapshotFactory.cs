using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Services;

public static class SalesInvoiceOpticalSnapshotFactory
{
    public static SalesInvoiceLinePrescriptionSnapshot Create(Guid salesInvoiceLineId, OpticalSnapshotDraft draft) =>
        SalesInvoiceLinePrescriptionSnapshot.Create(
            Guid.NewGuid(),
            salesInvoiceLineId,
            draft.PrescriptionRevisionId,
            draft.Eye,
            draft.SPH,
            draft.CYL,
            draft.Axis,
            draft.ADD,
            draft.Prism,
            draft.PrismBase,
            draft.PD,
            draft.MonocularPD,
            draft.VA,
            draft.FittingHeight);
}
