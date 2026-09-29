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

public sealed class SetPrescriptionStatusCommandHandler(IPrescriptionAggregateRepository repository, IUnitOfWork unitOfWork, SalesDtoAssembler assembler)
    : IRequestHandler<SetPrescriptionStatusCommand, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(SetPrescriptionStatusCommand request, CancellationToken ct)
    {
        var entity = await repository.GetAggregateAsync(request.PrescriptionId, true, ct)
            ?? throw new NotFoundException(nameof(Prescription), request.PrescriptionId);
        SalesConcurrency.Ensure(request.Request.RowVersion, entity.RowVersion, "الوصفة");
        entity.SetStatus((PrescriptionStatus)(byte)request.Request.Status);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.PrescriptionAsync(entity, ct);
    }
}
