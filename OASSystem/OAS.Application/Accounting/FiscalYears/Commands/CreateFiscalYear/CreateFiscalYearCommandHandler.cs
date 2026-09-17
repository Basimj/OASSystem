using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.FiscalYears.Mapping;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Commands.CreateFiscalYear;

public sealed class CreateFiscalYearCommandHandler(
    IRepository<FiscalYear, Guid> repository,
    FiscalYearMapper mapper)
    : IRequestHandler<CreateFiscalYearCommand, FiscalYear>
{
    public async Task<FiscalYear> Handle(
        CreateFiscalYearCommand request,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
