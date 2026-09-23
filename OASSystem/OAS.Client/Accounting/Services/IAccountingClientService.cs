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

public interface IAccountingClientService
{
    Task<PagedResult<AccountDto>> GetAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<AccountDto?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountDto?> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<AccountDto?> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken = default);
    Task<AccountDto?> SetAccountStatusAsync(Guid id, SetAccountStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<FiscalYearDto>> GetFiscalYearsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<FiscalYearDto?> GetFiscalYearByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FiscalYearDto?> CreateFiscalYearAsync(CreateFiscalYearRequest request, CancellationToken cancellationToken = default);
    Task<FiscalYearDto?> UpdateFiscalYearAsync(Guid id, UpdateFiscalYearRequest request, CancellationToken cancellationToken = default);
    Task<FiscalYearDto?> SetFiscalYearStatusAsync(Guid id, SetFiscalYearStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<FiscalPeriodDto>> GetFiscalPeriodsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto?> GetFiscalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto?> CreateFiscalPeriodAsync(CreateFiscalPeriodRequest request, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto?> UpdateFiscalPeriodAsync(Guid id, UpdateFiscalPeriodRequest request, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto?> SetFiscalPeriodStatusAsync(Guid id, SetFiscalPeriodStatusRequest request, CancellationToken cancellationToken = default);
    Task<FiscalPeriodDto?> SetPeriodLocksAsync(Guid id, SetFiscalPeriodLocksRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<JournalEntryDto>> GetJournalsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> GetJournalByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> CreateJournalAsync(CreateJournalEntryRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> UpdateJournalAsync(Guid id, UpdateJournalEntryRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> SetJournalStatusAsync(Guid id, SetJournalEntryStatusRequest request, CancellationToken cancellationToken = default);
    Task<JournalEntryDto?> ReverseJournalAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<PostingProfileDto>> GetPostingProfilesPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<PostingProfileDto?> GetPostingProfileByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PostingProfileDto?> CreatePostingProfileAsync(CreatePostingProfileRequest request, CancellationToken cancellationToken = default);
    Task<PostingProfileDto?> UpdatePostingProfileAsync(Guid id, UpdatePostingProfileRequest request, CancellationToken cancellationToken = default);
    Task<PostingProfileDto?> SetPostingProfileStatusAsync(Guid id, SetPostingProfileStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<CostCenterDto>> GetCostCentersPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<CostCenterDto?> GetCostCenterByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CostCenterDto?> CreateCostCenterAsync(CreateCostCenterRequest request, CancellationToken cancellationToken = default);
    Task<CostCenterDto?> UpdateCostCenterAsync(Guid id, UpdateCostCenterRequest request, CancellationToken cancellationToken = default);
    Task<CostCenterDto?> SetCostCenterStatusAsync(Guid id, SetCostCenterStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<CustomerDto>> GetCustomersPageAsync(PageRequest request, string? filter = null, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto?> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto?> SetCustomerStatusAsync(Guid id, SetCustomerStatusRequest request, CancellationToken cancellationToken = default);
    Task<CustomerCodeReservationDto?> ReserveCustomerCodeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerAccountParentDto>?> GetCustomerAccountParentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerLookupDto>?> LookupCustomersAsync(string? search, CancellationToken cancellationToken = default);

    Task<PagedResult<SupplierDto>> GetSuppliersPageAsync(PageRequest request, string? filter = null, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierDto?> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto?> UpdateSupplierAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto?> SetSupplierStatusAsync(Guid id, SetSupplierStatusRequest request, CancellationToken cancellationToken = default);
    Task<SupplierCodeReservationDto?> ReserveSupplierCodeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierAccountParentDto>?> GetSupplierAccountParentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierLookupDto>?> LookupSuppliersAsync(string? search, CancellationToken cancellationToken = default);

    Task<AccountingNumberReservationDto?> ReserveJournalNumberAsync(DateOnly postingDate, CancellationToken cancellationToken = default);
    Task<AccountingNumberReservationDto?> ReserveReceiptVoucherNumberAsync(DateOnly voucherDate, CancellationToken cancellationToken = default);
    Task<AccountingNumberReservationDto?> ReservePaymentVoucherNumberAsync(DateOnly voucherDate, CancellationToken cancellationToken = default);
    Task<AccountingNumberReservationDto?> ReserveExpenseNumberAsync(DateOnly expenseDate, CancellationToken cancellationToken = default);
    Task<AccountingNumberReservationDto?> ReserveCashShiftNumberAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<ReceiptVoucherDto>> GetReceiptVouchersPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ReceiptVoucherDto?> GetReceiptVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReceiptVoucherDto?> CreateReceiptVoucherAsync(CreateReceiptVoucherRequest request, CancellationToken cancellationToken = default);
    Task<ReceiptVoucherDto?> UpdateReceiptVoucherAsync(Guid id, UpdateReceiptVoucherRequest request, CancellationToken cancellationToken = default);
    Task<ReceiptVoucherDto?> SetReceiptVoucherStatusAsync(Guid id, SetReceiptVoucherStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<PaymentVoucherDto>> GetPaymentVouchersPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVoucherDto?> GetPaymentVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentVoucherDto?> CreatePaymentVoucherAsync(CreatePaymentVoucherRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVoucherDto?> UpdatePaymentVoucherAsync(Guid id, UpdatePaymentVoucherRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVoucherDto?> SetPaymentVoucherStatusAsync(Guid id, SetPaymentVoucherStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<CashAccountDto>> GetCashAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<CashAccountDto?> GetCashAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CashAccountDto?> CreateCashAccountAsync(CreateCashAccountRequest request, CancellationToken cancellationToken = default);
    Task<CashAccountDto?> UpdateCashAccountAsync(Guid id, UpdateCashAccountRequest request, CancellationToken cancellationToken = default);
    Task<CashAccountDto?> SetCashAccountStatusAsync(Guid id, SetCashAccountStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<BankAccountDto>> GetBankAccountsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<BankAccountDto?> GetBankAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BankAccountDto?> CreateBankAccountAsync(CreateBankAccountRequest request, CancellationToken cancellationToken = default);
    Task<BankAccountDto?> UpdateBankAccountAsync(Guid id, UpdateBankAccountRequest request, CancellationToken cancellationToken = default);
    Task<BankAccountDto?> SetBankAccountStatusAsync(Guid id, SetBankAccountStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<CashShiftDto>> GetCashShiftsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<CashShiftDto?> GetCashShiftByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CashShiftDto?> CreateCashShiftAsync(CreateCashShiftRequest request, CancellationToken cancellationToken = default);
    Task<CashShiftDto?> CloseCashShiftAsync(Guid id, SetCashShiftClosingRequest request, CancellationToken cancellationToken = default);
    Task<CashShiftDto?> ApproveCashShiftAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default);

    Task<PagedResult<ExpenseDto>> GetExpensesPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseDto?> GetExpenseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseDto?> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseDto?> UpdateExpenseAsync(Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseDto?> SetExpenseStatusAsync(Guid id, SetExpenseStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<ExpenseTypeDto>> GetExpenseTypesPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseTypeDto?> GetExpenseTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseTypeDto?> CreateExpenseTypeAsync(CreateExpenseTypeRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseTypeDto?> UpdateExpenseTypeAsync(Guid id, UpdateExpenseTypeRequest request, CancellationToken cancellationToken = default);
    Task<ExpenseTypeDto?> SetExpenseTypeStatusAsync(Guid id, SetExpenseTypeStatusRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<PaymentAllocationDto>> GetPaymentAllocationsPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<PaymentAllocationDto?> GetPaymentAllocationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentAllocationDto?> CreatePaymentAllocationAsync(CreatePaymentAllocationRequest request, CancellationToken cancellationToken = default);
    Task<PaymentAllocationDto?> UpdatePaymentAllocationAsync(Guid id, UpdatePaymentAllocationRequest request, CancellationToken cancellationToken = default);
}
