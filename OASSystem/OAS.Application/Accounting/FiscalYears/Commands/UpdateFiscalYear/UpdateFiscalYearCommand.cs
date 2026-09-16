using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.FiscalYears.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Commands.UpdateFiscalYear;

public sealed record UpdateFiscalYearCommand(
    Guid Id,
    UpdateFiscalYearRequest Data)
    : UpdateEntityCommand<
        FiscalYear,
        Guid,
        UpdateFiscalYearRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [FiscalYearPermissions.Edit];
}