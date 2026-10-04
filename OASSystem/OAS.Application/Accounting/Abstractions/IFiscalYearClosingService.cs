using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Abstractions;

public interface IFiscalYearClosingService
{
    Task<Guid?> CloseAsync(
        FiscalYear fiscalYear,
        Guid closedBy,
        DateTime closedAtUtc,
        CancellationToken cancellationToken = default);
}
