using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesPrescriptionValidator
{
    Task<PrescriptionEyeDetail?> ValidateLineAsync(
        Guid? prescriptionRevisionId,
        EyeSide? eye,
        bool prescriptionRequired,
        OpticalPrescriptionRangePolicy? opticalPolicy,
        CancellationToken cancellationToken = default);
}
