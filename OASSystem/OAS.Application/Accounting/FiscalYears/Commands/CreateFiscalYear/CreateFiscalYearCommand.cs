using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalYears.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Commands.CreateFiscalYear;

public sealed record CreateFiscalYearCommand(
    CreateFiscalYearRequest Data)
    : CreateEntityCommand<
        FiscalYear,
        Guid,
        CreateFiscalYearRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalYearPermissions.Create];
}