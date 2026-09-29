using OAS.Application.CRUD.Mapping;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Mapping;

public sealed class PrescriptionMapper : ICrudMapper<Prescription, Guid, PrescriptionDto, CreatePrescriptionRequest, UpdatePrescriptionRequest>
{
    public Prescription Create(CreatePrescriptionRequest source) => Prescription.Create(
        Guid.NewGuid(), source.PrescriptionCode, source.CustomerId, source.PrescriptionDate,
        source.PrescribedBy, source.ClinicName, source.Notes);

    public void Update(UpdatePrescriptionRequest source, Prescription destination)
    {
        SalesConcurrency.Ensure(source.RowVersion, destination.RowVersion, "الوصفة");
        destination.UpdateDetails(source.PrescriptionDate, source.PrescribedBy, source.ClinicName, source.Notes);
    }

    public PrescriptionDto ToRead(Prescription source) => SalesContractMapping.Prescription(source);
}
