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

public interface ISalesClientService
{
    Task<PagedResult<PrescriptionDto>> GetPrescriptionsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionDto?> GetPrescriptionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesCodeReservationDto?> ReservePrescriptionCodeAsync(CancellationToken cancellationToken = default);
    Task<PrescriptionDto?> CreatePrescriptionAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionDto?> UpdatePrescriptionAsync(Guid id, UpdatePrescriptionRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionDto?> CreatePrescriptionRevisionAsync(Guid id, CreatePrescriptionRevisionRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionDto?> SetPrescriptionStatusAsync(Guid id, SetPrescriptionStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<CustomerOrderDto>> GetCustomerOrdersPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<CustomerOrderDto?> GetCustomerOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesCodeReservationDto?> ReserveCustomerOrderCodeAsync(DateOnly orderDate, CancellationToken cancellationToken = default);
    Task<CustomerOrderDto?> CreateCustomerOrderAsync(CreateCustomerOrderRequest request, CancellationToken cancellationToken = default);
    Task<CustomerOrderDto?> UpdateCustomerOrderAsync(Guid id, UpdateCustomerOrderRequest request, CancellationToken cancellationToken = default);
    Task<CustomerOrderDto?> ConfirmCustomerOrderAsync(Guid id, ConfirmCustomerOrderRequest request, CancellationToken cancellationToken = default);
    Task<CustomerOrderDto?> CancelCustomerOrderAsync(Guid id, CancelCustomerOrderRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<SalesInvoiceDto>> GetSalesInvoicesPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<SalesInvoiceDto?> GetSalesInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesCodeReservationDto?> ReserveSalesInvoiceCodeAsync(DateOnly invoiceDate, CancellationToken cancellationToken = default);
    Task<SalesInvoiceDto?> CreateSalesInvoiceAsync(CreateSalesInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<SalesInvoiceDto?> CreateSalesInvoiceFromOrderAsync(Guid orderId, CreateSalesInvoiceFromOrderRequest request, CancellationToken cancellationToken = default);
    Task<SalesInvoiceDto?> UpdateSalesInvoiceAsync(Guid id, UpdateSalesInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<SalesConfirmationPreValidationDto?> PreValidateSalesInvoiceConfirmationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesInvoiceDto?> ConfirmSalesInvoiceAsync(Guid id, ConfirmSalesInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<SalesPostingPreValidationDto?> PreValidateSalesInvoicePostingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesInvoicePostingResultDto?> PostSalesInvoiceAsync(Guid id, PostSalesInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<SalesInvoiceDto?> CancelSalesInvoiceAsync(Guid id, CancelSalesInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<SalesInvoicePaymentSummaryDto?> GetSalesInvoicePaymentSummaryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<SalesReturnDto>> GetSalesReturnsPageAsync(PageRequest request, Guid? salesInvoiceId = null, Guid? customerId = null, SalesReturnStatus? status = null, CancellationToken cancellationToken = default);
    Task<SalesReturnDto?> GetSalesReturnByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SalesReturnDto?> CreateSalesReturnAsync(CreateSalesReturnRequest request, CancellationToken cancellationToken = default);
    Task<SalesReturnDto?> ConfirmSalesReturnAsync(Guid id, SalesReturnActionRequest request, CancellationToken cancellationToken = default);
    Task<SalesReturnPostingResultDto?> PostSalesReturnAsync(Guid id, SalesReturnActionRequest request, CancellationToken cancellationToken = default);
    Task<SalesReturnDto?> CancelSalesReturnAsync(Guid id, CancelSalesReturnRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommissionRuleDto>> GetCommissionRulesAsync(Guid? employeeId = null, CancellationToken cancellationToken = default);
    Task<CommissionRuleDto?> CreateCommissionRuleAsync(CreateCommissionRuleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommissionStatementDto>> GetCommissionStatementsAsync(Guid? employeeId = null, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default);
    Task<CommissionStatementDto?> GetCommissionStatementAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CommissionStatementDto?> CalculateCommissionStatementAsync(CalculateCommissionStatementRequest request, CancellationToken cancellationToken = default);
    Task<CommissionStatementDto?> FinalizeCommissionStatementAsync(Guid id, CommissionStatementActionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpticalProductionJobDto>> GetOpticalProductionJobsAsync(OpticalProductionStatus? status = null, Guid? salesInvoiceId = null, CancellationToken cancellationToken = default);
    Task<OpticalProductionJobDto?> GetOpticalProductionJobAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OpticalProductionJobDto?> CreateOpticalProductionJobAsync(CreateOpticalProductionJobRequest request, CancellationToken cancellationToken = default);
    Task<OpticalProductionJobDto?> ReleaseOpticalProductionJobAsync(Guid id, OpticalProductionActionRequest request, CancellationToken cancellationToken = default);
    Task<OpticalProductionJobDto?> StartOpticalProductionJobAsync(Guid id, OpticalProductionActionRequest request, CancellationToken cancellationToken = default);
    Task<OpticalProductionJobDto?> IssueOpticalProductionMaterialsAsync(Guid id, OpticalProductionActionRequest request, CancellationToken cancellationToken = default);
    Task<OpticalProductionJobDto?> SubmitOpticalProductionQcAsync(Guid id, SubmitOpticalProductionQcRequest request, CancellationToken cancellationToken = default);
    Task<OpticalProductionJobDto?> CreateOpticalProductionRemakeAsync(Guid id, CreateOpticalProductionRemakeRequest request, CancellationToken cancellationToken = default);
    Task<OpticalProductionJobDto?> CompleteOpticalProductionJobAsync(Guid id, OpticalProductionActionRequest request, CancellationToken cancellationToken = default);
    Task<OpticalProductionJobDto?> CancelOpticalProductionJobAsync(Guid id, OpticalProductionActionRequest request, CancellationToken cancellationToken = default);

    Task<SalesPriceOverrideDto?> RequestPriceOverrideAsync(Guid invoiceId, RequestSalesPriceOverrideRequest request, CancellationToken cancellationToken = default);
    Task<SalesPriceOverrideDto?> ApprovePriceOverrideAsync(Guid overrideId, ApproveSalesPriceOverrideRequest request, CancellationToken cancellationToken = default);
    Task<SalesPriceOverrideDto?> RejectPriceOverrideAsync(Guid overrideId, RejectSalesPriceOverrideRequest request, CancellationToken cancellationToken = default);
    Task<SalesPriceOverrideDto?> CancelPriceOverrideAsync(Guid overrideId, CancelSalesPriceOverrideRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesCustomerLookupDto>> SearchCustomersAsync(string? search, int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesPrescriptionLookupDto>> SearchPrescriptionsAsync(Guid? customerId, string? search, int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesPrescriptionRevisionLookupDto>> GetPrescriptionRevisionsAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesProductTypeLookupDto>> SearchProductTypesAsync(string? search, int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesProductCategoryLookupDto>> SearchProductCategoriesAsync(Guid? productTypeId, string? search, int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesProductVariantLookupDto>> SearchProductVariantsAsync(Guid? productTypeId, Guid? categoryId, string? search, int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesWarehouseLookupDto>> SearchWarehousesAsync(Guid? productVariantId, string? search, int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesCurrencyLookupDto>> SearchCurrenciesAsync(DateOnly documentDate, string? search, int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesCashAccountLookupDto>> SearchCashAccountsAsync(Guid currencyId, string? search, int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesBankAccountLookupDto>> SearchBankAccountsAsync(Guid currencyId, string? search, int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerOrderLookupDto>> SearchCustomerOrdersAsync(Guid? customerId, string? search, int take = 20, CancellationToken cancellationToken = default);
}
