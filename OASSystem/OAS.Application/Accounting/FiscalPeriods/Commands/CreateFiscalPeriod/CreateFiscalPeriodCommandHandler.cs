using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.FiscalPeriods.Mapping;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.CreateFiscalPeriod;

public sealed class CreateFiscalPeriodCommandHandler(
    IRepository<FiscalPeriod, Guid> repository,
    FiscalPeriodMapper mapper)
    : IRequestHandler<CreateFiscalPeriodCommand, FiscalPeriod>
{
    public async Task<FiscalPeriod> Handle(
        CreateFiscalPeriodCommand request,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}