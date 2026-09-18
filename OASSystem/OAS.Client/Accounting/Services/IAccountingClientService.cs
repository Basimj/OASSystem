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
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Contracts.Common.Pagination;

namespace OAS.Client.Accounting.Services;

public interface IAccountingClientService
{
    // Accounts
    Task<PagedResult<AccountDto>> GetAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<AccountDto?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountDto?> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<AccountDto?> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetAccountStatusAsync(Guid id, SetAccountStatusRequest request, CancellationToken cancellationToken = default);

    // Fiscal Years & Periods
    Task<PagedResult<FiscalYearDto>> GetFiscalYearsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<FiscalYearDto?> GetFiscalYearByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FiscalYearDto?> CreateFiscalYearAsync(CreateFiscalYearRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<FiscalPeriodDto>> GetFiscalPeriodsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetPeriodLocksAsync(Guid id, SetFiscalPeriodLocksRequest request, CancellationToken cancellationToken = default);

    // Journals
    Task<PagedResult<JournalEntryDto>> GetJournalsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> GetJournalByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> CreateJournalAsync(CreateJournalEntryRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetJournalStatusAsync(Guid id, SetJournalEntryStatusRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> ReverseJournalAsync(Guid id, CancellationToken cancellationToken = default);

    // Posting Profiles
    Task<PagedResult<PostingProfileDto>> GetPostingProfilesPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<PostingProfileDto?> CreatePostingProfileAsync(CreatePostingProfileRequest request, CancellationToken cancellationToken = default);

    // Cost Centers
    Task<PagedResult<CostCenterDto>> GetCostCentersPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<CostCenterDto?> CreateCostCenterAsync(CreateCostCenterRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetCostCenterStatusAsync(Guid id, SetCostCenterStatusRequest request, CancellationToken cancellationToken = default);

    // Customer & Supplier Accounts
    Task<PagedResult<CustomerAccountDto>> GetCustomerAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<CustomerAccountDto?> CreateCustomerAccountAsync(CreateCustomerAccountRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<SupplierAccountDto>> GetSupplierAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<SupplierAccountDto?> CreateSupplierAccountAsync(CreateSupplierAccountRequest request, CancellationToken cancellationToken = default);

    // Vouchers
    Task<PagedResult<ReceiptVoucherDto>> GetReceiptVouchersPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ReceiptVoucherDto?> CreateReceiptVoucherAsync(CreateReceiptVoucherRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetReceiptVoucherStatusAsync(Guid id, SetReceiptVoucherStatusRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<PaymentVoucherDto>> GetPaymentVouchersPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVoucherDto?> CreatePaymentVoucherAsync(CreatePaymentVoucherRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetPaymentVoucherStatusAsync(Guid id, SetPaymentVoucherStatusRequest request, CancellationToken cancellationToken = default);

    // Cash & Bank
    Task<PagedResult<CashAccountDto>> GetCashAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<CashAccountDto?> CreateCashAccountAsync(CreateCashAccountRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<BankAccountDto>> GetBankAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<BankAccountDto?> CreateBankAccountAsync(CreateBankAccountRequest request, CancellationToken cancellationToken = default);

    // Cash Shifts
    Task<PagedResult<CashShiftDto>> GetCashShiftsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<CashShiftDto?> CreateCashShiftAsync(CreateCashShiftRequest request, CancellationToken cancellationToken = default);
    Task<CashShiftDto?> CloseCashShiftAsync(Guid id, SetCashShiftClosingRequest request, CancellationToken cancellationToken = default);
    Task<CashShiftDto?> ApproveCashShiftAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default);

    // Expenses
    Task<PagedResult<ExpenseDto>> GetExpensesPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseDto?> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetExpenseStatusAsync(Guid id, SetExpenseStatusRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<ExpenseTypeDto>> GetExpenseTypesPageAsync(PageRequest request, CancellationToken cancellationToken = default);
}
