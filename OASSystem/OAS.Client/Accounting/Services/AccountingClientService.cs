using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Contracts.Common.Pagination;

namespace OAS.Client.Accounting.Services;

public sealed class AccountingClientService(OasApiClient apiClient) : IAccountingClientService
{
    private static string Query(PageRequest request)
    {
        var normalized = request.Normalize();
        var query = $"?pageNumber={normalized.PageNumber}&pageSize={normalized.PageSize}";
        if (!string.IsNullOrWhiteSpace(normalized.Search))
            query += $"&search={Uri.EscapeDataString(normalized.Search)}";
        if (!string.IsNullOrWhiteSpace(normalized.SortBy))
            query += $"&sortBy={Uri.EscapeDataString(normalized.SortBy)}&sortDirection={normalized.SortDirection}";
        return query;
    }

    private async Task<PagedResult<T>> GetPageAsync<T>(string route, PageRequest request, CancellationToken cancellationToken) =>
        await apiClient.GetAsync<PagedResult<T>>($"{route}{Query(request)}", cancellationToken) ?? new();

    public Task<PagedResult<AccountDto>> GetAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<AccountDto>("api/accounting/accounts", request, cancellationToken);
    public Task<AccountDto?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<AccountDto>($"api/accounting/accounts/{id}", cancellationToken);
    public Task<AccountDto?> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateAccountRequest, AccountDto>("api/accounting/accounts", request, cancellationToken);
    public Task<AccountDto?> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateAccountRequest, AccountDto>($"api/accounting/accounts/{id}", request, cancellationToken);
    public Task<AccountDto?> SetAccountStatusAsync(Guid id, SetAccountStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetAccountStatusRequest, AccountDto>($"api/accounting/accounts/{id}/status", request, cancellationToken);

    public Task<PagedResult<FiscalYearDto>> GetFiscalYearsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<FiscalYearDto>("api/accounting/fiscal-years", request, cancellationToken);
    public Task<FiscalYearDto?> GetFiscalYearByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<FiscalYearDto>($"api/accounting/fiscal-years/{id}", cancellationToken);
    public Task<FiscalYearDto?> CreateFiscalYearAsync(CreateFiscalYearRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateFiscalYearRequest, FiscalYearDto>("api/accounting/fiscal-years", request, cancellationToken);
    public Task<FiscalYearDto?> UpdateFiscalYearAsync(Guid id, UpdateFiscalYearRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateFiscalYearRequest, FiscalYearDto>($"api/accounting/fiscal-years/{id}", request, cancellationToken);
    public Task<FiscalYearDto?> SetFiscalYearStatusAsync(Guid id, SetFiscalYearStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetFiscalYearStatusRequest, FiscalYearDto>($"api/accounting/fiscal-years/{id}/status", request, cancellationToken);

    public Task<PagedResult<FiscalPeriodDto>> GetFiscalPeriodsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<FiscalPeriodDto>("api/accounting/fiscal-periods", request, cancellationToken);
    public Task<FiscalPeriodDto?> GetFiscalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<FiscalPeriodDto>($"api/accounting/fiscal-periods/{id}", cancellationToken);
    public Task<FiscalPeriodDto?> CreateFiscalPeriodAsync(CreateFiscalPeriodRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateFiscalPeriodRequest, FiscalPeriodDto>("api/accounting/fiscal-periods", request, cancellationToken);
    public Task<FiscalPeriodDto?> UpdateFiscalPeriodAsync(Guid id, UpdateFiscalPeriodRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateFiscalPeriodRequest, FiscalPeriodDto>($"api/accounting/fiscal-periods/{id}", request, cancellationToken);
    public Task<FiscalPeriodDto?> SetFiscalPeriodStatusAsync(Guid id, SetFiscalPeriodStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetFiscalPeriodStatusRequest, FiscalPeriodDto>($"api/accounting/fiscal-periods/{id}/status", request, cancellationToken);
    public Task<FiscalPeriodDto?> SetPeriodLocksAsync(Guid id, SetFiscalPeriodLocksRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetFiscalPeriodLocksRequest, FiscalPeriodDto>($"api/accounting/fiscal-periods/{id}/locks", request, cancellationToken);

    public Task<PagedResult<JournalEntryDto>> GetJournalsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<JournalEntryDto>("api/accounting/journals", request, cancellationToken);
    public Task<JournalEntryDto?> GetJournalByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<JournalEntryDto>($"api/accounting/journals/{id}", cancellationToken);
    public Task<JournalEntryDto?> CreateJournalAsync(CreateJournalEntryRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateJournalEntryRequest, JournalEntryDto>("api/accounting/journals", request, cancellationToken);
    public Task<JournalEntryDto?> UpdateJournalAsync(Guid id, UpdateJournalEntryRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateJournalEntryRequest, JournalEntryDto>($"api/accounting/journals/{id}", request, cancellationToken);
    public Task<JournalEntryDto?> SetJournalStatusAsync(Guid id, SetJournalEntryStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetJournalEntryStatusRequest, JournalEntryDto>($"api/accounting/journals/{id}/status", request, cancellationToken);
    public Task<JournalEntryDto?> ReverseJournalAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.PostAsync<object, JournalEntryDto>($"api/accounting/journals/{id}/reverse", new { }, cancellationToken);

    public Task<PagedResult<PostingProfileDto>> GetPostingProfilesPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<PostingProfileDto>("api/accounting/posting-profiles", request, cancellationToken);
    public Task<PostingProfileDto?> GetPostingProfileByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<PostingProfileDto>($"api/accounting/posting-profiles/{id}", cancellationToken);
    public Task<PostingProfileDto?> CreatePostingProfileAsync(CreatePostingProfileRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreatePostingProfileRequest, PostingProfileDto>("api/accounting/posting-profiles", request, cancellationToken);
    public Task<PostingProfileDto?> UpdatePostingProfileAsync(Guid id, UpdatePostingProfileRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdatePostingProfileRequest, PostingProfileDto>($"api/accounting/posting-profiles/{id}", request, cancellationToken);
    public Task<PostingProfileDto?> SetPostingProfileStatusAsync(Guid id, SetPostingProfileStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetPostingProfileStatusRequest, PostingProfileDto>($"api/accounting/posting-profiles/{id}/status", request, cancellationToken);

    public Task<PagedResult<CostCenterDto>> GetCostCentersPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<CostCenterDto>("api/accounting/cost-centers", request, cancellationToken);
    public Task<CostCenterDto?> GetCostCenterByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<CostCenterDto>($"api/accounting/cost-centers/{id}", cancellationToken);
    public Task<CostCenterDto?> CreateCostCenterAsync(CreateCostCenterRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateCostCenterRequest, CostCenterDto>("api/accounting/cost-centers", request, cancellationToken);
    public Task<CostCenterDto?> UpdateCostCenterAsync(Guid id, UpdateCostCenterRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateCostCenterRequest, CostCenterDto>($"api/accounting/cost-centers/{id}", request, cancellationToken);
    public Task<CostCenterDto?> SetCostCenterStatusAsync(Guid id, SetCostCenterStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetCostCenterStatusRequest, CostCenterDto>($"api/accounting/cost-centers/{id}/status", request, cancellationToken);

    public Task<PagedResult<CustomerAccountDto>> GetCustomerAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<CustomerAccountDto>("api/accounting/customer-accounts", request, cancellationToken);
    public Task<CustomerAccountDto?> GetCustomerAccountByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<CustomerAccountDto>($"api/accounting/customer-accounts/{id}", cancellationToken);
    public Task<CustomerAccountDto?> CreateCustomerAccountAsync(CreateCustomerAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateCustomerAccountRequest, CustomerAccountDto>("api/accounting/customer-accounts", request, cancellationToken);
    public Task<CustomerAccountDto?> UpdateCustomerAccountAsync(Guid id, UpdateCustomerAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateCustomerAccountRequest, CustomerAccountDto>($"api/accounting/customer-accounts/{id}", request, cancellationToken);
    public Task<CustomerAccountDto?> SetCustomerAccountStatusAsync(Guid id, SetCustomerAccountStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetCustomerAccountStatusRequest, CustomerAccountDto>($"api/accounting/customer-accounts/{id}/status", request, cancellationToken);

    public Task<PagedResult<SupplierAccountDto>> GetSupplierAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<SupplierAccountDto>("api/accounting/supplier-accounts", request, cancellationToken);
    public Task<SupplierAccountDto?> GetSupplierAccountByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<SupplierAccountDto>($"api/accounting/supplier-accounts/{id}", cancellationToken);
    public Task<SupplierAccountDto?> CreateSupplierAccountAsync(CreateSupplierAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateSupplierAccountRequest, SupplierAccountDto>("api/accounting/supplier-accounts", request, cancellationToken);
    public Task<SupplierAccountDto?> UpdateSupplierAccountAsync(Guid id, UpdateSupplierAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateSupplierAccountRequest, SupplierAccountDto>($"api/accounting/supplier-accounts/{id}", request, cancellationToken);
    public Task<SupplierAccountDto?> SetSupplierAccountStatusAsync(Guid id, SetSupplierAccountStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetSupplierAccountStatusRequest, SupplierAccountDto>($"api/accounting/supplier-accounts/{id}/status", request, cancellationToken);

    public Task<PagedResult<ReceiptVoucherDto>> GetReceiptVouchersPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<ReceiptVoucherDto>("api/accounting/receipt-vouchers", request, cancellationToken);
    public Task<ReceiptVoucherDto?> GetReceiptVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<ReceiptVoucherDto>($"api/accounting/receipt-vouchers/{id}", cancellationToken);
    public Task<ReceiptVoucherDto?> CreateReceiptVoucherAsync(CreateReceiptVoucherRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateReceiptVoucherRequest, ReceiptVoucherDto>("api/accounting/receipt-vouchers", request, cancellationToken);
    public Task<ReceiptVoucherDto?> UpdateReceiptVoucherAsync(Guid id, UpdateReceiptVoucherRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateReceiptVoucherRequest, ReceiptVoucherDto>($"api/accounting/receipt-vouchers/{id}", request, cancellationToken);
    public Task<ReceiptVoucherDto?> SetReceiptVoucherStatusAsync(Guid id, SetReceiptVoucherStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetReceiptVoucherStatusRequest, ReceiptVoucherDto>($"api/accounting/receipt-vouchers/{id}/status", request, cancellationToken);

    public Task<PagedResult<PaymentVoucherDto>> GetPaymentVouchersPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<PaymentVoucherDto>("api/accounting/payment-vouchers", request, cancellationToken);
    public Task<PaymentVoucherDto?> GetPaymentVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<PaymentVoucherDto>($"api/accounting/payment-vouchers/{id}", cancellationToken);
    public Task<PaymentVoucherDto?> CreatePaymentVoucherAsync(CreatePaymentVoucherRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreatePaymentVoucherRequest, PaymentVoucherDto>("api/accounting/payment-vouchers", request, cancellationToken);
    public Task<PaymentVoucherDto?> UpdatePaymentVoucherAsync(Guid id, UpdatePaymentVoucherRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdatePaymentVoucherRequest, PaymentVoucherDto>($"api/accounting/payment-vouchers/{id}", request, cancellationToken);
    public Task<PaymentVoucherDto?> SetPaymentVoucherStatusAsync(Guid id, SetPaymentVoucherStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetPaymentVoucherStatusRequest, PaymentVoucherDto>($"api/accounting/payment-vouchers/{id}/status", request, cancellationToken);

    public Task<PagedResult<CashAccountDto>> GetCashAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<CashAccountDto>("api/accounting/cash-accounts", request, cancellationToken);
    public Task<CashAccountDto?> GetCashAccountByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<CashAccountDto>($"api/accounting/cash-accounts/{id}", cancellationToken);
    public Task<CashAccountDto?> CreateCashAccountAsync(CreateCashAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateCashAccountRequest, CashAccountDto>("api/accounting/cash-accounts", request, cancellationToken);
    public Task<CashAccountDto?> UpdateCashAccountAsync(Guid id, UpdateCashAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateCashAccountRequest, CashAccountDto>($"api/accounting/cash-accounts/{id}", request, cancellationToken);
    public Task<CashAccountDto?> SetCashAccountStatusAsync(Guid id, SetCashAccountStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetCashAccountStatusRequest, CashAccountDto>($"api/accounting/cash-accounts/{id}/status", request, cancellationToken);

    public Task<PagedResult<BankAccountDto>> GetBankAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<BankAccountDto>("api/accounting/bank-accounts", request, cancellationToken);
    public Task<BankAccountDto?> GetBankAccountByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<BankAccountDto>($"api/accounting/bank-accounts/{id}", cancellationToken);
    public Task<BankAccountDto?> CreateBankAccountAsync(CreateBankAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateBankAccountRequest, BankAccountDto>("api/accounting/bank-accounts", request, cancellationToken);
    public Task<BankAccountDto?> UpdateBankAccountAsync(Guid id, UpdateBankAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateBankAccountRequest, BankAccountDto>($"api/accounting/bank-accounts/{id}", request, cancellationToken);
    public Task<BankAccountDto?> SetBankAccountStatusAsync(Guid id, SetBankAccountStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetBankAccountStatusRequest, BankAccountDto>($"api/accounting/bank-accounts/{id}/status", request, cancellationToken);

    public Task<PagedResult<CashShiftDto>> GetCashShiftsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<CashShiftDto>("api/accounting/cash-shifts", request, cancellationToken);
    public Task<CashShiftDto?> GetCashShiftByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<CashShiftDto>($"api/accounting/cash-shifts/{id}", cancellationToken);
    public Task<CashShiftDto?> CreateCashShiftAsync(CreateCashShiftRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateCashShiftRequest, CashShiftDto>("api/accounting/cash-shifts", request, cancellationToken);
    public Task<CashShiftDto?> CloseCashShiftAsync(Guid id, SetCashShiftClosingRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetCashShiftClosingRequest, CashShiftDto>($"api/accounting/cash-shifts/{id}/close", request, cancellationToken);
    public Task<CashShiftDto?> ApproveCashShiftAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default) => apiClient.PostAsync<object, CashShiftDto>($"api/accounting/cash-shifts/{id}/approve?rowVersion={Uri.EscapeDataString(rowVersion)}", new { }, cancellationToken);

    public Task<PagedResult<ExpenseDto>> GetExpensesPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<ExpenseDto>("api/accounting/expenses", request, cancellationToken);
    public Task<ExpenseDto?> GetExpenseByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<ExpenseDto>($"api/accounting/expenses/{id}", cancellationToken);
    public Task<ExpenseDto?> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateExpenseRequest, ExpenseDto>("api/accounting/expenses", request, cancellationToken);
    public Task<ExpenseDto?> UpdateExpenseAsync(Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateExpenseRequest, ExpenseDto>($"api/accounting/expenses/{id}", request, cancellationToken);
    public Task<ExpenseDto?> SetExpenseStatusAsync(Guid id, SetExpenseStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetExpenseStatusRequest, ExpenseDto>($"api/accounting/expenses/{id}/status", request, cancellationToken);

    public Task<PagedResult<ExpenseTypeDto>> GetExpenseTypesPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<ExpenseTypeDto>("api/accounting/expenses/types", request, cancellationToken);
    public Task<ExpenseTypeDto?> GetExpenseTypeByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<ExpenseTypeDto>($"api/accounting/expenses/types/{id}", cancellationToken);
    public Task<ExpenseTypeDto?> CreateExpenseTypeAsync(CreateExpenseTypeRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateExpenseTypeRequest, ExpenseTypeDto>("api/accounting/expenses/types", request, cancellationToken);
    public Task<ExpenseTypeDto?> UpdateExpenseTypeAsync(Guid id, UpdateExpenseTypeRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateExpenseTypeRequest, ExpenseTypeDto>($"api/accounting/expenses/types/{id}", request, cancellationToken);
    public Task<ExpenseTypeDto?> SetExpenseTypeStatusAsync(Guid id, SetExpenseTypeStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetExpenseTypeStatusRequest, ExpenseTypeDto>($"api/accounting/expenses/types/{id}/status", request, cancellationToken);

    public Task<PagedResult<PaymentAllocationDto>> GetPaymentAllocationsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<PaymentAllocationDto>("api/accounting/payment-allocations", request, cancellationToken);
    public Task<PaymentAllocationDto?> GetPaymentAllocationByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<PaymentAllocationDto>($"api/accounting/payment-allocations/{id}", cancellationToken);
    public Task<PaymentAllocationDto?> CreatePaymentAllocationAsync(CreatePaymentAllocationRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreatePaymentAllocationRequest, PaymentAllocationDto>("api/accounting/payment-allocations", request, cancellationToken);
    public Task<PaymentAllocationDto?> UpdatePaymentAllocationAsync(Guid id, UpdatePaymentAllocationRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdatePaymentAllocationRequest, PaymentAllocationDto>($"api/accounting/payment-allocations/{id}", request, cancellationToken);
}
