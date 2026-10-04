using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Queries.CustomerPrescriptionHistory;

public sealed class GetCustomerPrescriptionHistoryQueryHandler(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Prescription, Guid> prescriptions,
    IReadRepository<PrescriptionRevision, Guid> revisions,
    IReadRepository<PrescriptionEyeDetail, Guid> eyeDetails)
    : IRequestHandler<GetCustomerPrescriptionHistoryQuery, IReadOnlyList<PrescriptionHistoryItemDto>>
{
    public async Task<IReadOnlyList<PrescriptionHistoryItemDto>> Handle(GetCustomerPrescriptionHistoryQuery request, CancellationToken ct)
    {
        if (!await customers.ExistsAsync(request.CustomerId, ct))
            throw new NotFoundException(nameof(Customer), request.CustomerId);

        var customerPrescriptions = await prescriptions.ListAsync(
            new Specification<Prescription>().Where(x => x.CustomerId == request.CustomerId), ct);
        if (customerPrescriptions.Count == 0)
            return [];

        var items = new List<(Prescription Prescription, PrescriptionRevision Revision)>();
        foreach (var prescription in customerPrescriptions)
        {
            var prescriptionRevisions = await revisions.ListAsync(
                new Specification<PrescriptionRevision>().Where(x => x.PrescriptionId == prescription.Id), ct);
            items.AddRange(prescriptionRevisions.Select(x => (prescription, x)));
        }

        var latestRevisionId = items
            .OrderByDescending(x => x.Prescription.PrescriptionDate)
            .ThenByDescending(x => x.Revision.RevisionNumber)
            .Select(x => (Guid?)x.Revision.Id)
            .FirstOrDefault();

        var result = new List<PrescriptionHistoryItemDto>(items.Count);
        foreach (var item in items
                     .OrderByDescending(x => x.Prescription.PrescriptionDate)
                     .ThenByDescending(x => x.Revision.RevisionNumber))
        {
            var eyes = await eyeDetails.ListAsync(
                new Specification<PrescriptionEyeDetail>().Where(x => x.PrescriptionRevisionId == item.Revision.Id && x.IsActive), ct);
            result.Add(new PrescriptionHistoryItemDto(
                item.Prescription.Id,
                item.Prescription.PrescriptionCode,
                item.Prescription.PrescriptionDate,
                (OAS.Contracts.Sales.Enums.PrescriptionStatus)(byte)item.Prescription.Status,
                item.Revision.Id,
                item.Revision.RevisionNumber,
                item.Revision.EffectiveDate,
                item.Prescription.PrescribedBy,
                item.Prescription.ClinicName,
                latestRevisionId == item.Revision.Id,
                item.Revision.IsCurrent,
                MapEye(eyes.FirstOrDefault(x => x.Eye == OAS.Domain.Sales.Enums.EyeSide.RightOD)),
                MapEye(eyes.FirstOrDefault(x => x.Eye == OAS.Domain.Sales.Enums.EyeSide.LeftOS))));
        }

        return result;
    }

    private static CustomerPrescriptionEyeContextDto? MapEye(PrescriptionEyeDetail? x) => x is null ? null : new(
        (OAS.Contracts.Sales.Enums.EyeSide)(byte)x.Eye,
        x.SPH, x.CYL, x.Axis, x.ADD, x.Prism,
        x.PrismBase.HasValue ? (OAS.Contracts.Sales.Enums.PrismBaseDirection?)(byte)x.PrismBase.Value : null,
        x.PD, x.MonocularPD, x.VA, x.FittingHeight);
}
