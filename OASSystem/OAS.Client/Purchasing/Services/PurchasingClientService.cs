using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Contracts.Purchasing.PurchaseRequests;
using OAS.Contracts.Purchasing.PurchaseReturns;
using OAS.Contracts.Purchasing.SupplierCatalog;

namespace OAS.Client.Purchasing.Services;

public sealed class PurchasingClientService(OasApiClient api) : IPurchasingClientService
{
    private static string PageQuery(PageRequest request)
    {
        var x=request.Normalize();
        var q=$"?pageNumber={x.PageNumber}&pageSize={x.PageSize}";
        if(!string.IsNullOrWhiteSpace(x.Search)) q+=$"&search={Uri.EscapeDataString(x.Search)}";
        if(!string.IsNullOrWhiteSpace(x.SortBy)) q+=$"&sortBy={Uri.EscapeDataString(x.SortBy)}&sortDirection={x.SortDirection}";
        return q;
    }
    private static string Add(string q,string name,object? value)=>value is null?q:$"{q}&{name}={Uri.EscapeDataString(value.ToString()!)}";
    private async Task<PagedResult<T>> Page<T>(string route,PageRequest request,CancellationToken ct)
        => await api.GetAsync<PagedResult<T>>(route+PageQuery(request),ct) ?? new PagedResult<T>();

    public async Task<PagedResult<SupplierCatalogItemDto>> GetSupplierCatalogAsync(PageRequest r, Guid? supplierId=null, Guid? productVariantId=null, bool? isActive=null, CancellationToken ct=default)
    { var q=Add(Add(Add("api/purchasing/supplier-catalog"+PageQuery(r),"supplierId",supplierId),"productVariantId",productVariantId),"isActive",isActive); return await api.GetAsync<PagedResult<SupplierCatalogItemDto>>(q,ct) ?? new PagedResult<SupplierCatalogItemDto>(); }
    public Task<SupplierCatalogItemDto?> GetSupplierCatalogItemAsync(Guid id,CancellationToken ct=default)=>api.GetAsync<SupplierCatalogItemDto>($"api/purchasing/supplier-catalog/{id}",ct);
    public Task<SupplierCatalogItemDto?> CreateSupplierCatalogItemAsync(CreateSupplierCatalogItemRequest r,CancellationToken ct=default)=>api.PostAsync<CreateSupplierCatalogItemRequest,SupplierCatalogItemDto>("api/purchasing/supplier-catalog",r,ct);
    public Task<SupplierCatalogItemDto?> UpdateSupplierCatalogItemAsync(Guid id,UpdateSupplierCatalogItemRequest r,CancellationToken ct=default)=>api.PutAsync<UpdateSupplierCatalogItemRequest,SupplierCatalogItemDto>($"api/purchasing/supplier-catalog/{id}",r,ct);
    public Task<SupplierCatalogItemDto?> AddSupplierPriceAsync(Guid id,CreateSupplierPriceRequest r,CancellationToken ct=default)=>api.PostAsync<CreateSupplierPriceRequest,SupplierCatalogItemDto>($"api/purchasing/supplier-catalog/{id}/prices",r,ct);

    public Task<PagedResult<PurchaseRequestDto>> GetPurchaseRequestsAsync(PageRequest r,PurchaseRequestStatus? status=null,Guid? warehouseId=null,CancellationToken ct=default)
    { var q=Add(Add("api/purchasing/requests"+PageQuery(r),"status",status),"warehouseId",warehouseId); return GetPage<PurchaseRequestDto>(q,ct); }
    public Task<PurchaseRequestDto?> GetPurchaseRequestAsync(Guid id,CancellationToken ct=default)=>api.GetAsync<PurchaseRequestDto>($"api/purchasing/requests/{id}",ct);
    public Task<PurchaseRequestDto?> CreatePurchaseRequestAsync(CreatePurchaseRequestRequest r,CancellationToken ct=default)=>api.PostAsync<CreatePurchaseRequestRequest,PurchaseRequestDto>("api/purchasing/requests",r,ct);
    public Task<PurchaseRequestDto?> UpdatePurchaseRequestAsync(Guid id,UpdatePurchaseRequestRequest r,CancellationToken ct=default)=>api.PutAsync<UpdatePurchaseRequestRequest,PurchaseRequestDto>($"api/purchasing/requests/{id}",r,ct);
    public Task<PurchaseRequestDto?> SubmitPurchaseRequestAsync(Guid id,SetPurchaseRequestStatusRequest r,CancellationToken ct=default)=>Post<SetPurchaseRequestStatusRequest,PurchaseRequestDto>($"api/purchasing/requests/{id}/submit",r,ct);
    public Task<PurchaseRequestDto?> ApprovePurchaseRequestAsync(Guid id,SetPurchaseRequestStatusRequest r,CancellationToken ct=default)=>Post<SetPurchaseRequestStatusRequest,PurchaseRequestDto>($"api/purchasing/requests/{id}/approve",r,ct);
    public Task<PurchaseRequestDto?> RejectPurchaseRequestAsync(Guid id,SetPurchaseRequestStatusRequest r,CancellationToken ct=default)=>Post<SetPurchaseRequestStatusRequest,PurchaseRequestDto>($"api/purchasing/requests/{id}/reject",r,ct);
    public Task<PurchaseRequestDto?> CancelPurchaseRequestAsync(Guid id,SetPurchaseRequestStatusRequest r,CancellationToken ct=default)=>Post<SetPurchaseRequestStatusRequest,PurchaseRequestDto>($"api/purchasing/requests/{id}/cancel",r,ct);
    public Task<PurchaseOrderDto?> CreatePurchaseOrderFromRequestAsync(Guid id,CreatePurchaseOrderFromRequestRequest r,CancellationToken ct=default)=>Post<CreatePurchaseOrderFromRequestRequest,PurchaseOrderDto>($"api/purchasing/requests/{id}/create-purchase-order",r,ct);

    public Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersAsync(PageRequest r,PurchaseOrderStatus? status=null,Guid? supplierId=null,Guid? warehouseId=null,CancellationToken ct=default)
    { var q=Add(Add(Add("api/purchasing/orders"+PageQuery(r),"status",status),"supplierId",supplierId),"warehouseId",warehouseId); return GetPage<PurchaseOrderDto>(q,ct); }
    public Task<PurchaseOrderDto?> GetPurchaseOrderAsync(Guid id,CancellationToken ct=default)=>api.GetAsync<PurchaseOrderDto>($"api/purchasing/orders/{id}",ct);
    public Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest r,CancellationToken ct=default)=>Post<CreatePurchaseOrderRequest,PurchaseOrderDto>("api/purchasing/orders",r,ct);
    public Task<PurchaseOrderDto?> UpdatePurchaseOrderAsync(Guid id,UpdatePurchaseOrderRequest r,CancellationToken ct=default)=>api.PutAsync<UpdatePurchaseOrderRequest,PurchaseOrderDto>($"api/purchasing/orders/{id}",r,ct);
    public Task<PurchaseOrderDto?> SubmitPurchaseOrderAsync(Guid id,SubmitPurchaseOrderRequest r,CancellationToken ct=default)=>Post<SubmitPurchaseOrderRequest,PurchaseOrderDto>($"api/purchasing/orders/{id}/submit",r,ct);
    public Task<PurchaseOrderDto?> ApprovePurchaseOrderAsync(Guid id,ApprovePurchaseOrderRequest r,CancellationToken ct=default)=>Post<ApprovePurchaseOrderRequest,PurchaseOrderDto>($"api/purchasing/orders/{id}/approve",r,ct);
    public Task<PurchaseOrderDto?> RejectPurchaseOrderAsync(Guid id,RejectPurchaseOrderRequest r,CancellationToken ct=default)=>Post<RejectPurchaseOrderRequest,PurchaseOrderDto>($"api/purchasing/orders/{id}/reject",r,ct);
    public Task<PurchaseOrderDto?> SendPurchaseOrderAsync(Guid id,SendPurchaseOrderRequest r,CancellationToken ct=default)=>Post<SendPurchaseOrderRequest,PurchaseOrderDto>($"api/purchasing/orders/{id}/send",r,ct);
    public Task<PurchaseOrderDto?> CancelPurchaseOrderAsync(Guid id,CancelPurchaseOrderRequest r,CancellationToken ct=default)=>Post<CancelPurchaseOrderRequest,PurchaseOrderDto>($"api/purchasing/orders/{id}/cancel",r,ct);
    public Task<PurchaseOrderDto?> ClosePurchaseOrderAsync(Guid id,ClosePurchaseOrderRequest r,CancellationToken ct=default)=>Post<ClosePurchaseOrderRequest,PurchaseOrderDto>($"api/purchasing/orders/{id}/close",r,ct);

    public Task<PagedResult<PurchaseReceiptDto>> GetPurchaseReceiptsAsync(PageRequest r,PurchaseReceiptStatus? status=null,Guid? purchaseOrderId=null,Guid? supplierId=null,CancellationToken ct=default)
    { var q=Add(Add(Add("api/purchasing/receipts"+PageQuery(r),"status",status),"purchaseOrderId",purchaseOrderId),"supplierId",supplierId); return GetPage<PurchaseReceiptDto>(q,ct); }
    public Task<PurchaseReceiptDto?> GetPurchaseReceiptAsync(Guid id,CancellationToken ct=default)=>api.GetAsync<PurchaseReceiptDto>($"api/purchasing/receipts/{id}",ct);
    public Task<PurchaseReceiptDto?> CreatePurchaseReceiptAsync(CreatePurchaseReceiptRequest r,CancellationToken ct=default)=>Post<CreatePurchaseReceiptRequest,PurchaseReceiptDto>("api/purchasing/receipts",r,ct);
    public Task<PurchaseReceiptDto?> UpdatePurchaseReceiptAsync(Guid id,UpdatePurchaseReceiptRequest r,CancellationToken ct=default)=>api.PutAsync<UpdatePurchaseReceiptRequest,PurchaseReceiptDto>($"api/purchasing/receipts/{id}",r,ct);
    public Task<PurchaseReceiptDto?> ConfirmPurchaseReceiptAsync(Guid id,ConfirmPurchaseReceiptRequest r,CancellationToken ct=default)=>Post<ConfirmPurchaseReceiptRequest,PurchaseReceiptDto>($"api/purchasing/receipts/{id}/confirm",r,ct);
    public Task<PurchaseReceiptPostResultDto?> PostPurchaseReceiptAsync(Guid id,PostPurchaseReceiptRequest r,CancellationToken ct=default)=>Post<PostPurchaseReceiptRequest,PurchaseReceiptPostResultDto>($"api/purchasing/receipts/{id}/post",r,ct);
    public Task<PurchaseReceiptDto?> CancelPurchaseReceiptAsync(Guid id,CancelPurchaseReceiptRequest r,CancellationToken ct=default)=>Post<CancelPurchaseReceiptRequest,PurchaseReceiptDto>($"api/purchasing/receipts/{id}/cancel",r,ct);

    public Task<PagedResult<PurchaseInvoiceDto>> GetPurchaseInvoicesAsync(PageRequest r,PurchaseInvoiceStatus? status=null,Guid? supplierId=null,CancellationToken ct=default)
    { var q=Add(Add("api/purchasing/invoices"+PageQuery(r),"status",status),"supplierId",supplierId); return GetPage<PurchaseInvoiceDto>(q,ct); }
    public Task<PurchaseInvoiceDto?> GetPurchaseInvoiceAsync(Guid id,CancellationToken ct=default)=>api.GetAsync<PurchaseInvoiceDto>($"api/purchasing/invoices/{id}",ct);
    public Task<PurchaseInvoiceDto?> CreatePurchaseInvoiceAsync(CreatePurchaseInvoiceRequest r,CancellationToken ct=default)=>Post<CreatePurchaseInvoiceRequest,PurchaseInvoiceDto>("api/purchasing/invoices",r,ct);
    public Task<PurchaseInvoiceDto?> UpdatePurchaseInvoiceAsync(Guid id,UpdatePurchaseInvoiceRequest r,CancellationToken ct=default)=>api.PutAsync<UpdatePurchaseInvoiceRequest,PurchaseInvoiceDto>($"api/purchasing/invoices/{id}",r,ct);
    public Task<PurchaseMatchResultDto?> MatchPurchaseInvoiceAsync(Guid id,MatchPurchaseInvoiceRequest r,CancellationToken ct=default)=>Post<MatchPurchaseInvoiceRequest,PurchaseMatchResultDto>($"api/purchasing/invoices/{id}/match",r,ct);
    public Task<PurchaseInvoiceDto?> ConfirmPurchaseInvoiceAsync(Guid id,ConfirmPurchaseInvoiceRequest r,CancellationToken ct=default)=>Post<ConfirmPurchaseInvoiceRequest,PurchaseInvoiceDto>($"api/purchasing/invoices/{id}/confirm",r,ct);
    public Task<PurchaseInvoiceDto?> ApprovePurchaseVarianceAsync(Guid id,ApprovePurchaseVarianceRequest r,CancellationToken ct=default)=>Post<ApprovePurchaseVarianceRequest,PurchaseInvoiceDto>($"api/purchasing/invoices/{id}/variances/approve",r,ct);
    public Task<PurchaseInvoiceDto?> RejectPurchaseVarianceAsync(Guid id,RejectPurchaseVarianceRequest r,CancellationToken ct=default)=>Post<RejectPurchaseVarianceRequest,PurchaseInvoiceDto>($"api/purchasing/invoices/{id}/variances/reject",r,ct);
    public Task<PurchaseInvoicePostResultDto?> PostPurchaseInvoiceAsync(Guid id,PostPurchaseInvoiceRequest r,CancellationToken ct=default)=>Post<PostPurchaseInvoiceRequest,PurchaseInvoicePostResultDto>($"api/purchasing/invoices/{id}/post",r,ct);
    public Task<PurchaseInvoiceDto?> CancelPurchaseInvoiceAsync(Guid id,CancelPurchaseInvoiceRequest r,CancellationToken ct=default)=>Post<CancelPurchaseInvoiceRequest,PurchaseInvoiceDto>($"api/purchasing/invoices/{id}/cancel",r,ct);

    public Task<PagedResult<PurchaseReturnDto>> GetPurchaseReturnsAsync(PageRequest r, PurchaseReturnStatus? status=null, Guid? purchaseReceiptId=null, Guid? purchaseInvoiceId=null, Guid? supplierId=null, CancellationToken ct=default)
    {
        var q=Add(Add(Add(Add("api/purchasing/returns"+PageQuery(r),"status",status),"purchaseReceiptId",purchaseReceiptId),"purchaseInvoiceId",purchaseInvoiceId),"supplierId",supplierId);
        return GetPage<PurchaseReturnDto>(q,ct);
    }
    public Task<PurchaseReturnDto?> GetPurchaseReturnAsync(Guid id,CancellationToken ct=default)=>api.GetAsync<PurchaseReturnDto>($"api/purchasing/returns/{id:D}",ct);
    public Task<PurchaseReturnDto?> CreatePurchaseReturnAsync(CreatePurchaseReturnRequest r,CancellationToken ct=default)=>Post<CreatePurchaseReturnRequest,PurchaseReturnDto>("api/purchasing/returns",r,ct);
    public Task<PurchaseReturnDto?> ConfirmPurchaseReturnAsync(Guid id,PurchaseReturnActionRequest r,CancellationToken ct=default)=>Post<PurchaseReturnActionRequest,PurchaseReturnDto>($"api/purchasing/returns/{id:D}/confirm",r,ct);
    public Task<PurchaseReturnPostingResultDto?> PostPurchaseReturnAsync(Guid id,PurchaseReturnActionRequest r,CancellationToken ct=default)=>Post<PurchaseReturnActionRequest,PurchaseReturnPostingResultDto>($"api/purchasing/returns/{id:D}/post",r,ct);
    public Task<PurchaseReturnDto?> CancelPurchaseReturnAsync(Guid id,CancelPurchaseReturnRequest r,CancellationToken ct=default)=>Post<CancelPurchaseReturnRequest,PurchaseReturnDto>($"api/purchasing/returns/{id:D}/cancel",r,ct);

    private async Task<PagedResult<T>> GetPage<T>(string uri,CancellationToken ct)=>await api.GetAsync<PagedResult<T>>(uri,ct)??new();
    private Task<T?> Post<TRequest,T>(string uri,TRequest payload,CancellationToken ct)=>api.PostAsync<TRequest,T>(uri,payload,ct);
}
