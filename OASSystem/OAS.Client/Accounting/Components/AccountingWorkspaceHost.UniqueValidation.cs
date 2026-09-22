using OAS.Client.Accounting.Workspace;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Contracts.Common.Pagination;

namespace OAS.Client.Accounting.Components;

public partial class AccountingWorkspaceHost
{
    private const string DuplicateCodeMessage = "هذا الكود مستخدم مسبقًا.";
    private const string DuplicateBankAccountNumberMessage = "رقم الحساب البنكي مستخدم مسبقًا.";
    private const string DuplicateFiscalPeriodMessage = "رقم الفترة مستخدم مسبقًا في السنة المالية المحددة.";
    private const string DuplicateCustomerMessage = "هذا العميل مرتبط بحساب محاسبي مسبقًا.";
    private const string DuplicateSupplierMessage = "هذا المورد مرتبط بحساب محاسبي مسبقًا.";

    /// <summary>
    /// يفحص القيود الفريدة قبل إرسال أمر الحفظ إلى قاعدة البيانات.
    /// الهدف هو منع وصول الإدخال الطبيعي المتكرر إلى SQL Unique Constraint نهائياً،
    /// وبالتالي لا يحدث first-chance exception أو توقف في المصحح.
    /// </summary>
    private async Task<bool> ValidateUniqueFieldsBeforeSaveAsync(AccountingTabState tab)
    {
        if (!tab.IsNew || tab.Model is null)
            return true;

        var valid = tab.Model switch
        {
            AccountEditor.FormModel model => await ValidateAccountUniqueAsync(model),
            FiscalYearEditor.FormModel model => await ValidateFiscalYearUniqueAsync(model),
            FiscalPeriodEditor.FormModel model => await ValidateFiscalPeriodUniqueAsync(model),
            PostingProfileEditor.FormModel model => await ValidatePostingProfileUniqueAsync(model),
            CostCenterEditor.FormModel model => await ValidateCostCenterUniqueAsync(model),
            CustomerAccountEditor.FormModel model => await ValidateCustomerAccountUniqueAsync(model),
            SupplierAccountEditor.FormModel model => await ValidateSupplierAccountUniqueAsync(model),
            CashAccountEditor.FormModel model => await ValidateCashAccountUniqueAsync(model),
            BankAccountEditor.FormModel model => await ValidateBankAccountUniqueAsync(model),
            ExpenseTypeEditor.FormModel model => await ValidateExpenseTypeUniqueAsync(model),
            _ => true
        };

        if (!valid)
        {
            Snackbar.Error("توجد بيانات مستخدمة مسبقًا. راجع الحقول المميزة باللون الأحمر.");
            Workspace.NotifyStateChanged();
        }

        return valid;
    }

    private async Task<bool> ValidateAccountUniqueAsync(AccountEditor.FormModel model)
    {
        model.CodeError = null;
        var code = model.Code.Trim();
        if (code.Length == 0) return true;

        var duplicate = _accountsPage.Items.Any(x => Same(x.Code, code)) ||
                        await ExistsExactAsync(
                            request => AccountingService.GetAccountsPageAsync(request),
                            code,
                            x => Same(x.Code, code));

        if (!duplicate) return true;
        model.CodeError = DuplicateCodeMessage;
        return false;
    }

    private async Task<bool> ValidateFiscalYearUniqueAsync(FiscalYearEditor.FormModel model)
    {
        model.CodeError = null;
        var code = model.Code.Trim();
        if (code.Length == 0) return true;

        var duplicate = _fiscalYearsPage.Items.Any(x => Same(x.Code, code)) ||
                        await ExistsExactAsync(
                            request => AccountingService.GetFiscalYearsPageAsync(request),
                            code,
                            x => Same(x.Code, code));

        if (!duplicate) return true;
        model.CodeError = DuplicateCodeMessage;
        return false;
    }

    private async Task<bool> ValidateFiscalPeriodUniqueAsync(FiscalPeriodEditor.FormModel model)
    {
        model.PeriodNumberError = null;
        if (model.FiscalYearId == Guid.Empty || model.PeriodNumber <= 0) return true;

        var duplicate = _fiscalPeriodsPage.Items.Any(x =>
                            x.FiscalYearId == model.FiscalYearId && x.PeriodNumber == model.PeriodNumber) ||
                        await ExistsExactAsync(
                            request => AccountingService.GetFiscalPeriodsPageAsync(request),
                            model.PeriodNumber.ToString(),
                            x => x.FiscalYearId == model.FiscalYearId && x.PeriodNumber == model.PeriodNumber);

        if (!duplicate) return true;
        model.PeriodNumberError = DuplicateFiscalPeriodMessage;
        return false;
    }

    private async Task<bool> ValidatePostingProfileUniqueAsync(PostingProfileEditor.FormModel model)
    {
        model.CodeError = null;
        var code = model.Code.Trim();
        if (code.Length == 0) return true;

        var duplicate = _postingProfilesPage.Items.Any(x => Same(x.Code, code)) ||
                        await ExistsExactAsync(
                            request => AccountingService.GetPostingProfilesPageAsync(request),
                            code,
                            x => Same(x.Code, code));

        if (!duplicate) return true;
        model.CodeError = DuplicateCodeMessage;
        return false;
    }

    private async Task<bool> ValidateCostCenterUniqueAsync(CostCenterEditor.FormModel model)
    {
        model.CodeError = null;
        var code = model.Code.Trim();
        if (code.Length == 0) return true;

        var duplicate = _costCentersPage.Items.Any(x => Same(x.Code, code)) ||
                        await ExistsExactAsync(
                            request => AccountingService.GetCostCentersPageAsync(request),
                            code,
                            x => Same(x.Code, code));

        if (!duplicate) return true;
        model.CodeError = DuplicateCodeMessage;
        return false;
    }

    private async Task<bool> ValidateCustomerAccountUniqueAsync(CustomerAccountEditor.FormModel model)
    {
        model.CustomerIdError = null;
        if (!model.CustomerId.HasValue) return true;

        var id = model.CustomerId.Value;
        var duplicate = _customerAccountsPage.Items.Any(x => x.CustomerId == id) ||
                        await ExistsExactAsync(
                            request => AccountingService.GetCustomerAccountsPageAsync(request),
                            id.ToString("D"),
                            x => x.CustomerId == id);

        if (!duplicate) return true;
        model.CustomerIdError = DuplicateCustomerMessage;
        return false;
    }

    private async Task<bool> ValidateSupplierAccountUniqueAsync(SupplierAccountEditor.FormModel model)
    {
        model.SupplierIdError = null;
        if (!model.SupplierId.HasValue) return true;

        var id = model.SupplierId.Value;
        var duplicate = _supplierAccountsPage.Items.Any(x => x.SupplierId == id) ||
                        await ExistsExactAsync(
                            request => AccountingService.GetSupplierAccountsPageAsync(request),
                            id.ToString("D"),
                            x => x.SupplierId == id);

        if (!duplicate) return true;
        model.SupplierIdError = DuplicateSupplierMessage;
        return false;
    }

    private async Task<bool> ValidateCashAccountUniqueAsync(CashAccountEditor.FormModel model)
    {
        model.CodeError = null;
        var code = model.Code.Trim();
        if (code.Length == 0) return true;

        var duplicate = _cashAccountsPage.Items.Any(x => Same(x.Code, code)) ||
                        await ExistsExactAsync(
                            request => AccountingService.GetCashAccountsPageAsync(request),
                            code,
                            x => Same(x.Code, code));

        if (!duplicate) return true;
        model.CodeError = DuplicateCodeMessage;
        return false;
    }

    private async Task<bool> ValidateBankAccountUniqueAsync(BankAccountEditor.FormModel model)
    {
        model.CodeError = null;
        model.AccountNumberError = null;

        var code = model.Code.Trim();
        var accountNumber = model.AccountNumber.Trim();
        var valid = true;

        if (code.Length > 0)
        {
            var duplicateCode = _bankAccountsPage.Items.Any(x => Same(x.Code, code)) ||
                                await ExistsExactAsync(
                                    request => AccountingService.GetBankAccountsPageAsync(request),
                                    code,
                                    x => Same(x.Code, code));

            if (duplicateCode)
            {
                model.CodeError = DuplicateCodeMessage;
                valid = false;
            }
        }

        if (accountNumber.Length > 0)
        {
            var duplicateNumber = _bankAccountsPage.Items.Any(x => Same(x.AccountNumber, accountNumber)) ||
                                  await ExistsExactAsync(
                                      request => AccountingService.GetBankAccountsPageAsync(request),
                                      accountNumber,
                                      x => Same(x.AccountNumber, accountNumber));

            if (duplicateNumber)
            {
                model.AccountNumberError = DuplicateBankAccountNumberMessage;
                valid = false;
            }
        }

        return valid;
    }

    private async Task<bool> ValidateExpenseTypeUniqueAsync(ExpenseTypeEditor.FormModel model)
    {
        model.CodeError = null;
        var code = model.Code.Trim();
        if (code.Length == 0) return true;

        var duplicate = _expenseTypesPage.Items.Any(x => Same(x.Code, code)) ||
                        await ExistsExactAsync(
                            request => AccountingService.GetExpenseTypesPageAsync(request),
                            code,
                            x => Same(x.Code, code));

        if (!duplicate) return true;
        model.CodeError = DuplicateCodeMessage;
        return false;
    }

    private static async Task<bool> ExistsExactAsync<T>(
        Func<PageRequest, Task<PagedResult<T>>> query,
        string search,
        Func<T, bool> predicate)
    {
        var pageNumber = 1;

        while (true)
        {
            var page = await query(new PageRequest
            {
                PageNumber = pageNumber,
                PageSize = PageRequest.MaximumPageSize,
                Search = search
            });

            if (page.Items.Any(predicate))
                return true;

            if (!page.HasNextPage)
                return false;

            pageNumber++;
        }
    }

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// حماية احتياطية فقط لسباق نادر جداً بين مستخدمين يحفظان نفس القيمة في اللحظة نفسها.
    /// الإدخال الطبيعي المتكرر يجب أن يُمنع قبل الوصول إلى قاعدة البيانات بواسطة الفحص أعلاه.
    /// </summary>
    private bool TryApplyUniqueConflictToModel(AccountingTabState tab, string? code)
    {
        switch (code)
        {
            case "accounting_account_code_exists" when tab.Model is AccountEditor.FormModel model:
                model.CodeError = DuplicateCodeMessage;
                break;

            case "accounting_fiscal_year_code_exists" when tab.Model is FiscalYearEditor.FormModel model:
                model.CodeError = DuplicateCodeMessage;
                break;

            case "accounting_fiscal_period_number_exists" when tab.Model is FiscalPeriodEditor.FormModel model:
                model.PeriodNumberError = DuplicateFiscalPeriodMessage;
                break;

            case "accounting_posting_profile_code_exists" when tab.Model is PostingProfileEditor.FormModel model:
                model.CodeError = DuplicateCodeMessage;
                break;

            case "accounting_cost_center_code_exists" when tab.Model is CostCenterEditor.FormModel model:
                model.CodeError = DuplicateCodeMessage;
                break;

            case "accounting_customer_account_exists" when tab.Model is CustomerAccountEditor.FormModel model:
                model.CustomerIdError = DuplicateCustomerMessage;
                break;

            case "accounting_supplier_account_exists" when tab.Model is SupplierAccountEditor.FormModel model:
                model.SupplierIdError = DuplicateSupplierMessage;
                break;

            case "accounting_cash_account_code_exists" when tab.Model is CashAccountEditor.FormModel model:
                model.CodeError = DuplicateCodeMessage;
                break;

            case "accounting_bank_account_code_exists" when tab.Model is BankAccountEditor.FormModel model:
                model.CodeError = DuplicateCodeMessage;
                break;

            case "accounting_bank_account_number_exists" when tab.Model is BankAccountEditor.FormModel model:
                model.AccountNumberError = DuplicateBankAccountNumberMessage;
                break;

            case "accounting_expense_type_code_exists" when tab.Model is ExpenseTypeEditor.FormModel model:
                model.CodeError = DuplicateCodeMessage;
                break;

            default:
                return false;
        }

        Snackbar.Error("توجد بيانات مستخدمة مسبقًا. راجع الحقول المميزة باللون الأحمر.");
        Workspace.NotifyStateChanged();
        return true;
    }
}
