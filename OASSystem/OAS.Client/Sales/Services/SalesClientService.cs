using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.Returns;
using OAS.Contracts.Sales.Production;
using OAS.Contracts.Sales.Commissions;
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

    private static string AppendQuery(string route, params (string Key, string? Value)[] values)
    {
        var parts = values.Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value!)}").ToArray();
        if (parts.Length == 0) return route;
        var separator = route.Contains('?') ? "&" : "?";
        return $"{route}{separator}{string.Join("&", parts)}";
    }

    private static string AddQuery(string route, params (string Key, string? Value)[] values)
    {
        var parts = values.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value!)}").ToArray();
        if (parts.Length == 0) return route;
        var separator = route.Contains('?') ? "&" : "?";
        return $"{route}{separator}{string.Join("&", parts)}";
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
    public Task<SalesInvoiceDto?> UpdateSalesInvoiceAsync(Guid id, UpdateSalesInvoiceRequest r, CancellationToken ct = default) => apiClient.PutAsync<UpdateSalesInvoiceRequest, SalesInvoiceDto>($"api/sales/invoices/{id}", r, ct);
    public Task<SalesConfirmationPreValidationDto?> PreValidateSalesInvoiceConfirmationAsync(Guid id, CancellationToken ct = default) => apiClient.GetAsync<SalesConfirmationPreValidationDto>($"api/sales/invoices/{id}/confirmation-prevalidation", ct);
    public Task<SalesInvoiceDto?> ConfirmSalesInvoiceAsync(Guid id, ConfirmSalesInvoiceRequest r, CancellationToken ct = default) => apiClient.PostAsync<ConfirmSalesInvoiceRequest, SalesInvoiceDto>($"api/sales/invoices/{id}/confirm", r, ct);
    public Task<SalesPostingPreValidationDto?> PreValidateSalesInvoicePostingAsync(Guid id, CancellationToken ct = default) => apiClient.GetAsync<SalesPostingPreValidationDto>($"api/sales/invoices/{id}/posting-prevalidation", ct);
    public Task<SalesInvoicePostingResultDto?> PostSalesInvoiceAsync(Guid id, PostSalesInvoiceRequest r, CancellationToken ct = default) => apiClient.PostAsync<PostSalesInvoiceRequest, SalesInvoicePostingResultDto>($"api/sales/invoices/{id}/post", r, ct);
    public Task<SalesInvoiceDto?> CancelSalesInvoiceAsync(Guid id, CancelSalesInvoiceRequest r, CancellationToken ct = default) => apiClient.PostAsync<CancelSalesInvoiceRequest, SalesInvoiceDto>($"api/sales/invoices/{id}/cancel", r, ct);
    public Task<SalesInvoicePaymentSummaryDto?> GetSalesInvoicePaymentSummaryAsync(Guid id, CancellationToken ct = default) => apiClient.GetAsync<SalesInvoicePaymentSummaryDto>($"api/sales/invoices/{id}/payment-summary", ct);

    public async Task<PagedResult<SalesReturnDto>> GetSalesReturnsPageAsync(PageRequest r, Guid? salesInvoiceId = null, Guid? customerId = null, SalesReturnStatus? status = null, CancellationToken ct = default)
    {
        var route = AppendQuery("api/sales/returns" + PageQuery(r),
            ("salesInvoiceId", salesInvoiceId?.ToString()),
            ("customerId", customerId?.ToString()),
            ("status", status?.ToString()));
        return await apiClient.GetAsync<PagedResult<SalesReturnDto>>(route, ct) ?? new PagedResult<SalesReturnDto>();
    }

    public Task<SalesReturnDto?> GetSalesReturnByIdAsync(Guid id, CancellationToken ct = default)
        => apiClient.GetAsync<SalesReturnDto>($"api/sales/returns/{id:D}", ct);
    public Task<SalesReturnDto?> CreateSalesReturnAsync(CreateSalesReturnRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<CreateSalesReturnRequest, SalesReturnDto>("api/sales/returns", r, ct);
    public Task<SalesReturnDto?> ConfirmSalesReturnAsync(Guid id, SalesReturnActionRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<SalesReturnActionRequest, SalesReturnDto>($"api/sales/returns/{id:D}/confirm", r, ct);
    public Task<SalesReturnPostingResultDto?> PostSalesReturnAsync(Guid id, SalesReturnActionRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<SalesReturnActionRequest, SalesReturnPostingResultDto>($"api/sales/returns/{id:D}/post", r, ct);
    public Task<SalesReturnDto?> CancelSalesReturnAsync(Guid id, CancelSalesReturnRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<CancelSalesReturnRequest, SalesReturnDto>($"api/sales/returns/{id:D}/cancel", r, ct);

    public async Task<IReadOnlyList<CommissionRuleDto>> GetCommissionRulesAsync(Guid? employeeId = null, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<CommissionRuleDto>>(AppendQuery("api/sales/commissions/rules", ("employeeId", employeeId?.ToString())), ct) ?? [];
    public Task<CommissionRuleDto?> CreateCommissionRuleAsync(CreateCommissionRuleRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<CreateCommissionRuleRequest, CommissionRuleDto>("api/sales/commissions/rules", r, ct);
    public async Task<IReadOnlyList<CommissionStatementDto>> GetCommissionStatementsAsync(Guid? employeeId = null, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<CommissionStatementDto>>(AppendQuery("api/sales/commissions/statements",
            ("employeeId", employeeId?.ToString()), ("fromDate", fromDate?.ToString("yyyy-MM-dd")), ("toDate", toDate?.ToString("yyyy-MM-dd"))), ct) ?? [];
    public Task<CommissionStatementDto?> GetCommissionStatementAsync(Guid id, CancellationToken ct = default)
        => apiClient.GetAsync<CommissionStatementDto>($"api/sales/commissions/statements/{id:D}", ct);
    public Task<CommissionStatementDto?> CalculateCommissionStatementAsync(CalculateCommissionStatementRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<CalculateCommissionStatementRequest, CommissionStatementDto>("api/sales/commissions/statements/calculate", r, ct);
    public Task<CommissionStatementDto?> FinalizeCommissionStatementAsync(Guid id, CommissionStatementActionRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<CommissionStatementActionRequest, CommissionStatementDto>($"api/sales/commissions/statements/{id:D}/finalize", r, ct);

    public async Task<IReadOnlyList<OpticalProductionJobDto>> GetOpticalProductionJobsAsync(OpticalProductionStatus? status = null, Guid? salesInvoiceId = null, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<OpticalProductionJobDto>>(AppendQuery("api/sales/production", ("status", status?.ToString()), ("salesInvoiceId", salesInvoiceId?.ToString())), ct) ?? [];
    public Task<OpticalProductionJobDto?> GetOpticalProductionJobAsync(Guid id, CancellationToken ct = default)
        => apiClient.GetAsync<OpticalProductionJobDto>($"api/sales/production/{id:D}", ct);
    public Task<OpticalProductionJobDto?> CreateOpticalProductionJobAsync(CreateOpticalProductionJobRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<CreateOpticalProductionJobRequest, OpticalProductionJobDto>("api/sales/production", r, ct);
    public Task<OpticalProductionJobDto?> ReleaseOpticalProductionJobAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/release", r, ct);
    public Task<OpticalProductionJobDto?> StartOpticalProductionJobAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/start", r, ct);
    public Task<OpticalProductionJobDto?> IssueOpticalProductionMaterialsAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/issue-materials", r, ct);
    public Task<OpticalProductionJobDto?> SubmitOpticalProductionQcAsync(Guid id, SubmitOpticalProductionQcRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<SubmitOpticalProductionQcRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/quality-control", r, ct);
    public Task<OpticalProductionJobDto?> CreateOpticalProductionRemakeAsync(Guid id, CreateOpticalProductionRemakeRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<CreateOpticalProductionRemakeRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/remake", r, ct);
    public Task<OpticalProductionJobDto?> CompleteOpticalProductionJobAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/complete", r, ct);
    public Task<OpticalProductionJobDto?> CancelOpticalProductionJobAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/cancel", r, ct);

    public Task<SalesPriceOverrideDto?> RequestPriceOverrideAsync(Guid invoiceId, RequestSalesPriceOverrideRequest r, CancellationToken ct = default) => apiClient.PostAsync<RequestSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/{invoiceId}/price-overrides", r, ct);
    public Task<SalesPriceOverrideDto?> ApprovePriceOverrideAsync(Guid id, ApproveSalesPriceOverrideRequest r, CancellationToken ct = default) => apiClient.PostAsync<ApproveSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/price-overrides/{id}/approve", r, ct);
    public Task<SalesPriceOverrideDto?> RejectPriceOverrideAsync(Guid id, RejectSalesPriceOverrideRequest r, CancellationToken ct = default) => apiClient.PostAsync<RejectSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/price-overrides/{id}/reject", r, ct);
    public Task<SalesPriceOverrideDto?> CancelPriceOverrideAsync(Guid id, CancelSalesPriceOverrideRequest r, CancellationToken ct = default) => apiClient.PostAsync<CancelSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/price-overrides/{id}/cancel", r, ct);

    // Result-based mutation methods are used by the Sales UI for expected 4xx business responses.
    public Task<ApiCallResult<SalesCodeReservationDto>> ReservePrescriptionCodeResultAsync(CancellationToken ct = default)
        => apiClient.PostResultAsync<object, SalesCodeReservationDto>("api/sales/prescriptions/code/reserve", new { }, ct);
    public Task<ApiCallResult<SalesCodeReservationDto>> ReserveCustomerOrderCodeResultAsync(DateOnly date, CancellationToken ct = default)
        => apiClient.PostResultAsync<object, SalesCodeReservationDto>($"api/sales/customer-orders/code/reserve?orderDate={date:yyyy-MM-dd}", new { }, ct);
    public Task<ApiCallResult<SalesCodeReservationDto>> ReserveSalesInvoiceCodeResultAsync(DateOnly date, CancellationToken ct = default)
        => apiClient.PostResultAsync<object, SalesCodeReservationDto>($"api/sales/invoices/code/reserve?invoiceDate={date:yyyy-MM-dd}", new { }, ct);

    public Task<ApiCallResult<PrescriptionDto>> CreatePrescriptionResultAsync(CreatePrescriptionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CreatePrescriptionRequest, PrescriptionDto>("api/sales/prescriptions", r, ct);
    public Task<ApiCallResult<PrescriptionDto>> UpdatePrescriptionResultAsync(Guid id, UpdatePrescriptionRequest r, CancellationToken ct = default)
        => apiClient.PutResultAsync<UpdatePrescriptionRequest, PrescriptionDto>($"api/sales/prescriptions/{id}", r, ct);
    public Task<ApiCallResult<PrescriptionDto>> CreatePrescriptionRevisionResultAsync(Guid id, CreatePrescriptionRevisionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CreatePrescriptionRevisionRequest, PrescriptionDto>($"api/sales/prescriptions/{id}/revisions", r, ct);
    public Task<ApiCallResult<PrescriptionDto>> SetPrescriptionStatusResultAsync(Guid id, SetPrescriptionStatusRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<SetPrescriptionStatusRequest, PrescriptionDto>($"api/sales/prescriptions/{id}/status", r, ct);

    public Task<ApiCallResult<CustomerOrderDto>> CreateCustomerOrderResultAsync(CreateCustomerOrderRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CreateCustomerOrderRequest, CustomerOrderDto>("api/sales/customer-orders", r, ct);
    public Task<ApiCallResult<CustomerOrderDto>> UpdateCustomerOrderResultAsync(Guid id, UpdateCustomerOrderRequest r, CancellationToken ct = default)
        => apiClient.PutResultAsync<UpdateCustomerOrderRequest, CustomerOrderDto>($"api/sales/customer-orders/{id}", r, ct);
    public Task<ApiCallResult<CustomerOrderDto>> ConfirmCustomerOrderResultAsync(Guid id, ConfirmCustomerOrderRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<ConfirmCustomerOrderRequest, CustomerOrderDto>($"api/sales/customer-orders/{id}/confirm", r, ct);
    public Task<ApiCallResult<CustomerOrderDto>> CancelCustomerOrderResultAsync(Guid id, CancelCustomerOrderRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CancelCustomerOrderRequest, CustomerOrderDto>($"api/sales/customer-orders/{id}/cancel", r, ct);

    public Task<ApiCallResult<SalesInvoiceDto>> CreateSalesInvoiceResultAsync(CreateSalesInvoiceRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CreateSalesInvoiceRequest, SalesInvoiceDto>("api/sales/invoices", r, ct);
    public Task<ApiCallResult<SalesInvoiceDto>> UpdateSalesInvoiceResultAsync(Guid id, UpdateSalesInvoiceRequest r, CancellationToken ct = default)
        => apiClient.PutResultAsync<UpdateSalesInvoiceRequest, SalesInvoiceDto>($"api/sales/invoices/{id}", r, ct);
    public Task<ApiCallResult<SalesInvoiceDto>> ConfirmSalesInvoiceResultAsync(Guid id, ConfirmSalesInvoiceRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<ConfirmSalesInvoiceRequest, SalesInvoiceDto>($"api/sales/invoices/{id}/confirm", r, ct);
    public Task<ApiCallResult<SalesInvoicePostingResultDto>> PostSalesInvoiceResultAsync(Guid id, PostSalesInvoiceRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<PostSalesInvoiceRequest, SalesInvoicePostingResultDto>($"api/sales/invoices/{id}/post", r, ct);
    public Task<ApiCallResult<SalesInvoiceDto>> CancelSalesInvoiceResultAsync(Guid id, CancelSalesInvoiceRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CancelSalesInvoiceRequest, SalesInvoiceDto>($"api/sales/invoices/{id}/cancel", r, ct);

    public Task<ApiCallResult<SalesReturnDto>> CreateSalesReturnResultAsync(CreateSalesReturnRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CreateSalesReturnRequest, SalesReturnDto>("api/sales/returns", r, ct);
    public Task<ApiCallResult<SalesReturnDto>> ConfirmSalesReturnResultAsync(Guid id, SalesReturnActionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<SalesReturnActionRequest, SalesReturnDto>($"api/sales/returns/{id:D}/confirm", r, ct);
    public Task<ApiCallResult<SalesReturnPostingResultDto>> PostSalesReturnResultAsync(Guid id, SalesReturnActionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<SalesReturnActionRequest, SalesReturnPostingResultDto>($"api/sales/returns/{id:D}/post", r, ct);
    public Task<ApiCallResult<SalesReturnDto>> CancelSalesReturnResultAsync(Guid id, CancelSalesReturnRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CancelSalesReturnRequest, SalesReturnDto>($"api/sales/returns/{id:D}/cancel", r, ct);

    public Task<ApiCallResult<CommissionRuleDto>> CreateCommissionRuleResultAsync(CreateCommissionRuleRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CreateCommissionRuleRequest, CommissionRuleDto>("api/sales/commissions/rules", r, ct);
    public Task<ApiCallResult<CommissionStatementDto>> CalculateCommissionStatementResultAsync(CalculateCommissionStatementRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CalculateCommissionStatementRequest, CommissionStatementDto>("api/sales/commissions/statements/calculate", r, ct);
    public Task<ApiCallResult<CommissionStatementDto>> FinalizeCommissionStatementResultAsync(Guid id, CommissionStatementActionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CommissionStatementActionRequest, CommissionStatementDto>($"api/sales/commissions/statements/{id:D}/finalize", r, ct);

    public Task<ApiCallResult<OpticalProductionJobDto>> CreateOpticalProductionJobResultAsync(CreateOpticalProductionJobRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CreateOpticalProductionJobRequest, OpticalProductionJobDto>("api/sales/production", r, ct);
    public Task<ApiCallResult<OpticalProductionJobDto>> ReleaseOpticalProductionJobResultAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/release", r, ct);
    public Task<ApiCallResult<OpticalProductionJobDto>> StartOpticalProductionJobResultAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/start", r, ct);
    public Task<ApiCallResult<OpticalProductionJobDto>> IssueOpticalProductionMaterialsResultAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/issue-materials", r, ct);
    public Task<ApiCallResult<OpticalProductionJobDto>> SubmitOpticalProductionQcResultAsync(Guid id, SubmitOpticalProductionQcRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<SubmitOpticalProductionQcRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/quality-control", r, ct);
    public Task<ApiCallResult<OpticalProductionJobDto>> CreateOpticalProductionRemakeResultAsync(Guid id, CreateOpticalProductionRemakeRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CreateOpticalProductionRemakeRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/remake", r, ct);
    public Task<ApiCallResult<OpticalProductionJobDto>> CompleteOpticalProductionJobResultAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/complete", r, ct);
    public Task<ApiCallResult<OpticalProductionJobDto>> CancelOpticalProductionJobResultAsync(Guid id, OpticalProductionActionRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<OpticalProductionActionRequest, OpticalProductionJobDto>($"api/sales/production/{id:D}/cancel", r, ct);

    public Task<ApiCallResult<SalesPriceOverrideDto>> RequestPriceOverrideResultAsync(Guid invoiceId, RequestSalesPriceOverrideRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<RequestSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/{invoiceId}/price-overrides", r, ct);
    public Task<ApiCallResult<SalesPriceOverrideDto>> ApprovePriceOverrideResultAsync(Guid overrideId, ApproveSalesPriceOverrideRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<ApproveSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/price-overrides/{overrideId}/approve", r, ct);
    public Task<ApiCallResult<SalesPriceOverrideDto>> RejectPriceOverrideResultAsync(Guid overrideId, RejectSalesPriceOverrideRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<RejectSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/price-overrides/{overrideId}/reject", r, ct);
    public Task<ApiCallResult<SalesPriceOverrideDto>> CancelPriceOverrideResultAsync(Guid overrideId, CancelSalesPriceOverrideRequest r, CancellationToken ct = default)
        => apiClient.PostResultAsync<CancelSalesPriceOverrideRequest, SalesPriceOverrideDto>($"api/sales/invoices/price-overrides/{overrideId}/cancel", r, ct);

    public async Task<IReadOnlyList<SalesCustomerLookupDto>> SearchCustomersAsync(string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesCustomerLookupDto>>(AddQuery("api/sales/lookups/customers", ("search", search), ("take", take.ToString())), ct) ?? [];
    public async Task<IReadOnlyList<SalesPrescriptionLookupDto>> SearchPrescriptionsAsync(Guid? customerId, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesPrescriptionLookupDto>>(AddQuery("api/sales/lookups/prescriptions", ("customerId", customerId?.ToString()), ("search", search), ("take", take.ToString())), ct) ?? [];
    public async Task<IReadOnlyList<SalesPrescriptionRevisionLookupDto>> GetPrescriptionRevisionsAsync(Guid prescriptionId, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesPrescriptionRevisionLookupDto>>($"api/sales/lookups/prescriptions/{prescriptionId}/revisions", ct) ?? [];
    public Task<SalesOpticalRangeLookupDto?> GetPrescriptionOpticalRangesAsync(CancellationToken ct = default)
        => apiClient.GetAsync<SalesOpticalRangeLookupDto>("api/sales/lookups/prescription-optical-ranges", ct);

    public async Task<IReadOnlyList<SalesProductTypeLookupDto>> SearchProductTypesAsync(string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesProductTypeLookupDto>>(AddQuery("api/sales/lookups/product-types", ("search", search), ("take", take.ToString())), ct) ?? [];

    public async Task<IReadOnlyList<SalesProductCategoryLookupDto>> SearchProductCategoriesAsync(Guid? productTypeId, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesProductCategoryLookupDto>>(AddQuery("api/sales/lookups/product-categories", ("productTypeId", productTypeId?.ToString()), ("search", search), ("take", take.ToString())), ct) ?? [];

    public async Task<IReadOnlyList<SalesProductVariantLookupDto>> SearchProductVariantsAsync(Guid? productTypeId, Guid? categoryId, string? search, int take = 20, CancellationToken ct = default)
        => await apiClient.GetAsync<IReadOnlyList<SalesProductVariantLookupDto>>(AddQuery("api/sales/lookups/product-variants", ("productTypeId", productTypeId?.ToString()), ("categoryId", categoryId?.ToString()), ("search", search), ("take", take.ToString())), ct) ?? [];
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
