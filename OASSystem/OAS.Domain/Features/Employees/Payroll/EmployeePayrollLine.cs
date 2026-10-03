using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Payroll;

public sealed class EmployeePayrollLine : AuditableEntity<Guid>
{
    private EmployeePayrollLine() { }
    private EmployeePayrollLine(Guid id,Guid payrollId,int seq,Guid? structureId,Guid? componentId,string? componentCode,string componentName,SalaryComponentType type,PayrollLineSourceType sourceType,string? sourceModule,string? sourceDocumentType,Guid? sourceDocumentId,DateOnly? sourceDate,decimal? quantity,decimal? rate,decimal amount,string? debitRole,string? creditRole,string? description)
    { if(id==Guid.Empty||payrollId==Guid.Empty||seq<=0||string.IsNullOrWhiteSpace(componentName)||amount<0||!Enum.IsDefined(type)||!Enum.IsDefined(sourceType))throw new DomainException("Payroll line is invalid.");Id=id;EmployeePayrollId=payrollId;LineSequence=seq;SalaryStructureId=Normalize(structureId);SalaryComponentId=Normalize(componentId);ComponentCodeSnapshot=N(componentCode);ComponentNameSnapshot=componentName.Trim();ComponentType=type;SourceType=sourceType;SourceModule=N(sourceModule);SourceDocumentType=N(sourceDocumentType);SourceDocumentId=Normalize(sourceDocumentId);SourceDate=sourceDate;Quantity=quantity;Rate=rate;Amount=amount;DebitPostingRole=N(debitRole);CreditPostingRole=N(creditRole);Description=N(description); }
    public Guid EmployeePayrollId { get; private set; }
    public int LineSequence { get; private set; }
    public Guid? SalaryStructureId { get; private set; }
    public Guid? SalaryComponentId { get; private set; }
    public string? ComponentCodeSnapshot { get; private set; }
    public string ComponentNameSnapshot { get; private set; }=string.Empty;
    public SalaryComponentType ComponentType { get; private set; }
    public PayrollLineSourceType SourceType { get; private set; }
    public string? SourceModule { get; private set; }
    public string? SourceDocumentType { get; private set; }
    public Guid? SourceDocumentId { get; private set; }
    public DateOnly? SourceDate { get; private set; }
    public decimal? Quantity { get; private set; }
    public decimal? Rate { get; private set; }
    public decimal Amount { get; private set; }
    public string? DebitPostingRole { get; private set; }
    public string? CreditPostingRole { get; private set; }
    public string? Description { get; private set; }
    public byte[] RowVersion { get; private set; }=[];
    public static EmployeePayrollLine Create(Guid id,Guid payrollId,int seq,Guid? structureId,Guid? componentId,string? componentCode,string componentName,SalaryComponentType type,PayrollLineSourceType sourceType,string? sourceModule,string? sourceDocumentType,Guid? sourceDocumentId,DateOnly? sourceDate,decimal? quantity,decimal? rate,decimal amount,string? debitRole,string? creditRole,string? description)
        =>new(id,payrollId,seq,structureId,componentId,componentCode,componentName,type,sourceType,sourceModule,sourceDocumentType,sourceDocumentId,sourceDate,quantity,rate,amount,debitRole,creditRole,description);
    private static Guid? Normalize(Guid? v)=>v is { } id&&id!=Guid.Empty?id:null;
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
