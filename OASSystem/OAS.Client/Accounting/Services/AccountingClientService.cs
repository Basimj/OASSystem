using OAS.Contracts.Accounting.EmployeeAccounts;
using OAS.Contracts.Accounting.Settings;
using OAS.Contracts.Accounting.ExchangeRates;
using OAS.Contracts.Accounting.Currencies;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Accounting.Customers;
using OAS.Contracts.Accounting.Common;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Accounting.Suppliers;
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

    private async Task<PagedResult<T>> GetPageAsync<T>(string route, PageRequest request, CancellationToken cancellationToken)
    {
        var pageQuery = Query(request);
        if (route.Contains('?')) pageQuery = "&" + pageQuery[1..];
        return await apiClient.GetAsync<PagedResult<T>>($"{route}{pageQuery}", cancellationToken) ?? new();
    }

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

    public Task<PagedResult<CustomerDto>> GetCustomersPageAsync(PageRequest request, string? filter = null, CancellationToken cancellationToken = default) => GetPageAsync<CustomerDto>($"api/accounting/customers{(string.IsNullOrWhiteSpace(filter) || filter == "all" ? string.Empty : $"?filter={Uri.EscapeDataString(filter)}")}", request, cancellationToken);
    public Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<CustomerDto>($"api/accounting/customers/{id}", cancellationToken);
    public Task<CustomerDto?> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateCustomerRequest, CustomerDto>("api/accounting/customers", request, cancellationToken);
    public Task<CustomerDto?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateCustomerRequest, CustomerDto>($"api/accounting/customers/{id}", request, cancellationToken);
    public Task<CustomerDto?> SetCustomerStatusAsync(Guid id, SetCustomerStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetCustomerStatusRequest, CustomerDto>($"api/accounting/customers/{id}/status", request, cancellationToken);
    public Task<CustomerCodeReservationDto?> ReserveCustomerCodeAsync(CancellationToken cancellationToken = default) => apiClient.PostAsync<object, CustomerCodeReservationDto>("api/accounting/customers/code/reserve", new { }, cancellationToken);
    public Task<IReadOnlyList<CustomerAccountParentDto>?> GetCustomerAccountParentsAsync(CancellationToken cancellationToken = default) => apiClient.GetAsync<IReadOnlyList<CustomerAccountParentDto>>("api/accounting/customers/account-parents", cancellationToken);
    public Task<IReadOnlyList<CustomerLookupDto>?> LookupCustomersAsync(string? search, CancellationToken cancellationToken = default) => apiClient.GetAsync<IReadOnlyList<CustomerLookupDto>>($"api/accounting/customers/lookup?search={Uri.EscapeDataString(search ?? string.Empty)}", cancellationToken);

    public Task<PagedResult<SupplierDto>> GetSuppliersPageAsync(PageRequest request, string? filter = null, CancellationToken cancellationToken = default) => GetPageAsync<SupplierDto>($"api/accounting/suppliers{(string.IsNullOrWhiteSpace(filter) || filter == "all" ? string.Empty : $"?filter={Uri.EscapeDataString(filter)}")}", request, cancellationToken);
    public Task<SupplierDto?> GetSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<SupplierDto>($"api/accounting/suppliers/{id}", cancellationToken);
    public Task<SupplierDto?> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateSupplierRequest, SupplierDto>("api/accounting/suppliers", request, cancellationToken);
    public Task<SupplierDto?> UpdateSupplierAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateSupplierRequest, SupplierDto>($"api/accounting/suppliers/{id}", request, cancellationToken);
    public Task<SupplierDto?> SetSupplierStatusAsync(Guid id, SetSupplierStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetSupplierStatusRequest, SupplierDto>($"api/accounting/suppliers/{id}/status", request, cancellationToken);
    public Task<SupplierCodeReservationDto?> ReserveSupplierCodeAsync(CancellationToken cancellationToken = default) => apiClient.PostAsync<object, SupplierCodeReservationDto>("api/accounting/suppliers/code/reserve", new { }, cancellationToken);
    public Task<IReadOnlyList<SupplierAccountParentDto>?> GetSupplierAccountParentsAsync(CancellationToken cancellationToken = default) => apiClient.GetAsync<IReadOnlyList<SupplierAccountParentDto>>("api/accounting/suppliers/account-parents", cancellationToken);
    public Task<IReadOnlyList<SupplierLookupDto>?> LookupSuppliersAsync(string? search, CancellationToken cancellationToken = default) => apiClient.GetAsync<IReadOnlyList<SupplierLookupDto>>($"api/accounting/suppliers/lookup?search={Uri.EscapeDataString(search ?? string.Empty)}", cancellationToken);

    public Task<AccountingNumberReservationDto?> ReserveJournalNumberAsync(DateOnly postingDate, CancellationToken cancellationToken = default) => apiClient.PostAsync<object, AccountingNumberReservationDto>($"api/accounting/journals/number/reserve?postingDate={postingDate:yyyy-MM-dd}", new { }, cancellationToken);
    public Task<AccountingNumberReservationDto?> ReserveReceiptVoucherNumberAsync(DateOnly voucherDate, CancellationToken cancellationToken = default) => apiClient.PostAsync<object, AccountingNumberReservationDto>($"api/accounting/receipt-vouchers/number/reserve?voucherDate={voucherDate:yyyy-MM-dd}", new { }, cancellationToken);
    public Task<AccountingNumberReservationDto?> ReservePaymentVoucherNumberAsync(DateOnly voucherDate, CancellationToken cancellationToken = default) => apiClient.PostAsync<object, AccountingNumberReservationDto>($"api/accounting/payment-vouchers/number/reserve?voucherDate={voucherDate:yyyy-MM-dd}", new { }, cancellationToken);
    public Task<AccountingNumberReservationDto?> ReserveExpenseNumberAsync(DateOnly expenseDate, CancellationToken cancellationToken = default) => apiClient.PostAsync<object, AccountingNumberReservationDto>($"api/accounting/expenses/number/reserve?expenseDate={expenseDate:yyyy-MM-dd}", new { }, cancellationToken);
    public Task<AccountingNumberReservationDto?> ReserveCashShiftNumberAsync(CancellationToken cancellationToken = default) => apiClient.PostAsync<object, AccountingNumberReservationDto>("api/accounting/cash-shifts/number/reserve", new { }, cancellationToken);

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
    public Task<CashAccountCodeReservationDto?> ReserveCashAccountCodeAsync(CancellationToken cancellationToken = default) => apiClient.PostAsync<object, CashAccountCodeReservationDto>("api/accounting/cash-accounts/code/reserve", new { }, cancellationToken);
    public Task<CashAccountDto?> UpdateCashAccountAsync(Guid id, UpdateCashAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateCashAccountRequest, CashAccountDto>($"api/accounting/cash-accounts/{id}", request, cancellationToken);
    public Task<CashAccountDto?> SetCashAccountStatusAsync(Guid id, SetCashAccountStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetCashAccountStatusRequest, CashAccountDto>($"api/accounting/cash-accounts/{id}/status", request, cancellationToken);

    public Task<PagedResult<BankAccountDto>> GetBankAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<BankAccountDto>("api/accounting/bank-accounts", request, cancellationToken);
    public Task<BankAccountDto?> GetBankAccountByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<BankAccountDto>($"api/accounting/bank-accounts/{id}", cancellationToken);
    public Task<BankAccountDto?> CreateBankAccountAsync(CreateBankAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateBankAccountRequest, BankAccountDto>("api/accounting/bank-accounts", request, cancellationToken);
    public Task<BankAccountCodeReservationDto?> ReserveBankAccountCodeAsync(CancellationToken cancellationToken = default) => apiClient.PostAsync<object, BankAccountCodeReservationDto>("api/accounting/bank-accounts/code/reserve", new { }, cancellationToken);
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

    public Task<PagedResult<CurrencyDto>> GetCurrenciesPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<CurrencyDto>("api/accounting/currencies", request, cancellationToken);
    public Task<CurrencyDto?> GetCurrencyByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<CurrencyDto>($"api/accounting/currencies/{id}", cancellationToken);
    public Task<CurrencyDto?> CreateCurrencyAsync(CreateCurrencyRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateCurrencyRequest, CurrencyDto>("api/accounting/currencies", request, cancellationToken);
    public Task<CurrencyDto?> UpdateCurrencyAsync(Guid id, UpdateCurrencyRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateCurrencyRequest, CurrencyDto>($"api/accounting/currencies/{id}", request, cancellationToken);

    public Task<PagedResult<ExchangeRateDto>> GetExchangeRatesPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<ExchangeRateDto>("api/accounting/exchange-rates", request, cancellationToken);
    public Task<ExchangeRateDto?> GetExchangeRateByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<ExchangeRateDto>($"api/accounting/exchange-rates/{id}", cancellationToken);
    public Task<ExchangeRateDto?> CreateExchangeRateAsync(CreateExchangeRateRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<CreateExchangeRateRequest, ExchangeRateDto>("api/accounting/exchange-rates", request, cancellationToken);
    public Task<ExchangeRateDto?> UpdateExchangeRateAsync(Guid id, UpdateExchangeRateRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateExchangeRateRequest, ExchangeRateDto>($"api/accounting/exchange-rates/{id}", request, cancellationToken);
    public Task<EffectiveExchangeRateDto?> GetEffectiveExchangeRateAsync(Guid currencyId, DateOnly date, OAS.Contracts.Accounting.Enums.ExchangeRateType rateType = OAS.Contracts.Accounting.Enums.ExchangeRateType.Accounting, CancellationToken cancellationToken = default) => apiClient.GetAsync<EffectiveExchangeRateDto>($"api/accounting/exchange-rates/effective?currencyId={currencyId:D}&date={date:yyyy-MM-dd}&rateType={rateType}", cancellationToken);

    public Task<AccountingSettingsDto?> GetAccountingSettingsAsync(CancellationToken cancellationToken = default) => apiClient.GetAsync<AccountingSettingsDto>("api/accounting/accounting-settings", cancellationToken);
    public Task<AccountingSettingsDto?> UpdateAccountingSettingsAsync(UpdateAccountingSettingsRequest request, CancellationToken cancellationToken = default) => apiClient.PutAsync<UpdateAccountingSettingsRequest, AccountingSettingsDto>("api/accounting/accounting-settings", request, cancellationToken);

    public Task<PagedResult<EmployeeAccountDto>> GetEmployeeAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default) => GetPageAsync<EmployeeAccountDto>("api/accounting/employee-accounts", request, cancellationToken);
    public Task<EmployeeAccountDto?> GetEmployeeAccountByIdAsync(Guid id, CancellationToken cancellationToken = default) => apiClient.GetAsync<EmployeeAccountDto>($"api/accounting/employee-accounts/{id}", cancellationToken);
    public Task<EmployeeAccountDto?> ActivateEmployeeAccountAsync(ActivateEmployeeAccountRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<ActivateEmployeeAccountRequest, EmployeeAccountDto>("api/accounting/employee-accounts/activate", request, cancellationToken);
    public Task<EmployeeAccountDto?> SetEmployeeAccountStatusAsync(Guid id, SetEmployeeAccountStatusRequest request, CancellationToken cancellationToken = default) => apiClient.PostAsync<SetEmployeeAccountStatusRequest, EmployeeAccountDto>($"api/accounting/employee-accounts/{id}/status", request, cancellationToken);
}
