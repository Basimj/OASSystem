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



        if (message.Contains("UX_Departments_DepartmentCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("hr_department_code_exists", "Department code already exists.");
            return true;
        }

        if (message.Contains("UX_EmployeeContracts_ContractCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("hr_contract_code_exists", "Contract code already exists.");
            return true;
        }

        if (message.Contains("UX_EmployeeContracts_Employee_Active", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("contract_active_exists", "The employee already has an active contract.");
            return true;
        }

        if (message.Contains("UX_SalaryComponents_ComponentCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("hr_salary_component_code_exists", "Salary component code already exists.");
            return true;
        }

        if (message.Contains("UX_SalaryComponents_ActiveBasicSalary", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("salary_component_basic_exists", "An active basic salary component already exists.");
            return true;
        }

        if (message.Contains("UX_EmployeeSalaryStructures_StructureCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("hr_salary_structure_code_exists", "Salary structure code already exists.");
            return true;
        }

        if (message.Contains("UX_EmployeeSalaryStructures_Employee_Active", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("salary_structure_active_exists", "The employee already has an active salary structure.");
            return true;
        }

        if (message.Contains("UX_EmployeeSalaryStructureLines_Structure_Component", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("salary_structure_component_duplicate", "The salary structure contains the same component more than once.");
            return true;
        }

        if (message.Contains("UX_EmployeeDocuments_DocumentCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("hr_employee_document_code_exists", "Employee document code already exists.");
            return true;
        }

        if (message.Contains("UX_WorkShifts_ShiftCode", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("hr_shift_code_exists", "Shift code already exists."); return true; }
        if (message.Contains("UX_Holidays_HolidayCode", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("hr_holiday_code_exists", "Holiday code already exists."); return true; }
        if (message.Contains("UX_Attendance_Employee_Date", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("attendance_exists", "Attendance already exists for this employee and date."); return true; }
        if (message.Contains("UX_LeaveTypes_Code", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("hr_leave_type_code_exists", "Leave type code already exists."); return true; }
        if (message.Contains("UX_LeaveBalances_Employee_Type_Year", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("leave_balance_exists", "Leave balance already exists for this employee, leave type and leave year."); return true; }
        if (message.Contains("UX_LeaveRequests_Code", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("hr_leave_request_code_exists", "Leave request code already exists."); return true; }
        if (message.Contains("UX_Overtime_Code", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("hr_overtime_code_exists", "Overtime code already exists."); return true; }
        if (message.Contains("UX_Overtime_AttendanceRecord", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("overtime_attendance_exists", "Overtime already exists for this attendance record."); return true; }
        if (message.Contains("UX_EmployeeLoans_LoanCode", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("hr_employee_loan_code_exists", "Employee loan code already exists."); return true; }
        if (message.Contains("UX_EmployeeLoans_PaymentVoucher", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("employee_loan_voucher_duplicate", "Payment voucher is already linked to an employee loan."); return true; }
        if (message.Contains("UX_LoanInstallments_Loan_Sequence", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("loan_installment_sequence_duplicate", "Loan installment sequence already exists."); return true; }
        if (message.Contains("UX_EmployeeAdjustments_Code", StringComparison.OrdinalIgnoreCase)) { conflict = new ConflictException("hr_employee_adjustment_code_exists", "Employee adjustment code already exists."); return true; }

        if (message.Contains("UX_Prescriptions_PrescriptionCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("sales_duplicate_prescription_code", "كود الوصفة مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_CustomerOrders_OrderCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("sales_duplicate_order_code", "كود طلب العميل مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_SalesInvoices_InvoiceCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("sales_duplicate_invoice_code", "كود فاتورة المبيعات مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_PrescriptionRevisions_Current", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("UX_PrescriptionRevisions_Prescription_Revision", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("sales_prescription_revision_conflict", "تم إنشاء أو تعديل إصدار الوصفة بالتزامن. أعد تحميل الوصفة وحاول مرة أخرى.");
            return true;
        }

        if (message.Contains("UX_PrescriptionEyeDetails_Revision_Eye", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("sales_prescription_eye_duplicate", "لا يمكن تكرار نفس العين داخل إصدار الوصفة.");
            return true;
        }

        if (message.Contains("UX_CustomerOrderLines_Order_LineNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("sales_order_line_number_conflict", "حدث تعارض في ترتيب أسطر طلب العميل. أعد تحميل الطلب وحاول مرة أخرى.");
            return true;
        }

        if (message.Contains("UX_SalesInvoiceLines_Invoice_LineNumber", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("sales_invoice_line_number_conflict", "حدث تعارض في ترتيب أسطر الفاتورة. أعد تحميل الفاتورة وحاول مرة أخرى.");
            return true;
        }

        if (message.Contains("UX_SalesInvoicePrescriptionSnapshots_LineId", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("sales_prescription_snapshot_conflict", "تم حفظ Snapshot للوصفة لهذا السطر مسبقًا.");
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

        if (message.Contains("UX_SupplierCatalogItems_Supplier_ProductVariant_PurchaseUnit", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_supplier_catalog_duplicate", "هذا المورد مرتبط مسبقًا بنفس المنتج ووحدة الشراء.");
            return true;
        }

        if (message.Contains("UX_SupplierPriceHistory_Current_Catalog_Currency", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_supplier_current_price_exists", "يوجد سعر حالي فعال مسبقًا لنفس عنصر كتالوج المورد والعملة.");
            return true;
        }

        if (message.Contains("UX_PurchaseRequests_RequestCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_request_code_exists", "كود طلب الشراء مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_PurchaseRequestLines_Request_LineSequence", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_request_line_sequence_exists", "رقم سطر طلب الشراء مكرر داخل نفس الطلب.");
            return true;
        }

        if (message.Contains("UX_PurchaseOrders_PurchaseOrderCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_order_code_exists", "كود أمر الشراء مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_PurchaseOrderLines_Order_LineSequence", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_order_line_sequence_exists", "رقم سطر أمر الشراء مكرر داخل نفس الأمر.");
            return true;
        }

        if (message.Contains("UX_PurchaseOrderLineSources_OrderLine_RequestLine", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_order_line_source_duplicate", "تم ربط سطر طلب الشراء بهذا السطر من أمر الشراء مسبقًا.");
            return true;
        }

        if (message.Contains("UX_PurchaseReceipts_ReceiptCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_receipt_code_exists", "كود استلام المشتريات مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_PurchaseReceiptLines_Receipt_LineSequence", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_receipt_line_sequence_exists", "رقم سطر الاستلام مكرر داخل نفس السند.");
            return true;
        }

        if (message.Contains("UX_PurchaseInvoices_Supplier_SupplierInvoiceCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_supplier_invoice_duplicate", "فاتورة المورد مسجلة مسبقًا لنفس المورد.");
            return true;
        }

        if (message.Contains("UX_PurchaseInvoices_PurchaseInvoiceCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_invoice_code_exists", "كود فاتورة المشتريات مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_PurchaseInvoiceLines_Invoice_LineSequence", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_invoice_line_sequence_exists", "رقم سطر فاتورة المشتريات مكرر داخل نفس الفاتورة.");
            return true;
        }

        if (message.Contains("UX_PurchaseInvoiceReceiptAllocations_InvoiceLine_ReceiptLine", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchasing_match_allocation_duplicate", "تمت إضافة نفس علاقة المطابقة بين سطر الفاتورة وسطر الاستلام مسبقًا.");
            return true;
        }

        if (message.Contains("UX_SalesReturns_ReturnCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("sales_return_code_exists", "كود مرتجع المبيعات مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_PurchaseReturns_ReturnCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("purchase_return_code_exists", "كود مرتجع المشتريات مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_CommissionRules_Code", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("commission_rule_code_duplicate", "كود قاعدة العمولة مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_CommissionStatements_StatementCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("commission_statement_code_exists", "كود دورة العمولة مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_CommissionEntries_SourceLine", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("commission_source_already_processed", "تم احتساب عمولة هذا السطر مسبقًا.");
            return true;
        }

        if (message.Contains("UX_OpticalProductionJobs_JobCode", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("production_job_code_exists", "كود أمر الإنتاج مستخدم مسبقًا.");
            return true;
        }

        if (message.Contains("UX_OpticalProductionJobs_ActiveInvoiceLine", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("production_active_job_exists", "يوجد أمر إنتاج نشط لسطر الفاتورة بالفعل.");
            return true;
        }

        conflict = new ConflictException("unique_constraint_conflict", "A unique value already exists.");
        return true;
    }
}
