using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.Payroll;

public sealed class EmployeePayrollSalarySegment : AuditableEntity<Guid>
{
    private EmployeePayrollSalarySegment() { }
    private EmployeePayrollSalarySegment(Guid id,Guid payrollId,Guid? contractId,string? contractCode,Guid structureId,string structureCode,DateOnly from,DateOnly to,decimal basicRate,decimal proration)
    { if(id==Guid.Empty||payrollId==Guid.Empty||structureId==Guid.Empty||to<from||basicRate<0||proration<0||proration>1)throw new DomainException("Payroll salary segment is invalid.");Id=id;EmployeePayrollId=payrollId;ContractId=contractId;ContractCodeSnapshot=N(contractCode);SalaryStructureId=structureId;SalaryStructureCodeSnapshot=Required(structureCode);EffectiveFrom=from;EffectiveTo=to;BasicSalaryRateSnapshot=basicRate;ProrationFactor=proration; }
    public Guid EmployeePayrollId { get; private set; }
    public Guid? ContractId { get; private set; }
    public string? ContractCodeSnapshot { get; private set; }
    public Guid SalaryStructureId { get; private set; }
    public string SalaryStructureCodeSnapshot { get; private set; }=string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly EffectiveTo { get; private set; }
    public decimal BasicSalaryRateSnapshot { get; private set; }
    public decimal ProrationFactor { get; private set; }
    public byte[] RowVersion { get; private set; }=[];
    public static EmployeePayrollSalarySegment Create(Guid id,Guid payrollId,Guid? contractId,string? contractCode,Guid structureId,string structureCode,DateOnly from,DateOnly to,decimal basicRate,decimal proration)=>new(id,payrollId,contractId,contractCode,structureId,structureCode,from,to,basicRate,proration);
    private static string Required(string v)=>string.IsNullOrWhiteSpace(v)?throw new DomainException("Segment structure code is required."):v.Trim();
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
