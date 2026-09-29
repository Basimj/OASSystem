using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Commands;

public sealed class UpdatePrescriptionCommandHandler(IPrescriptionAggregateRepository repository)
    : IRequestHandler<UpdatePrescriptionCommand, Prescription>
{
    public async Task<Prescription> Handle(UpdatePrescriptionCommand request, CancellationToken ct)
    {
        var entity = await repository.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(Prescription), request.Id);

        SalesConcurrency.Ensure(request.Data.RowVersion, entity.RowVersion, "الوصفة");
        entity.UpdateDetails(
            request.Data.PrescriptionDate,
            request.Data.PrescribedBy,
            request.Data.ClinicName,
            request.Data.Notes);
        repository.Update(entity);
        return entity;
    }
}
