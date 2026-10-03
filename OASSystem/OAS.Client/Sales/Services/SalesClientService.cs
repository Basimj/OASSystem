using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Lookups;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Contracts.Sales.PriceOverrides;
using OAS.Contracts.Sales.SalesInvoices;

namespace OAS.Client.Sales.Services;

public sealed class SalesClientService(OasApiClient apiClient) : ISalesClientService
{
    private static string PageQuery(PageRequest request)
    {
        var r = request.Normalize();
        var query = $"?pageNumber={r.PageNumber}&pageSize={r.PageSize}";
        if (!string.IsNullOrWhiteSpace(r.Search)) query += $"&search={Uri.EscapeDataString(r.Search)}";
        if (!string.IsNullOrWhiteSpace(r.SortBy)) query += $"&sortBy={Uri.EscapeDataString(r.SortBy)}&sortDirection={r.SortDirection}";
        return query;
    }

    private async Task<PagedResult<T>> GetPageAsync<T>(string route, PageRequest request, CancellationToken ct)
        => await apiClient.GetAsync<PagedResult<T>>($"{route}{PageQuery(request)}", ct) ?? new PagedResult<T>();

    private static string AddQuery(string route, params (string Key, string? Value)[] values)
    {
        var parts = values.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value!)}").ToArray();
        return parts.Length == 0 ? route : $"{route}?{string.Join("&", parts)}";
    }

    public Task<PagedResult<PrescriptionDto>> GetPrescriptionsPageAsync(PageRequest r, CancellationToken ct = default) => GetPageAsync<PrescriptionDto>("api/sales/prescriptions", r, ct);
    public Task<PrescriptionDto?> GetPrescriptionByIdAsync(Guid id, CancellationToken ct = default) => apiClient.GetAsync<PrescriptionDto>($"api/sales/prescriptions/{id}", ct);
    public Task<SalesCodeReservationDto?> ReservePrescriptionCodeAsync(CancellationToken ct = default) => apiClient.PostAsync<object, SalesCodeReservationDto>("api/sales/prescriptions/code/reserve", new { }, ct);
    public Task<PrescriptionDto?> CreatePrescriptionAsync(CreatePrescriptionRequest r, CancellationToken ct = default) => apiClient.PostAsync<CreatePrescriptionRequest, PrescriptionDto>("api/sales/prescriptions", r, ct);
    public Task<PrescriptionDto?> UpdatePrescriptionAsync(Guid id, UpdatePrescriptionRequest r, CancellationToken ct = default) => apiClient.PutAsync<UpdatePrescriptionRequest, PrescriptionDto>($"api/sales/prescriptions/{id}", r, ct);
    public Task<PrescriptionDto?> CreatePrescriptionRevisionAsync(Guid id, CreatePrescriptionRevisionRequest r, CancellationToken ct = default) => apiClient.PostAsync<CreatePrescriptionRevisionRequest, PrescriptionDto>($"api/sales/prescriptions/{id}/revisions", r, ct);
    public Task<PrescriptionDto?> SetPrescriptionStatusAsync(Guid id, SetPrescriptionStatusRequest r, CancellationToken ct = default) => apiClient.PostAsync<SetPrescriptionStatusRequest, PrescriptionDto>($"api/sales/prescriptions/{id}/status", r, ct);

    public Task<PagedResult<CustomerOrderDto>> GetCustomerOrdersPageAsync(PageRequest r, CancellationToken ct = default) => GetPageAsync<CustomerOrderDto>("api/sales/customer-orders", r, ct);
    public Task<CustomerOrderDto?> GetCustomerOrderByIdAsync(Guid id, CancellationToken ct = default) => apiClient.GetAsync<CustomerOrderDto>($"api/sales/customer-orders/{id}", ct);
    public Task<SalesCodeReservationDto?> ReserveCustomerOrderCodeAsync(DateOnly date, CancellationToken ct = default) => apiClient.PostAsync<object, SalesCodeReservationDto>($"api/sales/customer-orders/code/reserve?orderDate={date:yyyy-MM-dd}", new { }, ct);
    public Task<CustomerOrderDto?> CreateCustomerOrderAsync(CreateCustomerOrderRequest r, CancellationToken ct = default) => apiClient.PostAsync<CreateCustomerOrderRequest, CustomerOrderDto>("api/sales/customer-orders", r, ct);
    public Task<CustomerOrderDto?> UpdateCustomerOrderAsync(Guid id, UpdateCustomerOrderRequest r, CancellationToken ct = default) => apiClient.PutAsync<UpdateCustomerOrderRequest, CustomerOrderDto>($"api/sales/customer-orders/{id}", r, ct);
    public Task<CustomerOrderDto?> ConfirmCustomerOrderAsync(Guid id, ConfirmCustomerOrderRequest r, CancellationToken ct = default) => apiClient.PostAsync<ConfirmCustomerOrderRequest, CustomerOrderDto>($"api/sales/customer-orders/{id}/confirm", r, ct);
    public Task<CustomerOrderDto?> CancelCustomerOrderAsync(Guid id, CancelCustomerOrderRequest r, CancellationToken ct = default) => apiClient.PostAsync<CancelCustomerOrderRequest, CustomerOrderDto>($"api/sales/customer-orders/{id}/cancel", r, ct);

    public Task<PagedResult<SalesInvoiceDto>> GetSalesInvoicesPageAsync(PageRequest r, CancellationToken ct = default) => GetPageAsync<SalesInvoiceDto>("api/sales/invoices", r, ct);
    public Task<SalesInvoiceDto?> GetSalesInvoiceByIdAsync(Guid id, CancellationToken ct = default) => apiClient.GetAsync<SalesInvoiceDto>($"api/sales/invoices/{id}", ct);
    public Task<SalesCodeReservationDto?> ReserveSalesInvoiceCodeAsync(DateOnly date, CancellationToken ct = default) => apiClient.PostAsync<object, SalesCodeReservationDto>($"api/sales/invoices/code/reserve?invoiceDate={date:yyyy-MM-dd}", new { }, ct);
    public Task<SalesInvoiceDto?> CreateSalesInvoiceAsync(CreateSalesInvoiceRequest r, CancellationToken ct = default) => apiClient.PostAsync<CreateSalesInvoiceRequest, SalesInvoiceDto>("api/sales/invoices", r, ct);
    public Task<SalesInvoiceDto?> CreateSalesInvoiceFromOrderAsync(Guid orderId, CreateSalesInvoiceFromOrderRequest r, CancellationToken ct = default) => apiClient.PostAsync<CreateSalesInvoiceFromOrderRequest, SalesInvoiceDto>($"api/sales/invoices/from-order/{orderId}", r, ct);
    public Task<SalesInvoiceDto?> UpdateSalesInvoiceAsync(Guid id, UpdateSalesInvoiceRequest r, CancellationToken ct = default) => apiClient.PutAsync<UpdateSalesInvoiceRequest, SalesInvoiceDto>($"api/sales/invoices/{id}", r, ct);
    public Task<SalesConfirmationPreValidationDto?> PreValidateSalesInvoiceConfirmationAsync(Guid id, CancellationToken ct = default) => apiClient.GetAsync<SalesConfirmationPreValidationDto>($"api/sales/invoices/{id}/confirmation-prevalidation", ct);
    public Task<SalesInvoiceDto?> ConfirmSalesInvoiceAsync(Guid id, ConfirmSalesInvoiceRequest r, CancellationToken ct = default) => apiClient.PostAsync<ConfirmSalesInvoiceRequest, SalesInvoiceDto>($"api/sales/invoices/{id}/confirm", r, ct);
    public Task<SalesPostingPreValidationDto?> PreValidateSalesInvoicePostingAsync(Guid id, CancellationToken ct = default) => apiClient.GetAsync<SalesPostingPreValidationDto>($"api/sales/invoices/{id}/posting-prevalidation", ct);
    public Task<SalesInvoicePostingResultDto?> PostSalesInvoiceAsync(Guid id, PostSalesInvoiceRequest r, CancellationToken ct = default) => apiClient.PostAsync<PostSalesInvoiceRequest, SalesInvoicePostingResultDto>($"api/sales/invoices/{id}/post", r, ct);
    public Task<SalesInvoiceDto?> CancelSalesInvoiceAsync(Guid id, CancelSalesInvoiceRequest r, CancellationToken ct = default) => apiClient.PostAsync<CancelSalesInvoiceRequest, SalesInvoiceDto>($"api/sales/invoices/{id}/cancel", r, ct);
    public Task<SalesInvoicePaymentSummaryDto?> GetSalesInvoicePaymentSummaryAsync(Guid id, CancellationToken ct = default) => apiClient.GetAsync<SalesInvoicePaymentSummaryDto>($"api/sales/invoices/{id}/payment-summary", ct);

    public Task<SalesPriceOverrideDto?> RequestPriceOverrideAsync(Guid invoiceId, RequestSalesPriceOverrideRequest r, CancellationToken ct = default) => apiClient.PostAsync<RequestSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/{invoiceId}/price-overrides", r, ct);
    public Task<SalesPriceOverrideDto?> ApprovePriceOverrideAsync(Guid id, ApproveSalesPriceOverrideRequest r, CancellationToken ct = default) => apiClient.PostAsync<ApproveSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/price-overrides/{id}/approve", r, ct);
    public Task<SalesPriceOverrideDto?> RejectPriceOverrideAsync(Guid id, RejectSalesPriceOverrideRequest r, CancellationToken ct = default) => apiClient.PostAsync<RejectSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/price-overrides/{id}/reject", r, ct);
    public Task<SalesPriceOverrideDto?> CancelPriceOverrideAsync(Guid id, CancelSalesPriceOverrideRequest r, CancellationToken ct = default) => apiClient.PostAsync<CancelSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/price-overrides/{id}/cancel", r, ct);

    public async Task<IReadOnlyList<SalesCustomerLookupDto>> SearchCustomersAsync(string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesCustomerLookupDto>>(AddQuery("api/sales/lookups/customers", ("search", search), ("take", take.ToString())), ct) ?? [];
    public async Task<IReadOnlyList<SalesPrescriptionLookupDto>> SearchPrescriptionsAsync(Guid? customerId, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesPrescriptionLookupDto>>(AddQuery("api/sales/lookups/prescriptions", ("customerId", customerId?.ToString()), ("search", search), ("take", take.ToString())), ct) ?? [];
    public async Task<IReadOnlyList<SalesPrescriptionRevisionLookupDto>> GetPrescriptionRevisionsAsync(Guid prescriptionId, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesPrescriptionRevisionLookupDto>>($"api/sales/lookups/prescriptions/{prescriptionId}/revisions", ct) ?? [];
    public async Task<IReadOnlyList<SalesProductCategoryLookupDto>> SearchProductCategoriesAsync(string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesProductCategoryLookupDto>>(AddQuery("api/sales/lookups/product-categories", ("search", search), ("take", take.ToString())), ct) ?? [];

    public async Task<IReadOnlyList<SalesProductVariantLookupDto>> SearchProductVariantsAsync(Guid? categoryId, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesProductVariantLookupDto>>(AddQuery("api/sales/lookups/product-variants", ("categoryId", categoryId?.ToString()), ("search", search), ("take", take.ToString())), ct) ?? [];
    public async Task<IReadOnlyList<SalesWarehouseLookupDto>> SearchWarehousesAsync(Guid? productVariantId, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesWarehouseLookupDto>>(AddQuery("api/sales/lookups/warehouses", ("productVariantId", productVariantId?.ToString()), ("search", search), ("take", take.ToString())), ct) ?? [];
    public async Task<IReadOnlyList<SalesCurrencyLookupDto>> SearchCurrenciesAsync(DateOnly documentDate, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesCurrencyLookupDto>>(AddQuery("api/sales/lookups/currencies", ("documentDate", documentDate.ToString("yyyy-MM-dd")), ("search", search), ("take", take.ToString())), ct) ?? [];
    public async Task<IReadOnlyList<SalesCashAccountLookupDto>> SearchCashAccountsAsync(Guid currencyId, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesCashAccountLookupDto>>(AddQuery("api/sales/lookups/cash-accounts", ("currencyId", currencyId.ToString()), ("search", search), ("take", take.ToString())), ct) ?? [];
    public async Task<IReadOnlyList<SalesBankAccountLookupDto>> SearchBankAccountsAsync(Guid currencyId, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesBankAccountLookupDto>>(AddQuery("api/sales/lookups/bank-accounts", ("currencyId", currencyId.ToString()), ("search", search), ("take", take.ToString())), ct) ?? [];
    public async Task<IReadOnlyList<CustomerOrderLookupDto>> SearchCustomerOrdersAsync(Guid? customerId, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<CustomerOrderLookupDto>>(AddQuery("api/sales/lookups/customer-orders", ("customerId", customerId?.ToString()), ("search", search), ("take", take.ToString())), ct) ?? [];
}
