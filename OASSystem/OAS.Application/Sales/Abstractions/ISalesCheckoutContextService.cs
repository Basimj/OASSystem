using OAS.Contracts.Sales.Checkout;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesCheckoutContextService
{
    Task<SalesCheckoutContextDto> GetAsync(Guid customerOrderId, CancellationToken cancellationToken = default);
}
