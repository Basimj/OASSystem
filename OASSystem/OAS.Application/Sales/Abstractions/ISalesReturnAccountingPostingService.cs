using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesReturnAccountingPostingService
{
    Task<Guid> PostAsync(
        SalesReturn salesReturn,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default);
}
