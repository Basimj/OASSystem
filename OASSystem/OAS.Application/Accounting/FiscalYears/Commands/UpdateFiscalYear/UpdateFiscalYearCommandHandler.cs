using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.FiscalYears.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Commands.UpdateFiscalYear;

public sealed class UpdateFiscalYearCommandHandler(
    IRepository<FiscalYear, Guid> repository,
    FiscalYearMapper mapper)
    : IRequestHandler<UpdateFiscalYearCommand, FiscalYear>
{
    public async Task<FiscalYear> Handle(
        UpdateFiscalYearCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(FiscalYear), request.Id);
        }

        if (!string.Equals(entity.Code, request.Data.Code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                "accounting_fiscal_year_code_immutable",
                "The fiscal year code cannot be changed after creation.");
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The fiscal year has been modified by another user.");
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
