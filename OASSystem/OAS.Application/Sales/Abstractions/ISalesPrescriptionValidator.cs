using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesPrescriptionValidator
{
    Task<PrescriptionEyeDetail?> ValidateLineAsync(
        Guid? prescriptionRevisionId,
        EyeSide? eye,
        bool prescriptionRequired,
        CancellationToken cancellationToken = default);
}
