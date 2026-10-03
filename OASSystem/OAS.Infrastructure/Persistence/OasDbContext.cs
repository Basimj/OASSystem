using Microsoft.EntityFrameworkCore;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Time;
using OAS.Domain.Features.Employees.Leave;
using OAS.Domain.Features.Employees.Overtime;
using OAS.Domain.Features.Employees.Loans;
using OAS.Domain.Features.Employees.Adjustments;
using OAS.Domain.Features.Employees.Settings;
using OAS.Domain.Features.Employees.Payroll;
using OAS.Domain.Features.Employees.EndOfService;
using OAS.Domain.Identity.Entities;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Infrastructure.Persistence;

public sealed class OasDbContext(DbContextOptions<OasDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserPasswordHistory> UserPasswordHistory => Set<UserPasswordHistory>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<EmployeeContract> EmployeeContracts => Set<EmployeeContract>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<EmployeeSalaryStructure> EmployeeSalaryStructures => Set<EmployeeSalaryStructure>();
    public DbSet<EmployeeSalaryStructureLine> EmployeeSalaryStructureLines => Set<EmployeeSalaryStructureLine>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<HrSettings> HrSettings => Set<HrSettings>();
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();
    public DbSet<EmployeeShiftAssignment> EmployeeShiftAssignments => Set<EmployeeShiftAssignment>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances => Set<EmployeeLeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<OvertimeRecord> OvertimeRecords => Set<OvertimeRecord>();
    public DbSet<EmployeeLoan> EmployeeLoans => Set<EmployeeLoan>();
    public DbSet<EmployeeLoanInstallment> EmployeeLoanInstallments => Set<EmployeeLoanInstallment>();
    public DbSet<EmployeeAdjustment> EmployeeAdjustments => Set<EmployeeAdjustment>();
    public DbSet<PayrollPolicy> PayrollPolicies => Set<PayrollPolicy>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<EmployeePayroll> EmployeePayrolls => Set<EmployeePayroll>();
    public DbSet<EmployeePayrollSalarySegment> EmployeePayrollSalarySegments => Set<EmployeePayrollSalarySegment>();
    public DbSet<EmployeePayrollLine> EmployeePayrollLines => Set<EmployeePayrollLine>();
    public DbSet<EndOfServiceSettlement> EndOfServiceSettlements => Set<EndOfServiceSettlement>();
    public DbSet<EndOfServiceSettlementLine> EndOfServiceSettlementLines => Set<EndOfServiceSettlementLine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<AccountingSettings> AccountingSettings => Set<AccountingSettings>();
    public DbSet<EmployeeAccount> EmployeeAccounts => Set<EmployeeAccount>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<FrameDetails> FrameDetails => Set<FrameDetails>();
    public DbSet<LensDetails> LensDetails => Set<LensDetails>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventoryTransactionLine> InventoryTransactionLines => Set<InventoryTransactionLine>();
    public DbSet<InventoryLedger> InventoryLedger => Set<InventoryLedger>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();
    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<SupplierCatalogItem> SupplierCatalogItems => Set<SupplierCatalogItem>();
    public DbSet<SupplierPriceHistory> SupplierPriceHistory => Set<SupplierPriceHistory>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestLine> PurchaseRequestLines => Set<PurchaseRequestLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseOrderLineSource> PurchaseOrderLineSources => Set<PurchaseOrderLineSource>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptLine> PurchaseReceiptLines => Set<PurchaseReceiptLine>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();
    public DbSet<PurchaseInvoiceReceiptAllocation> PurchaseInvoiceReceiptAllocations => Set<PurchaseInvoiceReceiptAllocation>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<long>("DepartmentCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EmployeeContractCodeSequence", "hr");
        modelBuilder.HasSequence<long>("SalaryComponentCodeSequence", "hr");
        modelBuilder.HasSequence<long>("SalaryStructureCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EmployeeDocumentCodeSequence", "hr");
        modelBuilder.HasSequence<long>("WorkShiftCodeSequence", "hr");
        modelBuilder.HasSequence<long>("HolidayCodeSequence", "hr");
        modelBuilder.HasSequence<long>("LeaveTypeCodeSequence", "hr");
        modelBuilder.HasSequence<long>("LeaveRequestCodeSequence", "hr");
        modelBuilder.HasSequence<long>("OvertimeCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EmployeeLoanCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EmployeeAdjustmentCodeSequence", "hr");
        modelBuilder.HasSequence<long>("PayrollPolicyCodeSequence", "hr");
        modelBuilder.HasSequence<long>("PayrollRunCodeSequence", "hr");
        modelBuilder.HasSequence<long>("EndOfServiceCodeSequence", "hr");
        modelBuilder.HasSequence<long>("PurchaseRequestCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("PurchaseOrderCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("PurchaseReceiptCodeSequence", "dbo");
        modelBuilder.HasSequence<long>("PurchaseInvoiceCodeSequence", "dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OasDbContext).Assembly);
    }
}
