using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Payroll;

public sealed class PayrollRun : AuditableEntity<Guid>
{
    private PayrollRun() { }
    private PayrollRun(Guid id, string code, Guid periodId, PayrollRunType runType, DateOnly calculationDate, DateOnly? postingDate, Guid policyId,
        Guid currencyId, string currencyCode, string? symbol, byte decimals)
    {
        if (id==Guid.Empty||periodId==Guid.Empty||policyId==Guid.Empty||currencyId==Guid.Empty||string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(currencyCode)) throw new DomainException("Payroll run identity is invalid.");
        if (!Enum.IsDefined(runType) || decimals>6) throw new DomainException("Payroll run type or currency is invalid.");
        Id=id; PayrollRunCode=code.Trim(); PayrollPeriodId=periodId; RunType=runType; Status=PayrollRunStatus.Draft; CalculationDate=calculationDate; PostingDate=postingDate;
        PayrollPolicyId=policyId; CurrencyId=currencyId; CurrencyCodeSnapshot=currencyCode.Trim().ToUpperInvariant(); CurrencySymbolSnapshot=N(symbol); CurrencyDecimalPlacesSnapshot=decimals;
    }
    public string PayrollRunCode { get; private set; }=string.Empty;
    public Guid PayrollPeriodId { get; private set; }
    public PayrollRunType RunType { get; private set; }
    public PayrollRunStatus Status { get; private set; }
    public DateOnly CalculationDate { get; private set; }
    public DateOnly? PostingDate { get; private set; }
    public Guid PayrollPolicyId { get; private set; }
    public Guid CurrencyId { get; private set; }
    public string CurrencyCodeSnapshot { get; private set; }=string.Empty;
    public string? CurrencySymbolSnapshot { get; private set; }
    public byte CurrencyDecimalPlacesSnapshot { get; private set; }
    public Guid? BaseCurrencyId { get; private set; }
    public string? BaseCurrencyCodeSnapshot { get; private set; }
    public byte? BaseCurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal? PostingExchangeRate { get; private set; }
    public DateOnly? PostingExchangeRateDate { get; private set; }
    public byte? PostingExchangeRateType { get; private set; }
    public byte? PostingExchangeRateSource { get; private set; }
    public decimal TotalGrossEarnings { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal TotalEmployerContributions { get; private set; }
    public decimal TotalNetPay { get; private set; }
    public decimal BaseGrossEarnings { get; private set; }
    public decimal BaseDeductions { get; private set; }
    public decimal BaseEmployerContributions { get; private set; }
    public decimal BaseNetPay { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public string? CalculatedBy { get; private set; }
    public DateTimeOffset? CalculatedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? PostedBy { get; private set; }
    public DateTimeOffset? PostedAtUtc { get; private set; }
    public string? ClosedBy { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; }=[];
    public static PayrollRun Create(Guid id,string code,Guid periodId,PayrollRunType runType,DateOnly calculationDate,DateOnly? postingDate,Guid policyId,Guid currencyId,string currencyCode,string? symbol,byte decimals,string? notes=null)
    { var x=new PayrollRun(id,code,periodId,runType,calculationDate,postingDate,policyId,currencyId,currencyCode,symbol,decimals); x.Notes=N(notes); return x; }
    public void MarkCalculated(decimal gross,decimal deductions,decimal employer,decimal net,string? actor,DateTimeOffset at)
    { if(Status is not (PayrollRunStatus.Draft or PayrollRunStatus.Calculated)) throw new DomainException("Only draft or calculated payroll runs can be recalculated. Reopen reviewed or approved runs first."); ValidateTotals(gross,deductions,employer,net); TotalGrossEarnings=gross;TotalDeductions=deductions;TotalEmployerContributions=employer;TotalNetPay=net;Status=PayrollRunStatus.Calculated;CalculatedBy=N(actor);CalculatedAtUtc=at;ReviewedBy=null;ReviewedAtUtc=null;ApprovedBy=null;ApprovedAtUtc=null; }
    public void Review(string? actor,DateTimeOffset at){ if(Status!=PayrollRunStatus.Calculated)throw new DomainException("Only calculated payroll runs can be reviewed.");Status=PayrollRunStatus.Reviewed;ReviewedBy=N(actor);ReviewedAtUtc=at; }
    public void Approve(string? actor,DateTimeOffset at){ if(Status!=PayrollRunStatus.Reviewed)throw new DomainException("Only reviewed payroll runs can be approved.");Status=PayrollRunStatus.Approved;ApprovedBy=N(actor);ApprovedAtUtc=at; }
    public void Reopen(){ if(Status is not(PayrollRunStatus.Reviewed or PayrollRunStatus.Approved))throw new DomainException("Only reviewed or approved payroll runs can be reopened.");Status=PayrollRunStatus.Calculated;ReviewedBy=null;ReviewedAtUtc=null;ApprovedBy=null;ApprovedAtUtc=null; }
    public void SetPostingSnapshot(DateOnly postingDate,Guid baseCurrencyId,string baseCode,byte baseDecimals,decimal rate,DateOnly rateDate,byte rateType,byte rateSource,decimal baseGross,decimal baseDeductions,decimal baseEmployer,decimal baseNet)
    { if(Status!=PayrollRunStatus.Approved)throw new DomainException("Only approved payroll runs can receive a posting snapshot."); if(baseCurrencyId==Guid.Empty||string.IsNullOrWhiteSpace(baseCode)||rate<=0||baseDecimals>6)throw new DomainException("Payroll posting currency snapshot is invalid."); PostingDate=postingDate;BaseCurrencyId=baseCurrencyId;BaseCurrencyCodeSnapshot=baseCode.Trim().ToUpperInvariant();BaseCurrencyDecimalPlacesSnapshot=baseDecimals;PostingExchangeRate=rate;PostingExchangeRateDate=rateDate;PostingExchangeRateType=rateType;PostingExchangeRateSource=rateSource;BaseGrossEarnings=baseGross;BaseDeductions=baseDeductions;BaseEmployerContributions=baseEmployer;BaseNetPay=baseNet; }
    public void MarkPosted(Guid journalId,string? actor,DateTimeOffset at){ if(Status!=PayrollRunStatus.Approved||journalId==Guid.Empty)throw new DomainException("Only approved payroll runs can be posted.");JournalEntryId=journalId;Status=PayrollRunStatus.Posted;PostedBy=N(actor);PostedAtUtc=at; }
    public void Close(string? actor,DateTimeOffset at){ if(Status!=PayrollRunStatus.Posted)throw new DomainException("Only posted payroll runs can be closed.");Status=PayrollRunStatus.Closed;ClosedBy=N(actor);ClosedAtUtc=at; }
    public void Cancel(){ if(Status is PayrollRunStatus.Posted or PayrollRunStatus.Closed)throw new DomainException("Posted payroll runs cannot be cancelled.");Status=PayrollRunStatus.Cancelled; }
    private static void ValidateTotals(decimal gross,decimal deductions,decimal employer,decimal net){ if(gross<0||deductions<0||employer<0||net<0||net!=gross-deductions)throw new DomainException("Payroll totals are invalid."); }
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
