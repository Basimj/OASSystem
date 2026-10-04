using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.EndOfService;

public sealed class EndOfServiceSettlement : AuditableEntity<Guid>
{
    private EndOfServiceSettlement() { }
    private EndOfServiceSettlement(Guid id,string code,Guid employeeId,Guid? contractId,Guid? finalPayrollId,DateOnly lastWorkingDate,Guid currencyId,string currencyCode,string? symbol,byte decimals,string? reason)
    { if(id==Guid.Empty||employeeId==Guid.Empty||currencyId==Guid.Empty||string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(currencyCode)||decimals>6)throw new DomainException("End-of-service identity is invalid.");Id=id;SettlementCode=code.Trim();EmployeeId=employeeId;ContractId=Normalize(contractId);FinalEmployeePayrollId=Normalize(finalPayrollId);LastWorkingDate=lastWorkingDate;CurrencyId=currencyId;CurrencyCodeSnapshot=currencyCode.Trim().ToUpperInvariant();CurrencySymbolSnapshot=N(symbol);CurrencyDecimalPlacesSnapshot=decimals;Reason=N(reason);Status=EndOfServiceStatus.Draft; }
    public string SettlementCode { get; private set; }=string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid? ContractId { get; private set; }
    public Guid? FinalEmployeePayrollId { get; private set; }
    public DateOnly LastWorkingDate { get; private set; }
    public EndOfServiceStatus Status { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; }=string.Empty;
    public string? CurrencySymbolSnapshot { get; private set; }
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal OutstandingPayrollAmountSnapshot { get; private set; }
    public decimal LeaveSettlementAmount { get; private set; }
    public decimal EndOfServiceBenefitAmount { get; private set; }
    public decimal OtherEarningsAmount { get; private set; }
    public decimal LoanDeductionAmount { get; private set; }
    public decimal OtherDeductionsAmount { get; private set; }
    public decimal GrossSettlementAmount { get; private set; }
    public decimal NetSettlementAmount { get; private set; }
    public decimal BaseGrossSettlementAmount { get; private set; }
    public decimal BaseNetSettlementAmount { get; private set; }
    public decimal? PostingExchangeRate { get; private set; }
    public DateOnly? PostingExchangeRateDate { get; private set; }
    public byte? PostingExchangeRateType { get; private set; }
    public byte? PostingExchangeRateSource { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public string? Reason { get; private set; }
    public string? CalculatedBy { get; private set; }
    public DateTimeOffset? CalculatedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? PostedBy { get; private set; }
    public DateTimeOffset? PostedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; }=[];
    public static EndOfServiceSettlement Create(Guid id,string code,Guid employeeId,Guid? contractId,Guid? finalPayrollId,DateOnly lastWorkingDate,Guid currencyId,string currencyCode,string? symbol,byte decimals,string? reason)=>new(id,code,employeeId,contractId,finalPayrollId,lastWorkingDate,currencyId,currencyCode,symbol,decimals,reason);
    public void SetCalculated(decimal outstandingPayroll,decimal leave,decimal benefit,decimal otherEarnings,decimal loanDeduction,decimal otherDeductions,string? actor,DateTimeOffset at)
    { if(Status is EndOfServiceStatus.Posted or EndOfServiceStatus.Paid)throw new DomainException("Posted settlement cannot be recalculated.");if(outstandingPayroll<0||leave<0||benefit<0||otherEarnings<0||loanDeduction<0||otherDeductions<0)throw new DomainException("End-of-service amounts cannot be negative.");OutstandingPayrollAmountSnapshot=outstandingPayroll;LeaveSettlementAmount=leave;EndOfServiceBenefitAmount=benefit;OtherEarningsAmount=otherEarnings;LoanDeductionAmount=loanDeduction;OtherDeductionsAmount=otherDeductions;GrossSettlementAmount=leave+benefit+otherEarnings;NetSettlementAmount=GrossSettlementAmount-loanDeduction-otherDeductions;if(NetSettlementAmount<0)throw new DomainException("End-of-service net settlement cannot be negative.");Status=EndOfServiceStatus.Calculated;CalculatedBy=N(actor);CalculatedAtUtc=at; }
    public void Review(string? actor,DateTimeOffset at){if(Status!=EndOfServiceStatus.Calculated)throw new DomainException("Only calculated settlements can be reviewed.");Status=EndOfServiceStatus.Reviewed;ReviewedBy=N(actor);ReviewedAtUtc=at;}
    public void Approve(string? actor,DateTimeOffset at){if(Status!=EndOfServiceStatus.Reviewed)throw new DomainException("Only reviewed settlements can be approved.");Status=EndOfServiceStatus.Approved;ApprovedBy=N(actor);ApprovedAtUtc=at;}
    public void SetPostingSnapshot(decimal rate,DateOnly rateDate,byte rateType,byte rateSource,decimal baseGross,decimal baseNet){if(Status!=EndOfServiceStatus.Approved||rate<=0||baseGross<0||baseNet<0)throw new DomainException("End-of-service posting snapshot is invalid.");PostingExchangeRate=rate;PostingExchangeRateDate=rateDate;PostingExchangeRateType=rateType;PostingExchangeRateSource=rateSource;BaseGrossSettlementAmount=baseGross;BaseNetSettlementAmount=baseNet;}
    public void MarkPosted(Guid journalId,string? actor,DateTimeOffset at){if(Status!=EndOfServiceStatus.Approved||journalId==Guid.Empty)throw new DomainException("Only approved settlements can be posted.");JournalEntryId=journalId;Status=EndOfServiceStatus.Posted;PostedBy=N(actor);PostedAtUtc=at;}
    public void MarkPaid(){if(Status!=EndOfServiceStatus.Posted)throw new DomainException("Only posted settlements can be marked paid.");Status=EndOfServiceStatus.Paid;}
    public void Cancel(){if(Status is EndOfServiceStatus.Posted or EndOfServiceStatus.Paid)throw new DomainException("Posted settlements cannot be cancelled.");Status=EndOfServiceStatus.Cancelled;}
    private static Guid? Normalize(Guid? v)=>v is { } id&&id!=Guid.Empty?id:null;
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
