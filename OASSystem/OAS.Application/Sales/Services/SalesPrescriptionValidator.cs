using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Exceptions;
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
        var detail = (await eyeDetails.ListAsync(spec, cancellationToken)).SingleOrDefault()
            ?? throw new ConflictException(SalesErrorCodes.InvalidPrescriptionRevision, "لا توجد قياسات فعالة للعين المحددة في إصدار الوصفة.");

        if (opticalPolicy is not null)
        {
            try
            {
                detail.ValidateAgainst(opticalPolicy);
            }
            catch (DomainException)
            {
                throw new ConflictException(
                    SalesErrorCodes.PrescriptionOutsideLensRange,
                    BuildRangeMessage(detail, opticalPolicy));
            }
        }

        return detail;
    }

    private static string BuildRangeMessage(
        PrescriptionEyeDetail detail,
        OpticalPrescriptionRangePolicy policy)
    {
        var issues = new List<string>();

        if (detail.SPH.HasValue && !policy.Sphere.Contains(detail.SPH.Value))
            issues.Add($"SPH {detail.SPH.Value:0.##} (المسموح {policy.Sphere.Minimum:0.##} إلى {policy.Sphere.Maximum:0.##})");
        if (detail.CYL.HasValue && !policy.Cylinder.Contains(detail.CYL.Value))
            issues.Add($"CYL {detail.CYL.Value:0.##} (المسموح {policy.Cylinder.Minimum:0.##} إلى {policy.Cylinder.Maximum:0.##})");
        if (detail.ADD.HasValue && !policy.Add.Contains(detail.ADD.Value))
            issues.Add($"ADD {detail.ADD.Value:0.##} (المسموح {policy.Add.Minimum:0.##} إلى {policy.Add.Maximum:0.##})");

        var eye = detail.Eye == EyeSide.RightOD ? "العين اليمنى OD" : "العين اليسرى OS";
        var detailText = issues.Count == 0 ? "القياسات خارج نطاق العدسة." : string.Join("، ", issues);
        return $"قياسات {eye} لا تتوافق مع نطاق العدسة المحددة: {detailText} اختر عدسة/SKU يدعم القياسات أو صحح نطاق العدسة في بيانات المنتج.";
    }

}
