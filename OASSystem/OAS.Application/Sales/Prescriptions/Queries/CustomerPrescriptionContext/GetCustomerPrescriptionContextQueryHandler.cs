using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Queries.CustomerPrescriptionContext;

public sealed class GetCustomerPrescriptionContextQueryHandler(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Prescription, Guid> prescriptions,
    IReadRepository<PrescriptionRevision, Guid> revisions,
    IReadRepository<PrescriptionEyeDetail, Guid> eyeDetails)
    : IRequestHandler<GetCustomerPrescriptionContextQuery, CustomerPrescriptionContextDto>
{
    public async Task<CustomerPrescriptionContextDto> Handle(GetCustomerPrescriptionContextQuery request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var customerPrescriptions = await prescriptions.ListAsync(
            new Specification<Prescription>().Where(x => x.CustomerId == request.CustomerId && x.IsActive), ct);
        var latestPrescription = customerPrescriptions
            .OrderByDescending(x => x.PrescriptionDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

        if (latestPrescription is null)
            return new CustomerPrescriptionContextDto(
                customer.Id, customer.CustomerCode, customer.NameAr, customer.ContactInfo.Mobile,
                false, null, null, null, null, null, null, null);

        var prescriptionRevisions = await revisions.ListAsync(
            new Specification<PrescriptionRevision>().Where(x => x.PrescriptionId == latestPrescription.Id && x.IsActive), ct);
        var latestRevision = prescriptionRevisions
            .OrderByDescending(x => x.IsCurrent)
            .ThenByDescending(x => x.RevisionNumber)
            .FirstOrDefault();

        CustomerPrescriptionEyeContextDto? od = null;
        CustomerPrescriptionEyeContextDto? os = null;
        if (latestRevision is not null)
        {
            var eyes = await eyeDetails.ListAsync(
                new Specification<PrescriptionEyeDetail>().Where(x => x.PrescriptionRevisionId == latestRevision.Id && x.IsActive), ct);
            od = MapEye(eyes.FirstOrDefault(x => x.Eye == OAS.Domain.Sales.Enums.EyeSide.RightOD));
            os = MapEye(eyes.FirstOrDefault(x => x.Eye == OAS.Domain.Sales.Enums.EyeSide.LeftOS));
        }

        return new CustomerPrescriptionContextDto(
            customer.Id,
            customer.CustomerCode,
            customer.NameAr,
            customer.ContactInfo.Mobile,
            true,
            latestPrescription.Id,
            latestPrescription.PrescriptionCode,
            latestRevision?.Id,
            latestRevision?.RevisionNumber,
            latestPrescription.PrescriptionDate,
            od,
            os);
    }

    private static CustomerPrescriptionEyeContextDto? MapEye(PrescriptionEyeDetail? x) => x is null ? null : new(
        (OAS.Contracts.Sales.Enums.EyeSide)(byte)x.Eye,
        x.SPH, x.CYL, x.Axis, x.ADD, x.Prism,
        x.PrismBase.HasValue ? (OAS.Contracts.Sales.Enums.PrismBaseDirection?)(byte)x.PrismBase.Value : null,
        x.PD, x.MonocularPD, x.VA, x.FittingHeight);
}
