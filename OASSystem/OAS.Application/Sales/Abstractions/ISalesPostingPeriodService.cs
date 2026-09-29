using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesPostingPeriodService
{
    Task<FiscalPeriod> GetOpenPostingPeriodAsync(
        DateOnly postingDate,
        bool requiresInventory,
        CancellationToken cancellationToken = default);
}
