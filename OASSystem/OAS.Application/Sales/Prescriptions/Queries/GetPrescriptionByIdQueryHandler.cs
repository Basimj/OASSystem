using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Queries;

public sealed class GetPrescriptionByIdQueryHandler(IPrescriptionAggregateRepository repository, SalesDtoAssembler assembler)
    : IRequestHandler<GetPrescriptionByIdQuery, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(GetPrescriptionByIdQuery request, CancellationToken ct)
    {
        var entity = await repository.GetAggregateAsync(request.Id, false, ct)
            ?? throw new NotFoundException(nameof(Prescription), request.Id);
        return await assembler.PrescriptionAsync(entity, ct);
    }
}
