using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Checkout;

namespace OAS.Application.Sales.Checkout.Queries;

public sealed record GetSalesCheckoutContextQuery(Guid CustomerOrderId)
    : IQuery<SalesCheckoutContextDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Checkout.Create];
}

public sealed class GetSalesCheckoutContextQueryHandler(ISalesCheckoutContextService service)
    : IRequestHandler<GetSalesCheckoutContextQuery, SalesCheckoutContextDto>
{
    public Task<SalesCheckoutContextDto> Handle(GetSalesCheckoutContextQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.CustomerOrderId, cancellationToken);
}
