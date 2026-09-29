using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Prescriptions.Commands;

public sealed class CreatePrescriptionRevisionCommandHandler(
    IPrescriptionAggregateRepository repository,
    IRepository<PrescriptionRevision, Guid> revisions,
    IRepository<PrescriptionEyeDetail, Guid> eyeDetails,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler)
    : IRequestHandler<CreatePrescriptionRevisionCommand, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(
        CreatePrescriptionRevisionCommand request,
        CancellationToken ct)
    {
        var prescription = await repository.GetAggregateAsync(
            request.PrescriptionId,
            tracking: true,
            ct)
            ?? throw new NotFoundException(
                nameof(Prescription),
                request.PrescriptionId);

        SalesConcurrency.Ensure(
            request.Request.PrescriptionRowVersion,
            prescription.RowVersion,
            "الوصفة");

        if (request.Request.EyeDetails is null || request.Request.EyeDetails.Count == 0)
        {
            throw new ConflictException(
                SalesErrorCodes.PrescriptionEyeRequired,
                "يجب إضافة قياس عين واحد على الأقل.");
        }

        if (request.Request.EyeDetails
            .GroupBy(x => x.Eye)
            .Any(group => group.Count() > 1))
        {
            throw new ConflictException(
                SalesErrorCodes.PrescriptionEyeDuplicate,
                "لا يمكن تكرار نفس العين في الإصدار.");
        }

        var revision = prescription.AddRevision(
            Guid.NewGuid(),
            request.Request.EffectiveDate,
            request.Request.Reason);

        await revisions.AddAsync(revision, ct);

        foreach (var eye in request.Request.EyeDetails)
        {
            var detail = revision.SetEyeDetail(
                Guid.NewGuid(),
                (EyeSide)(byte)eye.Eye,
                eye.SPH,
                eye.CYL,
                eye.Axis,
                eye.ADD,
                eye.Prism,
                eye.PrismBase.HasValue
                    ? (PrismBaseDirection?)(byte)eye.PrismBase.Value
                    : null,
                eye.PD,
                eye.MonocularPD,
                eye.VA,
                eye.FittingHeight,
                eye.Notes);

            await eyeDetails.AddAsync(detail, ct);
        }

        // دعم أي بيانات Draft قديمة موجودة قبل اعتماد السير الجديد.
        // أول Revision مكتمل يفعّل الوصفة تلقائيًا.
        if (prescription.Status == PrescriptionStatus.Draft)
            prescription.SetStatus(PrescriptionStatus.Active);

        await unitOfWork.SaveChangesAsync(ct);

        return await assembler.PrescriptionAsync(prescription, ct);
    }
}
