using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.CustomerDemand;

namespace OAS.Client.Purchasing.CustomerDemand.Services;

public sealed class CustomerDemandTrackingClientService(OasApiClient api) : ICustomerDemandTrackingClientService
{
    public async Task<PagedResult<CustomerDemandTrackingItemDto>> GetPageAsync(CustomerDemandTrackingQueryRequest r, CancellationToken ct = default)
        => await api.GetAsync<PagedResult<CustomerDemandTrackingItemDto>>(Build("api/purchasing/customer-demand", r), ct) ?? new();
    public Task<CustomerDemandTrackingSummaryDto?> GetSummaryAsync(CustomerDemandTrackingQueryRequest r, CancellationToken ct = default)
        => api.GetAsync<CustomerDemandTrackingSummaryDto>(Build("api/purchasing/customer-demand/summary", r with { PageNumber=1, PageSize=1}), ct);
    public Task<CustomerDemandTrackingDetailsDto?> GetDetailsAsync(Guid lineId, CancellationToken ct = default)
        => api.GetAsync<CustomerDemandTrackingDetailsDto>($"api/purchasing/customer-demand/{lineId:D}", ct);
    public Task<CustomerDemandTrackingDetailsDto?> AssignSupplierAsync(Guid lineId, AssignCustomerDemandSupplierRequest r, CancellationToken ct = default)
        => api.PatchAsync<AssignCustomerDemandSupplierRequest,CustomerDemandTrackingDetailsDto>($"api/purchasing/customer-demand/{lineId:D}/supplier", r, ct);
    public Task<CustomerDemandTrackingDetailsDto?> ScheduleAsync(Guid lineId, ScheduleCustomerDemandRequest r, CancellationToken ct = default)
        => api.PatchAsync<ScheduleCustomerDemandRequest,CustomerDemandTrackingDetailsDto>($"api/purchasing/customer-demand/{lineId:D}/schedule", r, ct);
    public async Task<Guid?> CreatePurchaseOrderAsync(Guid lineId, CreateCustomerDemandPurchaseOrderRequest r, CancellationToken ct = default)
    {
        var purchaseOrderId = await api.PostAsync<CreateCustomerDemandPurchaseOrderRequest, Guid>(
            $"api/purchasing/customer-demand/{lineId:D}/create-po", r, ct);
        return purchaseOrderId == Guid.Empty ? null : purchaseOrderId;
    }

    public async Task<Guid?> ResourceRemainingAsync(Guid lineId, ResourceCustomerDemandRemainingRequest r, CancellationToken ct = default)
    {
        var purchaseOrderId = await api.PostAsync<ResourceCustomerDemandRemainingRequest, Guid>(
            $"api/purchasing/customer-demand/{lineId:D}/resource-remaining", r, ct);
        return purchaseOrderId == Guid.Empty ? null : purchaseOrderId;
    }
    public async Task<PagedResult<SupplierPurchaseHistoryItemDto>> GetSupplierHistoryAsync(SupplierPurchaseHistoryQueryRequest r, CancellationToken ct = default)
    {
        var q = new List<string>
        {
            $"pageNumber={Math.Max(1, r.PageNumber)}",
            $"pageSize={Math.Clamp(r.PageSize, 1, PageRequest.MaximumPageSize)}"
        };
        Add(q, "productVariantId", r.ProductVariantId?.ToString());
        Add(q, "fromDate", r.FromDate?.ToString("yyyy-MM-dd"));
        Add(q, "toDate", r.ToDate?.ToString("yyyy-MM-dd"));
        return await api.GetAsync<PagedResult<SupplierPurchaseHistoryItemDto>>(
            $"api/purchasing/suppliers/{r.SupplierId:D}/purchase-history?{string.Join("&", q)}", ct) ?? new();
    }

    private static string Build(string route, CustomerDemandTrackingQueryRequest r)
    {
        var q=new List<string>{$"pageNumber={Math.Max(1,r.PageNumber)}",$"pageSize={Math.Clamp(r.PageSize,1,PageRequest.MaximumPageSize)}"};
        Add(q,"search",r.Search); Add(q,"requestDateFrom",r.RequestDateFrom?.ToString("yyyy-MM-dd")); Add(q,"requestDateTo",r.RequestDateTo?.ToString("yyyy-MM-dd"));
        Add(q,"supplierId",r.SupplierId?.ToString()); Add(q,"status",r.Status?.ToString()); Add(q,"productType",r.ProductType?.ToString()); Add(q,"warehouseId",r.WarehouseId?.ToString());
        Add(q,"expectedDeliveryDate",r.ExpectedDeliveryDate?.ToString("yyyy-MM-dd")); Add(q,"customerOrderId",r.CustomerOrderId?.ToString()); Add(q,"customerId",r.CustomerId?.ToString());
        if(r.OverdueOnly) Add(q,"overdueOnly","true"); Add(q,"eye",r.Eye?.ToString()); Add(q,"requiresProduction",r.RequiresProduction?.ToString().ToLowerInvariant());
        Add(q,"purchaseOrderStatus",r.PurchaseOrderStatus?.ToString()); Add(q,"receiptStatus",r.ReceiptStatus?.ToString()); Add(q,"sortBy",r.SortBy); Add(q,"sortDirection",r.SortDirection.ToString());
        return route+"?"+string.Join("&",q);
    }
    private static void Add(List<string> q,string key,string? value){if(!string.IsNullOrWhiteSpace(value)) q.Add($"{key}={Uri.EscapeDataString(value)}");}
}
