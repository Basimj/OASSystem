using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Prescriptions.Commands;

public sealed class CreatePrescriptionCommandHandler(
    IPrescriptionAggregateRepository repository,
    IReadRepository<Customer, Guid> customers,
    ISequenceNumberGenerator sequences)
    : IRequestHandler<CreatePrescriptionCommand, Prescription>
{
    public async Task<Prescription> Handle(
        CreatePrescriptionCommand request,
        CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.Data.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.Data.CustomerId);

        if (!customer.IsActive)
            throw new ConflictException(
                SalesErrorCodes.CustomerInactive,
                "العميل غير فعال.");

        var initialRevision = request.Data.InitialRevision
            ?? throw new ConflictException(
                SalesErrorCodes.PrescriptionRevisionRequired,
                "لا يمكن حفظ الوصفة بدون الإصدار الأول.");

        if (initialRevision.EyeDetails is null || initialRevision.EyeDetails.Count == 0)
            throw new ConflictException(
                SalesErrorCodes.PrescriptionEyeRequired,
                "يجب إضافة قياس عين واحد على الأقل للإصدار الأول.");

        if (initialRevision.EyeDetails
            .GroupBy(x => x.Eye)
            .Any(group => group.Count() > 1))
        {
            throw new ConflictException(
                SalesErrorCodes.PrescriptionEyeDuplicate,
                "لا يمكن تكرار نفس العين داخل الإصدار.");
        }

        var code = request.Data.PrescriptionCode?.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            code = PrescriptionCodeFormatter.Format(
                await sequences.NextAsync("PrescriptionCodeSequence", ct));
        }

        var duplicated = await repository.CountAsync(
            new Specification<Prescription>()
                .Where(x => x.PrescriptionCode == code),
            ct);

        if (duplicated > 0)
        {
            throw new ConflictException(
                SalesErrorCodes.DuplicatePrescriptionCode,
                "كود الوصفة مستخدم مسبقًا.");
        }

        var prescription = Prescription.Create(
            Guid.NewGuid(),
            code,
            request.Data.CustomerId,
            request.Data.PrescriptionDate,
            request.Data.PrescribedBy,
            request.Data.ClinicName,
            request.Data.Notes);

        var revision = prescription.AddRevision(
            Guid.NewGuid(),
            initialRevision.EffectiveDate,
            initialRevision.Reason);

        foreach (var eye in initialRevision.EyeDetails)
        {
            revision.SetEyeDetail(
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
        }

        // قاعدة العمل الجديدة:
        // لا تُحفظ وصفة جديدة كـ Draft.
        // بعد وجود الإصدار الأول وبيانات العين تصبح الوصفة Active مباشرة.
        prescription.SetStatus(PrescriptionStatus.Active);

        // إضافة الـAggregate root تكفي؛ EF يتتبع Revisions وEyeDetails عبر navigation graph.
        // TransactionBehavior سيحفظ الجميع ويعمل Commit كوحدة واحدة.
        await repository.AddAsync(prescription, ct);

        return prescription;
    }
}
