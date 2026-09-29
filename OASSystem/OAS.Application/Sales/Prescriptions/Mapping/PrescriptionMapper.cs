using OAS.Application.CRUD.Mapping;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Prescriptions.Mapping;

public sealed class PrescriptionMapper
    : ICrudMapper<
        Prescription,
        Guid,
        PrescriptionDto,
        CreatePrescriptionRequest,
        UpdatePrescriptionRequest>
{
    public Prescription Create(CreatePrescriptionRequest source)
    {
        var prescription = Prescription.Create(
            Guid.NewGuid(),
            source.PrescriptionCode,
            source.CustomerId,
            source.PrescriptionDate,
            source.PrescribedBy,
            source.ClinicName,
            source.Notes);

        var revision = prescription.AddRevision(
            Guid.NewGuid(),
            source.InitialRevision.EffectiveDate,
            source.InitialRevision.Reason);

        foreach (var eye in source.InitialRevision.EyeDetails)
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

        prescription.SetStatus(PrescriptionStatus.Active);
        return prescription;
    }

    public void Update(
        UpdatePrescriptionRequest source,
        Prescription destination)
    {
        SalesConcurrency.Ensure(
            source.RowVersion,
            destination.RowVersion,
            "الوصفة");

        destination.UpdateDetails(
            source.PrescriptionDate,
            source.PrescribedBy,
            source.ClinicName,
            source.Notes);
    }

    public PrescriptionDto ToRead(Prescription source) =>
        SalesContractMapping.Prescription(source);
}
