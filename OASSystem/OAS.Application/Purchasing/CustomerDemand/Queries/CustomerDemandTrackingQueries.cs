using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.CustomerDemand;

namespace OAS.Application.Purchasing.CustomerDemand.Queries;

public sealed record GetCustomerDemandTrackingQuery(CustomerDemandTrackingQueryRequest Request)
    : IQuery<PagedResult<CustomerDemandTrackingItemDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.CustomerDemand.View];
}

public sealed record GetCustomerDemandTrackingSummaryQuery(CustomerDemandTrackingQueryRequest Request)
    : IQuery<CustomerDemandTrackingSummaryDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.CustomerDemand.View];
}

public sealed record GetCustomerDemandTrackingDetailsQuery(Guid PurchaseRequestLineId)
    : IQuery<CustomerDemandTrackingDetailsDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.CustomerDemand.View];
}

public sealed record GetSupplierPurchaseHistoryQuery(SupplierPurchaseHistoryQueryRequest Request)
    : IQuery<PagedResult<SupplierPurchaseHistoryItemDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.CustomerDemand.View];
}

public sealed class GetCustomerDemandTrackingQueryHandler(
    ICustomerDemandTrackingQueryService queries,
    IPermissionChecker permissions)
    : IRequestHandler<GetCustomerDemandTrackingQuery, PagedResult<CustomerDemandTrackingItemDto>>
{
    public async Task<PagedResult<CustomerDemandTrackingItemDto>> Handle(GetCustomerDemandTrackingQuery request, CancellationToken ct) =>
        await queries.GetPageAsync(request.Request,
            await permissions.HasPermissionAsync(PurchasingPermissions.CustomerDemand.ViewCosts, ct), ct);
}

public sealed class GetCustomerDemandTrackingSummaryQueryHandler(ICustomerDemandTrackingQueryService queries)
    : IRequestHandler<GetCustomerDemandTrackingSummaryQuery, CustomerDemandTrackingSummaryDto>
{
    public Task<CustomerDemandTrackingSummaryDto> Handle(GetCustomerDemandTrackingSummaryQuery request, CancellationToken ct) =>
        queries.GetSummaryAsync(request.Request, ct);
}

public sealed class GetCustomerDemandTrackingDetailsQueryHandler(
    ICustomerDemandTrackingQueryService queries,
    IPermissionChecker permissions)
    : IRequestHandler<GetCustomerDemandTrackingDetailsQuery, CustomerDemandTrackingDetailsDto>
{
    public async Task<CustomerDemandTrackingDetailsDto> Handle(GetCustomerDemandTrackingDetailsQuery request, CancellationToken ct) =>
        await queries.GetDetailsAsync(request.PurchaseRequestLineId,
            await permissions.HasPermissionAsync(PurchasingPermissions.CustomerDemand.ViewCosts, ct), ct);
}

public sealed class GetSupplierPurchaseHistoryQueryHandler(
    ICustomerDemandTrackingQueryService queries,
    IPermissionChecker permissions)
    : IRequestHandler<GetSupplierPurchaseHistoryQuery, PagedResult<SupplierPurchaseHistoryItemDto>>
{
    public async Task<PagedResult<SupplierPurchaseHistoryItemDto>> Handle(GetSupplierPurchaseHistoryQuery request, CancellationToken ct) =>
        await queries.GetSupplierHistoryAsync(request.Request,
            await permissions.HasPermissionAsync(PurchasingPermissions.CustomerDemand.ViewCosts, ct), ct);
}
