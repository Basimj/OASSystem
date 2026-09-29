using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Application.Sales.Services;

public sealed class SalesPrescriptionValidator(
    IReadRepository<PrescriptionRevision, Guid> revisions,
    IReadRepository<PrescriptionEyeDetail, Guid> eyeDetails) : ISalesPrescriptionValidator
{
    public async Task<PrescriptionEyeDetail?> ValidateLineAsync(
        Guid? prescriptionRevisionId,
        EyeSide? eye,
        bool prescriptionRequired,
        OpticalPrescriptionRangePolicy? opticalPolicy,
        CancellationToken cancellationToken = default)
    {
        if (!prescriptionRequired && !prescriptionRevisionId.HasValue && !eye.HasValue)
            return null;

        if (!prescriptionRevisionId.HasValue || !eye.HasValue)
            throw new ConflictException(SalesErrorCodes.PrescriptionRequired, "يجب تحديد إصدار الوصفة والعين لهذا السطر.");

        var revision = await revisions.GetByIdAsync(prescriptionRevisionId.Value, cancellationToken)
            ?? throw new ConflictException(SalesErrorCodes.InvalidPrescriptionRevision, "إصدار الوصفة المحدد غير موجود.");
        if (!revision.IsActive)
            throw new ConflictException(SalesErrorCodes.InvalidPrescriptionRevision, "إصدار الوصفة المحدد غير فعال.");

        var spec = new Specification<PrescriptionEyeDetail>()
            .Where(x => x.PrescriptionRevisionId == revision.Id && x.Eye == eye.Value && x.IsActive);
        var detail = (await eyeDetails.ListAsync(spec, cancellationToken)).SingleOrDefault()
            ?? throw new ConflictException(SalesErrorCodes.InvalidPrescriptionRevision, "لا توجد قياسات فعالة للعين المحددة في إصدار الوصفة.");

        if (opticalPolicy is not null)
            detail.ValidateAgainst(opticalPolicy);

        return detail;
    }
}
