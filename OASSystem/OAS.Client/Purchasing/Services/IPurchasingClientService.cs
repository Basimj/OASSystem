using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Contracts.Purchasing.PurchaseRequests;
using OAS.Contracts.Purchasing.SupplierCatalog;

namespace OAS.Client.Purchasing.Services;

public interface IPurchasingClientService
{
    Task<PagedResult<SupplierCatalogItemDto>> GetSupplierCatalogAsync(PageRequest request, Guid? supplierId = null, Guid? productVariantId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<SupplierCatalogItemDto?> GetSupplierCatalogItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierCatalogItemDto?> CreateSupplierCatalogItemAsync(CreateSupplierCatalogItemRequest request, CancellationToken cancellationToken = default);
    Task<SupplierCatalogItemDto?> UpdateSupplierCatalogItemAsync(Guid id, UpdateSupplierCatalogItemRequest request, CancellationToken cancellationToken = default);
    Task<SupplierCatalogItemDto?> AddSupplierPriceAsync(Guid id, CreateSupplierPriceRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<PurchaseRequestDto>> GetPurchaseRequestsAsync(PageRequest request, PurchaseRequestStatus? status = null, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<PurchaseRequestDto?> GetPurchaseRequestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseRequestDto?> CreatePurchaseRequestAsync(CreatePurchaseRequestRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseRequestDto?> UpdatePurchaseRequestAsync(Guid id, UpdatePurchaseRequestRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseRequestDto?> SubmitPurchaseRequestAsync(Guid id, SetPurchaseRequestStatusRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseRequestDto?> ApprovePurchaseRequestAsync(Guid id, SetPurchaseRequestStatusRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseRequestDto?> RejectPurchaseRequestAsync(Guid id, SetPurchaseRequestStatusRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseRequestDto?> CancelPurchaseRequestAsync(Guid id, SetPurchaseRequestStatusRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> CreatePurchaseOrderFromRequestAsync(Guid id, CreatePurchaseOrderFromRequestRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersAsync(PageRequest request, PurchaseOrderStatus? status = null, Guid? supplierId = null, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> GetPurchaseOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> UpdatePurchaseOrderAsync(Guid id, UpdatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> SubmitPurchaseOrderAsync(Guid id, SubmitPurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> ApprovePurchaseOrderAsync(Guid id, ApprovePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> RejectPurchaseOrderAsync(Guid id, RejectPurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> SendPurchaseOrderAsync(Guid id, SendPurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> CancelPurchaseOrderAsync(Guid id, CancelPurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> ClosePurchaseOrderAsync(Guid id, ClosePurchaseOrderRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<PurchaseReceiptDto>> GetPurchaseReceiptsAsync(PageRequest request, PurchaseReceiptStatus? status = null, Guid? purchaseOrderId = null, Guid? supplierId = null, CancellationToken cancellationToken = default);
    Task<PurchaseReceiptDto?> GetPurchaseReceiptAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseReceiptDto?> CreatePurchaseReceiptAsync(CreatePurchaseReceiptRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseReceiptDto?> UpdatePurchaseReceiptAsync(Guid id, UpdatePurchaseReceiptRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseReceiptDto?> ConfirmPurchaseReceiptAsync(Guid id, ConfirmPurchaseReceiptRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseReceiptPostResultDto?> PostPurchaseReceiptAsync(Guid id, PostPurchaseReceiptRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseReceiptDto?> CancelPurchaseReceiptAsync(Guid id, CancelPurchaseReceiptRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<PurchaseInvoiceDto>> GetPurchaseInvoicesAsync(PageRequest request, PurchaseInvoiceStatus? status = null, Guid? supplierId = null, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceDto?> GetPurchaseInvoiceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceDto?> CreatePurchaseInvoiceAsync(CreatePurchaseInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceDto?> UpdatePurchaseInvoiceAsync(Guid id, UpdatePurchaseInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseMatchResultDto?> MatchPurchaseInvoiceAsync(Guid id, MatchPurchaseInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceDto?> ConfirmPurchaseInvoiceAsync(Guid id, ConfirmPurchaseInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceDto?> ApprovePurchaseVarianceAsync(Guid id, ApprovePurchaseVarianceRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceDto?> RejectPurchaseVarianceAsync(Guid id, RejectPurchaseVarianceRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseInvoicePostResultDto?> PostPurchaseInvoiceAsync(Guid id, PostPurchaseInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceDto?> CancelPurchaseInvoiceAsync(Guid id, CancelPurchaseInvoiceRequest request, CancellationToken cancellationToken = default);
}
