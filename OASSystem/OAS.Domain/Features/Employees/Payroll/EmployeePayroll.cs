using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Payroll;

public sealed class EmployeePayroll : AuditableEntity<Guid>
{
    private EmployeePayroll() { }
    private EmployeePayroll(Guid id, Guid runId, Guid periodId, Guid employeeId, DateOnly coverageFrom, DateOnly coverageTo,
        string employeeCode, string employeeName, string? jobTitle, string? department, Guid? contractId, string? contractCode,
        Guid currencyId, string currencyCode, string? symbol, byte decimals, decimal configuredBasic, decimal calculatedBasic,
        decimal gross, decimal deductions, decimal employer, decimal net)
    {
        if(id==Guid.Empty||runId==Guid.Empty||periodId==Guid.Empty||employeeId==Guid.Empty||currencyId==Guid.Empty||coverageTo<coverageFrom)throw new DomainException("Employee payroll identity or coverage is invalid.");
        ValidateTotals(configuredBasic,calculatedBasic,gross,deductions,employer,net);
        Id=id;PayrollRunId=runId;PayrollPeriodId=periodId;EmployeeId=employeeId;CoverageFrom=coverageFrom;CoverageTo=coverageTo;
        EmployeeCodeSnapshot=Required(employeeCode);EmployeeNameSnapshot=Required(employeeName);JobTitleSnapshot=N(jobTitle);DepartmentSnapshot=N(department);ContractId=Normalize(contractId);ContractCodeSnapshot=N(contractCode);
        CurrencyId=currencyId;CurrencyCodeSnapshot=Required(currencyCode).ToUpperInvariant();CurrencySymbolSnapshot=N(symbol);CurrencyDecimalPlacesSnapshot=decimals;
        ConfiguredBasicSalarySnapshot=configuredBasic;CalculatedBasicSalary=calculatedBasic;GrossEarnings=gross;TotalDeductions=deductions;TotalEmployerContributions=employer;NetPay=net;Status=EmployeePayrollStatus.Calculated;
    }
    public Guid PayrollRunId { get; private set; }
    public Guid PayrollPeriodId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly CoverageFrom { get; private set; }
    public DateOnly CoverageTo { get; private set; }
    public string EmployeeCodeSnapshot { get; private set; }=string.Empty;
    public string EmployeeNameSnapshot { get; private set; }=string.Empty;
    public string? JobTitleSnapshot { get; private set; }
    public string? DepartmentSnapshot { get; private set; }
    public Guid? ContractId { get; private set; }
    public string? ContractCodeSnapshot { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; }=string.Empty;
    public string? CurrencySymbolSnapshot { get; private set; }
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal ConfiguredBasicSalarySnapshot { get; private set; }
    public decimal CalculatedBasicSalary { get; private set; }
    public decimal GrossEarnings { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal TotalEmployerContributions { get; private set; }
    public decimal NetPay { get; private set; }
    public decimal BaseGrossEarnings { get; private set; }
    public decimal BaseDeductions { get; private set; }
    public decimal BaseEmployerContributions { get; private set; }
    public decimal BaseNetPay { get; private set; }
    public EmployeePayrollStatus Status { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; }=[];
    public static EmployeePayroll Create(Guid id,Guid runId,Guid periodId,Guid employeeId,DateOnly coverageFrom,DateOnly coverageTo,string employeeCode,string employeeName,string? jobTitle,string? department,Guid? contractId,string? contractCode,Guid currencyId,string currencyCode,string? symbol,byte decimals,decimal configuredBasic,decimal calculatedBasic,decimal gross,decimal deductions,decimal employer,decimal net)
        => new(id,runId,periodId,employeeId,coverageFrom,coverageTo,employeeCode,employeeName,jobTitle,department,contractId,contractCode,currencyId,currencyCode,symbol,decimals,configuredBasic,calculatedBasic,gross,deductions,employer,net);
    public void MarkReviewed(string? actor,DateTimeOffset at){ if(Status!=EmployeePayrollStatus.Calculated)throw new DomainException("Only calculated employee payroll can be reviewed.");Status=EmployeePayrollStatus.Reviewed;ReviewedBy=N(actor);ReviewedAtUtc=at; }
    public void MarkApproved(string? actor,DateTimeOffset at){ if(Status!=EmployeePayrollStatus.Reviewed)throw new DomainException("Only reviewed employee payroll can be approved.");Status=EmployeePayrollStatus.Approved;ApprovedBy=N(actor);ApprovedAtUtc=at; }
    public void Reopen(){ if(Status is not(EmployeePayrollStatus.Reviewed or EmployeePayrollStatus.Approved))throw new DomainException("Only reviewed or approved employee payroll can be reopened.");Status=EmployeePayrollStatus.Calculated;ReviewedBy=null;ReviewedAtUtc=null;ApprovedBy=null;ApprovedAtUtc=null; }
    public void SetBaseAmounts(decimal gross,decimal deductions,decimal employer,decimal net){ if(Status!=EmployeePayrollStatus.Approved)throw new DomainException("Only approved employee payroll can receive base amounts.");if(gross<0||deductions<0||employer<0||net<0)throw new DomainException("Base payroll amounts cannot be negative.");BaseGrossEarnings=gross;BaseDeductions=deductions;BaseEmployerContributions=employer;BaseNetPay=net; }
    public void MarkPosted(){ if(Status!=EmployeePayrollStatus.Approved)throw new DomainException("Only approved employee payroll can be posted.");Status=EmployeePayrollStatus.Posted; }
    public void Cancel(){ if(Status==EmployeePayrollStatus.Posted)throw new DomainException("Posted employee payroll cannot be cancelled.");Status=EmployeePayrollStatus.Cancelled; }
    private static void ValidateTotals(decimal configuredBasic,decimal calculatedBasic,decimal gross,decimal deductions,decimal employer,decimal net){if(configuredBasic<0||calculatedBasic<0||gross<0||deductions<0||employer<0||net<0||net!=gross-deductions)throw new DomainException("Employee payroll totals are invalid.");}
    private static Guid? Normalize(Guid? v)=>v is { } id&&id!=Guid.Empty?id:null;
    private static string Required(string v)=>string.IsNullOrWhiteSpace(v)?throw new DomainException("Payroll snapshot is required."):v.Trim();
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
