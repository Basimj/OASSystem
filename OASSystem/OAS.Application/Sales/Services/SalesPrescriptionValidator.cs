using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

/// <summary>
/// Validates prescription references and the stored eye detail itself. Product/stock
/// fulfillment is intentionally not a prescription range concern: lens fulfillment is
/// resolved later by exact LensDetails SPH/CYL/ADD identity.
/// </summary>
public sealed class SalesPrescriptionValidator(
    IReadRepository<PrescriptionRevision, Guid> revisions,
    IReadRepository<PrescriptionEyeDetail, Guid> eyeDetails) : ISalesPrescriptionValidator
{
    public async Task<PrescriptionEyeDetail?> ValidateLineAsync(
        Guid? prescriptionRevisionId,
        EyeSide? eye,
        bool prescriptionRequired,
        CancellationToken cancellationToken = default)
    {
        if (!prescriptionRequired && !prescriptionRevisionId.HasValue && !eye.HasValue)
            return null;

        if (!prescriptionRevisionId.HasValue)
            throw new ConflictException(
                SalesErrorCodes.PrescriptionRequired,
                "يجب تحديد إصدار الوصفة لهذا السطر. إذا اخترت وصفة في رأس المستند فسيتم استخدامها تلقائيًا لأسطر العدسات.");

        if (!eye.HasValue)
            throw new ConflictException(
                SalesErrorCodes.PrescriptionEyeRequired,
                "يجب تحديد العين (OD أو OS) لسطر العدسة.");

        var revision = await revisions.GetByIdAsync(prescriptionRevisionId.Value, cancellationToken)
            ?? throw new ConflictException(SalesErrorCodes.InvalidPrescriptionRevision, "إصدار الوصفة المحدد غير موجود.");
        if (!revision.IsActive)
            throw new ConflictException(SalesErrorCodes.InvalidPrescriptionRevision, "إصدار الوصفة المحدد غير فعال.");

        var spec = new Specification<PrescriptionEyeDetail>()
            .Where(x => x.PrescriptionRevisionId == revision.Id && x.Eye == eye.Value && x.IsActive);
        return (await eyeDetails.ListAsync(spec, cancellationToken)).SingleOrDefault()
            ?? throw new ConflictException(SalesErrorCodes.InvalidPrescriptionRevision, "لا توجد قياسات فعالة للعين المحددة في إصدار الوصفة.");
    }
}
