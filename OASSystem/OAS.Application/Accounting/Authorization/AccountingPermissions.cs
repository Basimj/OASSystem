namespace OAS.Application.Accounting.Authorization;

public static class AccountingPermissions
{
    public static class Accounts
    {
        public const string View = "accounting.accounts.view";
        public const string Create = "accounting.accounts.create";
        public const string Edit = "accounting.accounts.edit";
        public const string Disable = "accounting.accounts.disable";
    }

    public static class FiscalYears
    {
        public const string View = "accounting.fiscal_years.view";
        public const string Create = "accounting.fiscal_years.create";
        public const string Edit = "accounting.fiscal_years.edit";
    }

    public static class FiscalPeriods
    {
        public const string View = "accounting.fiscal_periods.view";
        public const string Create = "accounting.fiscal_periods.create";
        public const string Edit = "accounting.fiscal_periods.edit";
    }

    public static class Journals
    {
        public const string View = "accounting.journals.view";
        public const string Create = "accounting.journals.create";
        public const string Edit = "accounting.journals.edit";
        public const string Approve = "accounting.journals.approve";
        public const string Post = "accounting.journals.post";
        public const string Reverse = "accounting.journals.reverse";
    }

    public static class PostingProfiles
    {
        public const string View = "accounting.posting_profiles.view";
        public const string Create = "accounting.posting_profiles.create";
        public const string Edit = "accounting.posting_profiles.edit";
        public const string Disable = "accounting.posting_profiles.disable";
    }

    public static class CostCenters
    {
        public const string View = "accounting.cost_centers.view";
        public const string Create = "accounting.cost_centers.create";
        public const string Edit = "accounting.cost_centers.edit";
        public const string Disable = "accounting.cost_centers.disable";
    }

    public static class CustomerAccounts
    {
        public const string View = "accounting.customer_accounts.view";
        public const string Create = "accounting.customer_accounts.create";
        public const string Edit = "accounting.customer_accounts.edit";
        public const string Disable = "accounting.customer_accounts.disable";
    }

    public static class SupplierAccounts
    {
        public const string View = "accounting.supplier_accounts.view";
        public const string Create = "accounting.supplier_accounts.create";
        public const string Edit = "accounting.supplier_accounts.edit";
        public const string Disable = "accounting.supplier_accounts.disable";
    }

    public static class ReceiptVouchers
    {
        public const string View = "accounting.receipt_vouchers.view";
        public const string Create = "accounting.receipt_vouchers.create";
        public const string Edit = "accounting.receipt_vouchers.edit";
        public const string Approve = "accounting.receipt_vouchers.approve";
        public const string Post = "accounting.receipt_vouchers.post";
    }

    public static class PaymentVouchers
    {
        public const string View = "accounting.payment_vouchers.view";
        public const string Create = "accounting.payment_vouchers.create";
        public const string Edit = "accounting.payment_vouchers.edit";
        public const string Approve = "accounting.payment_vouchers.approve";
        public const string Post = "accounting.payment_vouchers.post";
    }

    public static class PaymentAllocations
    {
        public const string View = "accounting.payment_allocations.view";
        public const string Create = "accounting.payment_allocations.create";
        public const string Edit = "accounting.payment_allocations.edit";
    }

    public static class CashAccounts
    {
        public const string View = "accounting.cash_accounts.view";
        public const string Create = "accounting.cash_accounts.create";
        public const string Edit = "accounting.cash_accounts.edit";
        public const string Disable = "accounting.cash_accounts.disable";
    }

    public static class BankAccounts
    {
        public const string View = "accounting.bank_accounts.view";
        public const string Create = "accounting.bank_accounts.create";
        public const string Edit = "accounting.bank_accounts.edit";
        public const string Disable = "accounting.bank_accounts.disable";
    }

    public static class CashShifts
    {
        public const string View = "accounting.cash_shifts.view";
        public const string Create = "accounting.cash_shifts.create";
        public const string Close = "accounting.cash_shifts.close";
        public const string Approve = "accounting.cash_shifts.approve";
    }

    public static class ExpenseTypes
    {
        public const string View = "accounting.expense_types.view";
        public const string Create = "accounting.expense_types.create";
        public const string Edit = "accounting.expense_types.edit";
        public const string Disable = "accounting.expense_types.disable";
    }

    public static class Expenses
    {
        public const string View = "accounting.expenses.view";
        public const string Create = "accounting.expenses.create";
        public const string Edit = "accounting.expenses.edit";
        public const string Approve = "accounting.expenses.approve";
        public const string Post = "accounting.expenses.post";
    }
}