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
    private static string Query(PageRequest req) =>
        $"?pageNumber={req.PageNumber}&pageSize={req.PageSize}&searchTerm={Uri.EscapeDataString(req.Search ?? string.Empty)}";

    // Accounts
    public async Task<PagedResult<AccountDto>> GetAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<AccountDto>>($"api/accounting/accounts{Query(request)}", cancellationToken) ?? new();

    public Task<AccountDto?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<AccountDto>($"api/accounting/accounts/{id}", cancellationToken);

    public Task<AccountDto?> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateAccountRequest, AccountDto>("api/accounting/accounts", request, cancellationToken);

    public Task<AccountDto?> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<UpdateAccountRequest, AccountDto>($"api/accounting/accounts/{id}", request, cancellationToken);

    public async Task<bool> SetAccountStatusAsync(Guid id, SetAccountStatusRequest request, CancellationToken cancellationToken = default)
    {
        var res = await apiClient.PostResultAsync<SetAccountStatusRequest, object>($"api/accounting/accounts/{id}/status", request, cancellationToken);
        return res.Succeeded;
    }

    // Fiscal Years & Periods
    public async Task<PagedResult<FiscalYearDto>> GetFiscalYearsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<FiscalYearDto>>($"api/accounting/fiscal-years{Query(request)}", cancellationToken) ?? new();

    public Task<FiscalYearDto?> GetFiscalYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<FiscalYearDto>($"api/accounting/fiscal-years/{id}", cancellationToken);

    public Task<FiscalYearDto?> CreateFiscalYearAsync(CreateFiscalYearRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateFiscalYearRequest, FiscalYearDto>("api/accounting/fiscal-years", request, cancellationToken);

    public async Task<PagedResult<FiscalPeriodDto>> GetFiscalPeriodsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<FiscalPeriodDto>>($"api/accounting/fiscal-periods{Query(request)}", cancellationToken) ?? new();

    public async Task<bool> SetPeriodLocksAsync(Guid id, SetFiscalPeriodLocksRequest request, CancellationToken cancellationToken = default)
    {
        var res = await apiClient.PostResultAsync<SetFiscalPeriodLocksRequest, object>($"api/accounting/fiscal-periods/{id}/locks", request, cancellationToken);
        return res.Succeeded;
    }

    // Journals
    public async Task<PagedResult<JournalEntryDto>> GetJournalsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<JournalEntryDto>>($"api/accounting/journals{Query(request)}", cancellationToken) ?? new();

    public Task<JournalEntryDto?> GetJournalByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<JournalEntryDto>($"api/accounting/journals/{id}", cancellationToken);

    public Task<JournalEntryDto?> CreateJournalAsync(CreateJournalEntryRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateJournalEntryRequest, JournalEntryDto>("api/accounting/journals", request, cancellationToken);

    public async Task<bool> SetJournalStatusAsync(Guid id, SetJournalEntryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var res = await apiClient.PostResultAsync<SetJournalEntryStatusRequest, object>($"api/accounting/journals/{id}/status", request, cancellationToken);
        return res.Succeeded;
    }

    public Task<JournalEntryDto?> ReverseJournalAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, JournalEntryDto>($"api/accounting/journals/{id}/reverse", new { }, cancellationToken);

    // Posting Profiles
    public async Task<PagedResult<PostingProfileDto>> GetPostingProfilesPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<PostingProfileDto>>($"api/accounting/posting-profiles{Query(request)}", cancellationToken) ?? new();

    public Task<PostingProfileDto?> CreatePostingProfileAsync(CreatePostingProfileRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreatePostingProfileRequest, PostingProfileDto>("api/accounting/posting-profiles", request, cancellationToken);

    // Cost Centers
    public async Task<PagedResult<CostCenterDto>> GetCostCentersPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<CostCenterDto>>($"api/accounting/cost-centers{Query(request)}", cancellationToken) ?? new();

    public Task<CostCenterDto?> CreateCostCenterAsync(CreateCostCenterRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateCostCenterRequest, CostCenterDto>("api/accounting/cost-centers", request, cancellationToken);

    public async Task<bool> SetCostCenterStatusAsync(Guid id, SetCostCenterStatusRequest request, CancellationToken cancellationToken = default)
    {
        var res = await apiClient.PostResultAsync<SetCostCenterStatusRequest, object>($"api/accounting/cost-centers/{id}/status", request, cancellationToken);
        return res.Succeeded;
    }

    // Customer & Supplier Accounts
    public async Task<PagedResult<CustomerAccountDto>> GetCustomerAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<CustomerAccountDto>>($"api/accounting/customer-accounts{Query(request)}", cancellationToken) ?? new();

    public Task<CustomerAccountDto?> CreateCustomerAccountAsync(CreateCustomerAccountRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateCustomerAccountRequest, CustomerAccountDto>("api/accounting/customer-accounts", request, cancellationToken);

    public async Task<PagedResult<SupplierAccountDto>> GetSupplierAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<SupplierAccountDto>>($"api/accounting/supplier-accounts{Query(request)}", cancellationToken) ?? new();

    public Task<SupplierAccountDto?> CreateSupplierAccountAsync(CreateSupplierAccountRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateSupplierAccountRequest, SupplierAccountDto>("api/accounting/supplier-accounts", request, cancellationToken);

    // Vouchers
    public async Task<PagedResult<ReceiptVoucherDto>> GetReceiptVouchersPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<ReceiptVoucherDto>>($"api/accounting/receipt-vouchers{Query(request)}", cancellationToken) ?? new();

    public Task<ReceiptVoucherDto?> CreateReceiptVoucherAsync(CreateReceiptVoucherRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateReceiptVoucherRequest, ReceiptVoucherDto>("api/accounting/receipt-vouchers", request, cancellationToken);

    public async Task<bool> SetReceiptVoucherStatusAsync(Guid id, SetReceiptVoucherStatusRequest request, CancellationToken cancellationToken = default)
    {
        var res = await apiClient.PostResultAsync<SetReceiptVoucherStatusRequest, object>($"api/accounting/receipt-vouchers/{id}/status", request, cancellationToken);
        return res.Succeeded;
    }

    public async Task<PagedResult<PaymentVoucherDto>> GetPaymentVouchersPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<PaymentVoucherDto>>($"api/accounting/payment-vouchers{Query(request)}", cancellationToken) ?? new();

    public Task<PaymentVoucherDto?> CreatePaymentVoucherAsync(CreatePaymentVoucherRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreatePaymentVoucherRequest, PaymentVoucherDto>("api/accounting/payment-vouchers", request, cancellationToken);

    public async Task<bool> SetPaymentVoucherStatusAsync(Guid id, SetPaymentVoucherStatusRequest request, CancellationToken cancellationToken = default)
    {
        var res = await apiClient.PostResultAsync<SetPaymentVoucherStatusRequest, object>($"api/accounting/payment-vouchers/{id}/status", request, cancellationToken);
        return res.Succeeded;
    }

    // Cash & Bank
    public async Task<PagedResult<CashAccountDto>> GetCashAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<CashAccountDto>>($"api/accounting/cash-accounts{Query(request)}", cancellationToken) ?? new();

    public Task<CashAccountDto?> CreateCashAccountAsync(CreateCashAccountRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateCashAccountRequest, CashAccountDto>("api/accounting/cash-accounts", request, cancellationToken);

    public async Task<PagedResult<BankAccountDto>> GetBankAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<BankAccountDto>>($"api/accounting/bank-accounts{Query(request)}", cancellationToken) ?? new();

    public Task<BankAccountDto?> CreateBankAccountAsync(CreateBankAccountRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateBankAccountRequest, BankAccountDto>("api/accounting/bank-accounts", request, cancellationToken);

    // Cash Shifts
    public async Task<PagedResult<CashShiftDto>> GetCashShiftsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<CashShiftDto>>($"api/accounting/cash-shifts{Query(request)}", cancellationToken) ?? new();

    public Task<CashShiftDto?> CreateCashShiftAsync(CreateCashShiftRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateCashShiftRequest, CashShiftDto>("api/accounting/cash-shifts", request, cancellationToken);

    public Task<CashShiftDto?> CloseCashShiftAsync(Guid id, SetCashShiftClosingRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<SetCashShiftClosingRequest, CashShiftDto>($"api/accounting/cash-shifts/{id}/close", request, cancellationToken);

    public Task<CashShiftDto?> ApproveCashShiftAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, CashShiftDto>($"api/accounting/cash-shifts/{id}/approve?rowVersion={Uri.EscapeDataString(rowVersion)}", new { }, cancellationToken);

    // Expenses
    public async Task<PagedResult<ExpenseDto>> GetExpensesPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<ExpenseDto>>($"api/accounting/expenses{Query(request)}", cancellationToken) ?? new();

    public Task<ExpenseDto?> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreateExpenseRequest, ExpenseDto>("api/accounting/expenses", request, cancellationToken);

    public async Task<bool> SetExpenseStatusAsync(Guid id, SetExpenseStatusRequest request, CancellationToken cancellationToken = default)
    {
        var res = await apiClient.PostResultAsync<SetExpenseStatusRequest, object>($"api/accounting/expenses/{id}/status", request, cancellationToken);
        return res.Succeeded;
    }

    public async Task<PagedResult<ExpenseTypeDto>> GetExpenseTypesPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<ExpenseTypeDto>>($"api/accounting/expenses/types{Query(request)}", cancellationToken) ?? new();

    // Payment Allocations
    public async Task<PagedResult<PaymentAllocationDto>> GetPaymentAllocationsPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<PaymentAllocationDto>>($"api/accounting/payment-allocations{Query(request)}", cancellationToken) ?? new();

    public Task<PaymentAllocationDto?> GetPaymentAllocationByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<PaymentAllocationDto>($"api/accounting/payment-allocations/{id}", cancellationToken);

    public Task<PaymentAllocationDto?> CreatePaymentAllocationAsync(CreatePaymentAllocationRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<CreatePaymentAllocationRequest, PaymentAllocationDto>("api/accounting/payment-allocations", request, cancellationToken);
}
