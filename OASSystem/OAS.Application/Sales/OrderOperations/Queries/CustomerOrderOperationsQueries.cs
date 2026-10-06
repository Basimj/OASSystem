using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OrderOperations;

namespace OAS.Application.Sales.OrderOperations.Queries;

public sealed record GetCustomerOrderOperationsQuery(CustomerOrderOperationsQueryRequest Request)
    : IQuery<PagedResult<CustomerOrderOperationsItemDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OrderOperations.View];
}

public sealed record GetCustomerOrderOperationsSummaryQuery(CustomerOrderOperationsQueryRequest Request)
    : IQuery<CustomerOrderOperationsSummaryDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OrderOperations.View];
}

public sealed record GetCustomerOrderOperationsDetailsQuery(Guid CustomerOrderId)
    : IQuery<CustomerOrderOperationsDetailsDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.OrderOperations.View];
}

public sealed class GetCustomerOrderOperationsQueryHandler(ICustomerOrderOperationsQueryService queries)
    : IRequestHandler<GetCustomerOrderOperationsQuery, PagedResult<CustomerOrderOperationsItemDto>>
{
    public Task<PagedResult<CustomerOrderOperationsItemDto>> Handle(GetCustomerOrderOperationsQuery request, CancellationToken ct) =>
        queries.GetPageAsync(request.Request, ct);
}

public sealed class GetCustomerOrderOperationsSummaryQueryHandler(ICustomerOrderOperationsQueryService queries)
    : IRequestHandler<GetCustomerOrderOperationsSummaryQuery, CustomerOrderOperationsSummaryDto>
{
    public Task<CustomerOrderOperationsSummaryDto> Handle(GetCustomerOrderOperationsSummaryQuery request, CancellationToken ct) =>
        queries.GetSummaryAsync(request.Request, ct);
}

public sealed class GetCustomerOrderOperationsDetailsQueryHandler(ICustomerOrderOperationsQueryService queries)
    : IRequestHandler<GetCustomerOrderOperationsDetailsQuery, CustomerOrderOperationsDetailsDto>
{
    public Task<CustomerOrderOperationsDetailsDto> Handle(GetCustomerOrderOperationsDetailsQuery request, CancellationToken ct) =>
        queries.GetDetailsAsync(request.CustomerOrderId, ct);
}
