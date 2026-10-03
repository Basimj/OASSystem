using Microsoft.EntityFrameworkCore;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Features.Employees.Services;

public sealed class HRAccountingReferencePort(OasDbContext dbContext) : IHRAccountingReferencePort
{
    public Task<HRCurrencyReference?> GetCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default) =>
        dbContext.Currencies.AsNoTracking().Where(x => x.Id == currencyId).Select(x => new HRCurrencyReference(x.Id, x.Code, x.Symbol, x.DecimalPlaces, x.IsActive)).SingleOrDefaultAsync(cancellationToken);
}
