using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Contracts.Common.Pagination;

namespace OAS.Infrastructure.Purchasing.Persistence.Repositories;

internal static class PurchasingRepositoryHelpers
{
    public static async Task<PagedData<T>> PageAsync<T>(
        IQueryable<T> query,
        PageRequest request,
        Func<IQueryable<T>, string, IQueryable<T>>? search = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = request.Normalize();
        if (normalized.Search is { Length: > 0 } term && search is not null)
            query = search(query, term);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedData<T>(items, total);
    }
}
