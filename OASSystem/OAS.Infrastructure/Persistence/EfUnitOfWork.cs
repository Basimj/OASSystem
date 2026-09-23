using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;

namespace OAS.Infrastructure.Persistence;

public sealed class EfUnitOfWork(OasDbContext dbContext) : IUnitOfWork
{
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(
                "The record was changed by another operation. Reload it and try again.",
                ex);
        }
        catch (DbUpdateException ex) when (TryMapUniqueConstraint(ex, out var conflict))
        {
            throw conflict;
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (dbContext.Database.CurrentTransaction is not null)
        {
            var nestedResult = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            return nestedResult;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool TryMapUniqueConstraint(DbUpdateException exception, out ConflictException conflict)
    {
        conflict = null!;
        if (exception.InnerException is not SqlException sqlException ||
            sqlException.Number is not (UniqueConstraintViolation or UniqueIndexViolation))
        {
            return false;
        }

        var message = sqlException.Message;
        if (message.Contains("IX_Users_NormalizedUserName", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("identity_username_exists", "User name already exists.");
            return true;
        }

        if (message.Contains("IX_Users_NormalizedEmail", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("identity_email_exists", "Email already exists.");
            return true;
        }

        if (message.Contains("IX_UserRoles_UserId_RoleId", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("identity_user_role_exists", "The role is already assigned to the user.");
            return true;
        }

        if (message.Contains("IX_Roles_NormalizedName", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("identity_role_exists", "Role already exists.");
            return true;
        }

        if (message.Contains("UX_Accounts_Code", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_account_code_exists", "An account with this code already exists.");
            return true;
        }

        if (message.Contains("UX_FiscalYears_Code", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_fiscal_year_code_exists", "A fiscal year with this code already exists.");
            return true;
        }

        if (message.Contains("UX_FiscalPeriods_FiscalYearId_PeriodNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_fiscal_period_number_exists", "This period number already exists in the selected fiscal year.");
            return true;
        }

        if (message.Contains("UX_CostCenters_Code", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_cost_center_code_exists", "A cost center with this code already exists.");
            return true;
        }

        if (message.Contains("UX_JournalEntries_JournalNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_journal_number_exists", "A journal entry with this number already exists.");
            return true;
        }

        if (message.Contains("UX_PostingProfiles_Code", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_posting_profile_code_exists", "A posting profile with this code already exists.");
            return true;
        }

        if (message.Contains("UX_CashAccounts_Code", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_cash_account_code_exists", "A cash account with this code already exists.");
            return true;
        }

        if (message.Contains("UX_BankAccounts_AccountNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_bank_account_number_exists", "رقم الحساب البنكي مستخدم مسبقاً.");
            return true;
        }

        if (message.Contains("UX_BankAccounts_Code", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_bank_account_code_exists", "A bank account with this code already exists.");
            return true;
        }

        if (message.Contains("UX_Customers_CustomerCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_customer_code_duplicate", "Customer code is already in use.");
            return true;
        }

        if (message.Contains("UX_Customers_AccountId", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_customer_account_duplicate", "This accounting account is already assigned to a customer.");
            return true;
        }

        if (message.Contains("UX_Customers_NationalId", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_customer_national_id_duplicate", "National id is already in use.");
            return true;
        }

        if (message.Contains("UX_Customers_TaxNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_customer_tax_number_duplicate", "Tax number is already in use.");
            return true;
        }

        if (message.Contains("UX_Customers_CommercialRegistrationNo", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_customer_cr_duplicate", "Commercial registration number is already in use.");
            return true;
        }

        if (message.Contains("UX_Suppliers_SupplierCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_supplier_code_duplicate", "Supplier code is already in use.");
            return true;
        }

        if (message.Contains("UX_Suppliers_AccountId", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_supplier_account_duplicate", "This accounting account is already assigned to a supplier.");
            return true;
        }

        if (message.Contains("UX_Suppliers_NationalId", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_supplier_national_id_duplicate", "National id is already in use.");
            return true;
        }

        if (message.Contains("UX_Suppliers_TaxNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_supplier_tax_number_duplicate", "Tax number is already in use.");
            return true;
        }

        if (message.Contains("UX_Suppliers_CommercialRegistrationNo", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_supplier_cr_duplicate", "Commercial registration number is already in use.");
            return true;
        }

        if (message.Contains("UX_ReceiptVouchers_VoucherNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_receipt_number_exists", "A receipt voucher with this number already exists.");
            return true;
        }

        if (message.Contains("UX_PaymentVouchers_VoucherNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_payment_number_exists", "A payment voucher with this number already exists.");
            return true;
        }

        if (message.Contains("UX_CashShifts_ShiftNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_cash_shift_number_exists", "A cash shift with this number already exists.");
            return true;
        }

        if (message.Contains("UX_ExpenseTypes_Code", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_expense_type_code_exists", "An expense type with this code already exists.");
            return true;
        }

        if (message.Contains("UX_Expenses_ExpenseNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("accounting_expense_number_exists", "An expense with this number already exists.");
            return true;
        }

        if (message.Contains("UX_Warehouses_ActiveDefault", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("inventory_default_warehouse_exists", "يوجد مخزن افتراضي فعال بالفعل. لا يمكن تعيين أكثر من مخزن افتراضي فعال.");
            return true;
        }

        if (message.Contains("UX_Products_ProductCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("inventory_product_code_exists", "كود المنتج مستخدم مسبقاً.");
            return true;
        }

        if (message.Contains("UX_ProductVariants_SKU", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("inventory_variant_sku_exists", "SKU مستخدم مسبقاً.");
            return true;
        }

        if (message.Contains("UX_ProductVariants_Barcode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("inventory_variant_barcode_exists", "الباركود مستخدم مسبقاً.");
            return true;
        }

        conflict = new ConflictException("unique_constraint_conflict", "A unique value already exists.");
        return true;
    }
}
