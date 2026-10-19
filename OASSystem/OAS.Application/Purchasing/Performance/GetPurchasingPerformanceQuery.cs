using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.Performance;

namespace OAS.Application.Purchasing.Performance;

public interface IPurchasingPerformanceQueryService
{
    Task<PurchasingPerformanceReportDto> GetAsync(PurchasingPerformanceFilterDto filter, CancellationToken ct = default);
    Task<PurchasingPerformanceOrderLineDrillDownDto> GetLineDrillDownAsync(
        Guid purchaseOrderLineId, PurchasingPerformanceFilterDto filter, CancellationToken ct = default);
}

/// <summary>Requires read permissions for every data source exposed by the unified report.
/// The permissions are enforced by the application pipeline, not only the Blazor page.</summary>
public sealed record GetPurchasingPerformanceQuery(PurchasingPerformanceFilterDto Filter)
    : IQuery<PurchasingPerformanceReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        PurchasingPermissions.Orders.View,
        PurchasingPermissions.Receipts.View,
        PurchasingPermissions.Invoices.View,
        PurchasingPermissions.Catalog.View
    ];
}

public sealed class GetPurchasingPerformanceQueryHandler(IPurchasingPerformanceQueryService queries)
    : IRequestHandler<GetPurchasingPerformanceQuery, PurchasingPerformanceReportDto>
{
    public Task<PurchasingPerformanceReportDto> Handle(GetPurchasingPerformanceQuery request, CancellationToken ct)
        => queries.GetAsync(request.Filter, ct);
}


/// <summary>Uses the same server-side authorization as the main purchasing report.
/// The supplier and warehouse filters are revalidated by the SQL query.</summary>
public sealed record GetPurchasingPerformanceOrderLineDrillDownQuery(
    Guid PurchaseOrderLineId, PurchasingPerformanceFilterDto Filter)
    : IQuery<PurchasingPerformanceOrderLineDrillDownDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        PurchasingPermissions.Orders.View,
        PurchasingPermissions.Receipts.View,
        PurchasingPermissions.Invoices.View,
        PurchasingPermissions.Catalog.View
    ];
}

public sealed class GetPurchasingPerformanceOrderLineDrillDownQueryHandler(IPurchasingPerformanceQueryService queries)
    : IRequestHandler<GetPurchasingPerformanceOrderLineDrillDownQuery, PurchasingPerformanceOrderLineDrillDownDto>
{
    public Task<PurchasingPerformanceOrderLineDrillDownDto> Handle(
        GetPurchasingPerformanceOrderLineDrillDownQuery request, CancellationToken ct)
        => queries.GetLineDrillDownAsync(request.PurchaseOrderLineId, request.Filter, ct);
}
