using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface IOpticalProductionPort
{
    Task<Guid?> EnsureJobAsync(
        CustomerOrder order,
        Guid? salesInvoiceId,
        CancellationToken cancellationToken = default);
}
