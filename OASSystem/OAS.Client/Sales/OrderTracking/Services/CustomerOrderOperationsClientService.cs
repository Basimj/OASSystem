using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.OrderOperations;

namespace OAS.Client.Sales.OrderTracking.Services;

public sealed class CustomerOrderOperationsClientService(OasApiClient api) : ICustomerOrderOperationsClientService
{
    public async Task<PagedResult<CustomerOrderOperationsItemDto>> GetPageAsync(CustomerOrderOperationsQueryRequest r, CancellationToken ct = default)
        => await api.GetAsync<PagedResult<CustomerOrderOperationsItemDto>>(Build("api/sales/order-operations", r), ct) ?? new();

    public Task<CustomerOrderOperationsSummaryDto?> GetSummaryAsync(CustomerOrderOperationsQueryRequest r, CancellationToken ct = default)
        => api.GetAsync<CustomerOrderOperationsSummaryDto>(Build("api/sales/order-operations/summary", r with { PageNumber = 1, PageSize = 1 }), ct);

    public Task<CustomerOrderOperationsDetailsDto?> GetDetailsAsync(Guid orderId, CancellationToken ct = default)
        => api.GetAsync<CustomerOrderOperationsDetailsDto>($"api/sales/order-operations/{orderId:D}", ct);

    private static string Build(string route, CustomerOrderOperationsQueryRequest r)
    {
        var q = new List<string>
        {
            $"pageNumber={Math.Max(1,r.PageNumber)}",
            $"pageSize={Math.Clamp(r.PageSize,1,PageRequest.MaximumPageSize)}"
        };
        Add(q,"search",r.Search); Add(q,"orderDateFrom",r.OrderDateFrom?.ToString("yyyy-MM-dd")); Add(q,"orderDateTo",r.OrderDateTo?.ToString("yyyy-MM-dd"));
        Add(q,"status",r.Status?.ToString()); Add(q,"productType",r.ProductType?.ToString()); Add(q,"warehouseId",r.WarehouseId?.ToString());
        Add(q,"requiresProduction",r.RequiresProduction?.ToString().ToLowerInvariant()); Add(q,"availability",r.Availability?.ToString());
        Add(q,"requiredDate",r.RequiredDate?.ToString("yyyy-MM-dd")); if(r.OverdueOnly) Add(q,"overdueOnly","true"); Add(q,"technicianId",r.TechnicianId?.ToString());
        Add(q,"sortBy",r.SortBy); Add(q,"sortDirection",r.SortDirection.ToString());
        return route + "?" + string.Join("&", q);
    }
    private static void Add(List<string> q,string key,string? value){ if(!string.IsNullOrWhiteSpace(value)) q.Add($"{key}={Uri.EscapeDataString(value)}"); }
}
