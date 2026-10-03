using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Authorization;
using OAS.Contracts.Features.Employees.Payroll;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Features.Employees.Adjustments;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;
using OAS.Domain.Features.Employees.Loans;
using OAS.Domain.Features.Employees.Overtime;
using OAS.Domain.Features.Employees.Payroll;
using OAS.Domain.Features.Employees.Time;

namespace OAS.Application.Features.Employees.Payroll;

public sealed record GetPayrollPoliciesQuery : IQuery<IReadOnlyList<PayrollPolicyDto>>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollPolicyView]; }
public sealed record CreatePayrollPolicyCommand(CreatePayrollPolicyRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollPolicyManage]; }
public sealed record UpdatePayrollPolicyCommand(Guid Id,UpdatePayrollPolicyRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollPolicyManage]; }
public sealed record ActivatePayrollPolicyCommand(Guid Id,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollPolicyManage]; }
public sealed record CancelPayrollPolicyCommand(Guid Id,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollPolicyManage]; }

public sealed record GetPayrollPeriodsQuery : IQuery<IReadOnlyList<PayrollPeriodDto>>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollView]; }
public sealed record CreatePayrollPeriodCommand(CreatePayrollPeriodRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollCreate]; }
public sealed record LockPayrollPeriodCommand(Guid Id,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollClose]; }
public sealed record ReopenPayrollPeriodCommand(Guid Id,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollReopen]; }
public sealed record ClosePayrollPeriodCommand(Guid Id,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollClose]; }

public sealed record GetPayrollRunsQuery(Guid? PeriodId=null,byte? Status=null):IQuery<IReadOnlyList<PayrollRunDto>>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollView]; }
public sealed record CreatePayrollRunCommand(CreatePayrollRunRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollCreate]; }
public sealed record GetPayrollPrevalidationQuery(Guid RunId):IQuery<PayrollPrevalidationDto>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollView]; }
public sealed record CalculatePayrollRunCommand(Guid RunId,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollCalculate]; }
public sealed record ReviewEmployeePayrollCommand(Guid EmployeePayrollId,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollReview]; }
public sealed record ReviewPayrollRunCommand(Guid RunId,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollReview]; }
public sealed record ApprovePayrollRunCommand(Guid RunId,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollApprove]; }
public sealed record ReopenPayrollRunCommand(Guid RunId,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollReopen]; }
public sealed record PostPayrollRunCommand(Guid RunId,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollPost]; }
public sealed record ClosePayrollRunCommand(Guid RunId,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollClose]; }
public sealed record CancelPayrollRunCommand(Guid RunId,PayrollLifecycleRequest Request):ICommand<Guid>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollCreate]; }

public sealed record GetEmployeePayrollsQuery(Guid? RunId=null,Guid? EmployeeId=null):IQuery<IReadOnlyList<EmployeePayrollDto>>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollView]; }
public sealed record GetEmployeePayrollByIdQuery(Guid Id):IQuery<EmployeePayrollDto>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollView]; }
public sealed record PayEmployeePayrollCommand(Guid EmployeePayrollId,SalaryPaymentRequest Request):ICommand<SalaryPaymentResultDto>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayrollPay]; }
public sealed record GetPayslipQuery(Guid EmployeePayrollId):IQuery<PayslipDto>,IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[HrPermissions.PayslipView]; }

public sealed class CreatePayrollPolicyValidator:AbstractValidator<CreatePayrollPolicyCommand>
{
    public CreatePayrollPolicyValidator(){RuleFor(x=>x.Request.NameAr).NotEmpty().MaximumLength(150);RuleFor(x=>x.Request.ProrationMethod).InclusiveBetween((byte)1,(byte)3);RuleFor(x=>x.Request.DailyRateMethod).InclusiveBetween((byte)1,(byte)3);RuleFor(x=>x.Request.HourlyRateMethod).InclusiveBetween((byte)1,(byte)2);}
}
public sealed class CreatePayrollPeriodValidator:AbstractValidator<CreatePayrollPeriodCommand>
{
    public CreatePayrollPeriodValidator(){RuleFor(x=>x.Request.Year).GreaterThanOrEqualTo((short)1900);RuleFor(x=>x.Request.Month).InclusiveBetween((byte)1,(byte)12);RuleFor(x=>x.Request.EndDate).GreaterThanOrEqualTo(x=>x.Request.StartDate);}
}
public sealed class CreatePayrollRunValidator:AbstractValidator<CreatePayrollRunCommand>
{
    public CreatePayrollRunValidator(){RuleFor(x=>x.Request.PayrollPeriodId).NotEmpty();RuleFor(x=>x.Request.PayrollPolicyId).NotEmpty();RuleFor(x=>x.Request.CurrencyId).NotEmpty();RuleFor(x=>x.Request.RunType).InclusiveBetween((byte)1,(byte)2);}
}
public sealed class SalaryPaymentValidator:AbstractValidator<PayEmployeePayrollCommand>
{
    public SalaryPaymentValidator(){RuleFor(x=>x.Request.Amount).GreaterThan(0);RuleFor(x=>x.Request.SettlementAccountId).NotEmpty();RuleFor(x=>x.Request.PaymentMethod).GreaterThan((byte)0);}
}

internal static class PayrollMapping
{
    public static PayrollPolicyDto Policy(PayrollPolicy x)=>new(x.Id,x.PolicyCode,x.NameAr,x.EffectiveFrom,x.EffectiveTo,(byte)x.Status,(byte)x.ProrationMethod,(byte)x.DailyRateMethod,(byte)x.HourlyRateMethod,x.RequireApprovedAttendance,x.RequireFullPaymentBeforeRunClose,x.AbsenceDeductionComponentId,x.LateDeductionComponentId,x.EarlyLeaveDeductionComponentId,x.UnpaidLeaveComponentId,x.OvertimeComponentId,x.Notes,B64(x.RowVersion));
    public static PayrollPeriodDto Period(PayrollPeriod x)=>new(x.Id,x.PeriodCode,x.Year,x.Month,x.StartDate,x.EndDate,(byte)x.Status,B64(x.RowVersion));
    public static PayrollRunDto Run(PayrollRun x,string periodCode)=>new(x.Id,x.PayrollRunCode,x.PayrollPeriodId,periodCode,(byte)x.RunType,(byte)x.Status,x.CalculationDate,x.PostingDate,x.PayrollPolicyId,x.CurrencyId,x.CurrencyCodeSnapshot,x.CurrencySymbolSnapshot,x.CurrencyDecimalPlacesSnapshot,x.TotalGrossEarnings,x.TotalDeductions,x.TotalEmployerContributions,x.TotalNetPay,x.BaseNetPay,x.JournalEntryId,x.Notes,B64(x.RowVersion));
    public static EmployeePayrollDto Employee(EmployeePayroll x,IReadOnlyList<EmployeePayrollSalarySegment> segments,IReadOnlyList<EmployeePayrollLine> lines,IReadOnlyList<PaymentAllocation> allocations)
    {
        var related=allocations.Where(a=>a.TargetDocumentType==AllocationTargetDocumentType.EmployeePayroll&&a.TargetDocumentId==x.Id).ToArray();
        var paid=related.Sum(a=>a.AllocatedAmount);var outstanding=Math.Max(0m,x.NetPay-paid);var status=paid<=0?"Unpaid":outstanding>0?"PartiallyPaid":"Paid";
        return new(x.Id,x.PayrollRunId,x.PayrollPeriodId,x.EmployeeId,x.CoverageFrom,x.CoverageTo,x.EmployeeCodeSnapshot,x.EmployeeNameSnapshot,x.JobTitleSnapshot,x.DepartmentSnapshot,x.ContractId,x.ContractCodeSnapshot,x.CurrencyId,x.CurrencyCodeSnapshot,x.CurrencySymbolSnapshot,x.CurrencyDecimalPlacesSnapshot,x.ConfiguredBasicSalarySnapshot,x.CalculatedBasicSalary,x.GrossEarnings,x.TotalDeductions,x.TotalEmployerContributions,x.NetPay,x.BaseNetPay,(byte)x.Status,paid,outstanding,status,segments.OrderBy(s=>s.EffectiveFrom).Select(s=>new EmployeePayrollSalarySegmentDto(s.Id,s.ContractId,s.ContractCodeSnapshot,s.SalaryStructureId,s.SalaryStructureCodeSnapshot,s.EffectiveFrom,s.EffectiveTo,s.BasicSalaryRateSnapshot,s.ProrationFactor)).ToArray(),lines.OrderBy(l=>l.LineSequence).Select(l=>new EmployeePayrollLineDto(l.Id,l.LineSequence,l.SalaryStructureId,l.SalaryComponentId,l.ComponentCodeSnapshot,l.ComponentNameSnapshot,(byte)l.ComponentType,(byte)l.SourceType,l.SourceModule,l.SourceDocumentType,l.SourceDocumentId,l.SourceDate,l.Quantity,l.Rate,l.Amount,l.DebitPostingRole,l.CreditPostingRole,l.Description)).ToArray(),B64(x.RowVersion));
    }
    public static void CheckRowVersion(string incoming,byte[] current){byte[] value;try{value=Convert.FromBase64String(incoming);}catch(FormatException ex){throw new ConcurrencyException("Row version is invalid.",ex);}if(!value.SequenceEqual(current))throw new ConcurrencyException("Payroll data was changed by another operation. Reload and try again.");}
    private static string B64(byte[] value)=>Convert.ToBase64String(value);
}

public sealed class GetPayrollPoliciesQueryHandler(IReadRepository<PayrollPolicy,Guid> repo):IRequestHandler<GetPayrollPoliciesQuery,IReadOnlyList<PayrollPolicyDto>>
{ public async Task<IReadOnlyList<PayrollPolicyDto>> Handle(GetPayrollPoliciesQuery r,CancellationToken ct)=>(await repo.ListAsync(cancellationToken:ct)).OrderByDescending(x=>x.EffectiveFrom).Select(PayrollMapping.Policy).ToArray(); }
public sealed class CreatePayrollPolicyCommandHandler(IRepository<PayrollPolicy,Guid> repo,ISequenceNumberGenerator seq):IRequestHandler<CreatePayrollPolicyCommand,Guid>
{ public async Task<Guid> Handle(CreatePayrollPolicyCommand r,CancellationToken ct){var n=await seq.NextAsync("PayrollPolicyCodeSequence",ct);var d=r.Request;var x=PayrollPolicy.Create(Guid.NewGuid(),$"PPOL-{n:000000}",d.NameAr,d.EffectiveFrom,d.EffectiveTo,(PayrollProrationMethod)d.ProrationMethod,(PayrollDailyRateMethod)d.DailyRateMethod,(PayrollHourlyRateMethod)d.HourlyRateMethod,d.RequireApprovedAttendance,d.RequireFullPaymentBeforeRunClose,d.AbsenceDeductionComponentId,d.LateDeductionComponentId,d.EarlyLeaveDeductionComponentId,d.UnpaidLeaveComponentId,d.OvertimeComponentId,d.Notes);await repo.AddAsync(x,ct);return x.Id;} }
public sealed class UpdatePayrollPolicyCommandHandler(IRepository<PayrollPolicy,Guid> repo):IRequestHandler<UpdatePayrollPolicyCommand,Guid>
{ public async Task<Guid> Handle(UpdatePayrollPolicyCommand r,CancellationToken ct){var x=await repo.GetForUpdateAsync(r.Id,ct)??throw new NotFoundException(nameof(PayrollPolicy),r.Id);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);var d=r.Request;x.UpdateDraft(d.NameAr,d.EffectiveFrom,d.EffectiveTo,(PayrollProrationMethod)d.ProrationMethod,(PayrollDailyRateMethod)d.DailyRateMethod,(PayrollHourlyRateMethod)d.HourlyRateMethod,d.RequireApprovedAttendance,d.RequireFullPaymentBeforeRunClose,d.AbsenceDeductionComponentId,d.LateDeductionComponentId,d.EarlyLeaveDeductionComponentId,d.UnpaidLeaveComponentId,d.OvertimeComponentId,d.Notes);repo.Update(x);return x.Id;} }
public sealed class ActivatePayrollPolicyCommandHandler(IRepository<PayrollPolicy,Guid> repo,ICurrentUser user,TimeProvider time,IUnitOfWork uow):IRequestHandler<ActivatePayrollPolicyCommand,Guid>
{ public async Task<Guid> Handle(ActivatePayrollPolicyCommand r,CancellationToken ct){var x=await repo.GetForUpdateAsync(r.Id,ct)??throw new NotFoundException(nameof(PayrollPolicy),r.Id);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);var active=(await repo.ListAsync(new Specification<PayrollPolicy>().Where(p=>p.Status==PayrollPolicyStatus.Active&&p.Id!=x.Id).Tracking(),ct)).SingleOrDefault();if(active is not null){if(x.EffectiveFrom<=active.EffectiveFrom)throw new ConflictException("payroll_policy_effective_date_invalid","New payroll policy must start after the current active policy.");active.Supersede(x.EffectiveFrom.AddDays(-1));repo.Update(active);await uow.SaveChangesAsync(ct);}x.Activate(user.UserId,time.GetUtcNow());repo.Update(x);return x.Id;} }
public sealed class CancelPayrollPolicyCommandHandler(IRepository<PayrollPolicy,Guid> repo):IRequestHandler<CancelPayrollPolicyCommand,Guid>
{ public async Task<Guid> Handle(CancelPayrollPolicyCommand r,CancellationToken ct){var x=await repo.GetForUpdateAsync(r.Id,ct)??throw new NotFoundException(nameof(PayrollPolicy),r.Id);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);x.Cancel();repo.Update(x);return x.Id;} }

public sealed class GetPayrollPeriodsQueryHandler(IReadRepository<PayrollPeriod,Guid> repo):IRequestHandler<GetPayrollPeriodsQuery,IReadOnlyList<PayrollPeriodDto>>
{ public async Task<IReadOnlyList<PayrollPeriodDto>> Handle(GetPayrollPeriodsQuery r,CancellationToken ct)=>(await repo.ListAsync(cancellationToken:ct)).OrderByDescending(x=>x.StartDate).Select(PayrollMapping.Period).ToArray(); }
public sealed class CreatePayrollPeriodCommandHandler(IRepository<PayrollPeriod,Guid> repo):IRequestHandler<CreatePayrollPeriodCommand,Guid>
{ public async Task<Guid> Handle(CreatePayrollPeriodCommand r,CancellationToken ct){var d=r.Request;if(await repo.CountAsync(new Specification<PayrollPeriod>().Where(x=>x.Year==d.Year&&x.Month==d.Month),ct)>0)throw new ConflictException("payroll_period_exists","A payroll period already exists for this year and month.");var x=PayrollPeriod.Create(Guid.NewGuid(),d.Year,d.Month,d.StartDate,d.EndDate);await repo.AddAsync(x,ct);return x.Id;} }
public sealed class LockPayrollPeriodCommandHandler(IRepository<PayrollPeriod,Guid> repo,ICurrentUser user,TimeProvider time):IRequestHandler<LockPayrollPeriodCommand,Guid>
{ public async Task<Guid> Handle(LockPayrollPeriodCommand r,CancellationToken ct){var x=await repo.GetForUpdateAsync(r.Id,ct)??throw new NotFoundException(nameof(PayrollPeriod),r.Id);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);x.Lock(user.UserId,time.GetUtcNow());repo.Update(x);return x.Id;} }
public sealed class ReopenPayrollPeriodCommandHandler(IRepository<PayrollPeriod,Guid> repo):IRequestHandler<ReopenPayrollPeriodCommand,Guid>
{ public async Task<Guid> Handle(ReopenPayrollPeriodCommand r,CancellationToken ct){var x=await repo.GetForUpdateAsync(r.Id,ct)??throw new NotFoundException(nameof(PayrollPeriod),r.Id);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);x.Reopen();repo.Update(x);return x.Id;} }
public sealed class ClosePayrollPeriodCommandHandler(IRepository<PayrollPeriod,Guid> repo,IReadRepository<PayrollRun,Guid> runs,ICurrentUser user,TimeProvider time):IRequestHandler<ClosePayrollPeriodCommand,Guid>
{ public async Task<Guid> Handle(ClosePayrollPeriodCommand r,CancellationToken ct){var x=await repo.GetForUpdateAsync(r.Id,ct)??throw new NotFoundException(nameof(PayrollPeriod),r.Id);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);var open=await runs.CountAsync(new Specification<PayrollRun>().Where(v=>v.PayrollPeriodId==x.Id&&v.Status!=PayrollRunStatus.Closed&&v.Status!=PayrollRunStatus.Cancelled),ct);if(open>0)throw new ConflictException("payroll_period_open_runs","All payroll runs must be closed or cancelled before closing the payroll period.");x.Close(user.UserId,time.GetUtcNow());repo.Update(x);return x.Id;} }

public sealed class GetPayrollRunsQueryHandler(IReadRepository<PayrollRun,Guid> runs,IReadRepository<PayrollPeriod,Guid> periods):IRequestHandler<GetPayrollRunsQuery,IReadOnlyList<PayrollRunDto>>
{ public async Task<IReadOnlyList<PayrollRunDto>> Handle(GetPayrollRunsQuery r,CancellationToken ct){var rows=await runs.ListAsync(new Specification<PayrollRun>().Where(x=>(!r.PeriodId.HasValue||x.PayrollPeriodId==r.PeriodId.Value)&&(!r.Status.HasValue||(byte)x.Status==r.Status.Value)),ct);var p=(await periods.ListAsync(cancellationToken:ct)).ToDictionary(x=>x.Id);return rows.OrderByDescending(x=>x.CalculationDate).Select(x=>PayrollMapping.Run(x,p.TryGetValue(x.PayrollPeriodId,out var z)?z.PeriodCode:string.Empty)).ToArray();} }
public sealed class CreatePayrollRunCommandHandler(IRepository<PayrollRun,Guid> runs,IReadRepository<PayrollPeriod,Guid> periods,IReadRepository<PayrollPolicy,Guid> policies,IHRAccountingReferencePort accounting,ISequenceNumberGenerator seq):IRequestHandler<CreatePayrollRunCommand,Guid>
{ public async Task<Guid> Handle(CreatePayrollRunCommand r,CancellationToken ct){var d=r.Request;var period=await periods.GetByIdAsync(d.PayrollPeriodId,ct)??throw new NotFoundException(nameof(PayrollPeriod),d.PayrollPeriodId);if(period.Status!=PayrollPeriodStatus.Open)throw new ConflictException("payroll_period_not_open","New payroll runs require an open payroll period.");var policy=await policies.GetByIdAsync(d.PayrollPolicyId,ct)??throw new NotFoundException(nameof(PayrollPolicy),d.PayrollPolicyId);if(policy.Status!=PayrollPolicyStatus.Active||policy.EffectiveFrom>period.EndDate||(policy.EffectiveTo.HasValue&&policy.EffectiveTo.Value<period.StartDate))throw new ConflictException("payroll_policy_not_effective","Selected payroll policy is not effective for this period.");var currency=await accounting.GetCurrencyAsync(d.CurrencyId,ct)??throw new ConflictException("currency_not_found","Payroll currency does not exist.");if(!currency.IsActive)throw new ConflictException("currency_inactive","Payroll currency is inactive.");var type=(PayrollRunType)d.RunType;if(await runs.CountAsync(new Specification<PayrollRun>().Where(x=>x.PayrollPeriodId==period.Id&&x.CurrencyId==currency.Id&&x.RunType==type&&x.Status!=PayrollRunStatus.Cancelled),ct)>0)throw new ConflictException("payroll_run_exists","A payroll run already exists for this period, currency and run type.");var n=await seq.NextAsync("PayrollRunCodeSequence",ct);var x=PayrollRun.Create(Guid.NewGuid(),$"PRUN-{d.CalculationDate.Year:0000}-{n:000000}",period.Id,type,d.CalculationDate,d.PostingDate,policy.Id,currency.Id,currency.Code,currency.Symbol,currency.DecimalPlaces,d.Notes);await runs.AddAsync(x,ct);return x.Id;} }

public sealed class GetPayrollPrevalidationQueryHandler(IReadRepository<PayrollRun,Guid> runs,IReadRepository<PayrollPeriod,Guid> periods,IReadRepository<PayrollPolicy,Guid> policies,IEmployeePayrollCalculationService calc):IRequestHandler<GetPayrollPrevalidationQuery,PayrollPrevalidationDto>
{ public async Task<PayrollPrevalidationDto> Handle(GetPayrollPrevalidationQuery r,CancellationToken ct){var run=await runs.GetByIdAsync(r.RunId,ct)??throw new NotFoundException(nameof(PayrollRun),r.RunId);var period=await periods.GetByIdAsync(run.PayrollPeriodId,ct)??throw new NotFoundException(nameof(PayrollPeriod),run.PayrollPeriodId);var policy=await policies.GetByIdAsync(run.PayrollPolicyId,ct)??throw new NotFoundException(nameof(PayrollPolicy),run.PayrollPolicyId);var batch=await calc.PrevalidateAndCalculateAsync(run,period,policy,false,ct);var dto=batch.Issues.Select(x=>new PayrollPrevalidationIssueDto(x.EmployeeId,x.EmployeeCode,x.EmployeeName,x.Code,x.Message,x.Severity)).ToArray();return new(run.Id,!dto.Any(x=>x.Severity==(byte)PayrollIssueSeverity.Blocking),dto);} }

public sealed class CalculatePayrollRunCommandHandler(IRepository<PayrollRun,Guid> runs,IReadRepository<PayrollPeriod,Guid> periods,IReadRepository<PayrollPolicy,Guid> policies,IEmployeePayrollCalculationService calc,IRepository<EmployeePayroll,Guid> payrolls,IRepository<EmployeePayrollSalarySegment,Guid> segments,IRepository<EmployeePayrollLine,Guid> lines,ICurrentUser user,TimeProvider time):IRequestHandler<CalculatePayrollRunCommand,Guid>
{ public async Task<Guid> Handle(CalculatePayrollRunCommand r,CancellationToken ct){var run=await runs.GetForUpdateAsync(r.RunId,ct)??throw new NotFoundException(nameof(PayrollRun),r.RunId);PayrollMapping.CheckRowVersion(r.Request.RowVersion,run.RowVersion);if(run.Status is not (PayrollRunStatus.Draft or PayrollRunStatus.Calculated))throw new ConflictException("payroll_run_reopen_required","Only draft or calculated payroll can be recalculated. Reopen reviewed or approved payroll first.");var period=await periods.GetByIdAsync(run.PayrollPeriodId,ct)??throw new NotFoundException(nameof(PayrollPeriod),run.PayrollPeriodId);if(period.Status!=PayrollPeriodStatus.Open)throw new ConflictException("payroll_period_not_open","Payroll calculation and recalculation require an open payroll period.");var policy=await policies.GetByIdAsync(run.PayrollPolicyId,ct)??throw new NotFoundException(nameof(PayrollPolicy),run.PayrollPolicyId);var batch=await calc.PrevalidateAndCalculateAsync(run,period,policy,true,ct);var blocking=batch.Issues.Where(x=>x.Severity==(byte)PayrollIssueSeverity.Blocking).ToArray();if(blocking.Length>0)throw new ConflictException("payroll_prevalidation_failed",string.Join(" | ",blocking.Take(10).Select(x=>$"{x.EmployeeCode}: {x.Message}")));var old=await payrolls.ListAsync(new Specification<EmployeePayroll>().Where(x=>x.PayrollRunId==run.Id).Tracking(),ct);if(old.Any(x=>x.Status==EmployeePayrollStatus.Posted))throw new ConflictException("payroll_run_contains_posted_employee","Posted employee payroll cannot be replaced.");var oldIds=old.Select(x=>x.Id).ToHashSet();if(oldIds.Count>0){var oldLines=await lines.ListAsync(new Specification<EmployeePayrollLine>().Where(x=>oldIds.Contains(x.EmployeePayrollId)).Tracking(),ct);var oldSegments=await segments.ListAsync(new Specification<EmployeePayrollSalarySegment>().Where(x=>oldIds.Contains(x.EmployeePayrollId)).Tracking(),ct);lines.DeleteRange(oldLines);segments.DeleteRange(oldSegments);payrolls.DeleteRange(old);}decimal gross=0,deductions=0,employer=0,net=0;foreach(var c in batch.Employees){var id=Guid.NewGuid();var ep=EmployeePayroll.Create(id,run.Id,period.Id,c.EmployeeId,c.CoverageFrom,c.CoverageTo,c.EmployeeCode,c.EmployeeName,c.JobTitle,c.Department,c.ContractId,c.ContractCode,c.CurrencyId,c.CurrencyCode,c.CurrencySymbol,c.CurrencyDecimalPlaces,c.ConfiguredBasicSalary,c.CalculatedBasicSalary,c.Gross,c.Deductions,c.EmployerContributions,c.Net);await payrolls.AddAsync(ep,ct);var segs=c.Segments.Select(s=>EmployeePayrollSalarySegment.Create(Guid.NewGuid(),id,s.ContractId,s.ContractCode,s.SalaryStructureId,s.SalaryStructureCode,s.From,s.To,s.BasicRate,s.ProrationFactor)).ToArray();if(segs.Length>0)await segments.AddRangeAsync(segs,ct);var n=1;var ls=c.Lines.Select(l=>EmployeePayrollLine.Create(Guid.NewGuid(),id,n++,l.SalaryStructureId,l.SalaryComponentId,l.ComponentCode,l.ComponentName,(SalaryComponentType)l.ComponentType,(PayrollLineSourceType)l.SourceType,l.SourceModule,l.SourceDocumentType,l.SourceDocumentId,l.SourceDate,l.Quantity,l.Rate,Math.Round(l.Amount,c.CurrencyDecimalPlaces,MidpointRounding.AwayFromZero),l.DebitPostingRole,l.CreditPostingRole,l.Description)).ToArray();if(ls.Length>0)await lines.AddRangeAsync(ls,ct);gross+=c.Gross;deductions+=c.Deductions;employer+=c.EmployerContributions;net+=c.Net;}if(batch.Employees.Count==0)throw new ConflictException("payroll_no_eligible_employees","No eligible employees were found for this payroll run.");run.MarkCalculated(Math.Round(gross,run.CurrencyDecimalPlacesSnapshot),Math.Round(deductions,run.CurrencyDecimalPlacesSnapshot),Math.Round(employer,run.CurrencyDecimalPlacesSnapshot),Math.Round(net,run.CurrencyDecimalPlacesSnapshot),user.UserId,time.GetUtcNow());runs.Update(run);return run.Id;} }

public sealed class ReviewEmployeePayrollCommandHandler(IRepository<EmployeePayroll,Guid> payrolls,ICurrentUser user,TimeProvider time):IRequestHandler<ReviewEmployeePayrollCommand,Guid>
{ public async Task<Guid> Handle(ReviewEmployeePayrollCommand r,CancellationToken ct){var x=await payrolls.GetForUpdateAsync(r.EmployeePayrollId,ct)??throw new NotFoundException(nameof(EmployeePayroll),r.EmployeePayrollId);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);x.MarkReviewed(user.UserId,time.GetUtcNow());payrolls.Update(x);return x.Id;} }
public sealed class ReviewPayrollRunCommandHandler(IRepository<PayrollRun,Guid> runs,IReadRepository<EmployeePayroll,Guid> payrolls,ICurrentUser user,TimeProvider time):IRequestHandler<ReviewPayrollRunCommand,Guid>
{ public async Task<Guid> Handle(ReviewPayrollRunCommand r,CancellationToken ct){var x=await runs.GetForUpdateAsync(r.RunId,ct)??throw new NotFoundException(nameof(PayrollRun),r.RunId);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);var rows=await payrolls.ListAsync(new Specification<EmployeePayroll>().Where(p=>p.PayrollRunId==x.Id),ct);if(rows.Count==0||rows.Any(p=>p.Status!=EmployeePayrollStatus.Reviewed))throw new ConflictException("employee_payroll_review_incomplete","Every employee payroll must be reviewed before reviewing the payroll run.");x.Review(user.UserId,time.GetUtcNow());runs.Update(x);return x.Id;} }
public sealed class ApprovePayrollRunCommandHandler(IRepository<PayrollRun,Guid> runs,IRepository<EmployeePayroll,Guid> payrolls,ICurrentUser user,TimeProvider time):IRequestHandler<ApprovePayrollRunCommand,Guid>
{ public async Task<Guid> Handle(ApprovePayrollRunCommand r,CancellationToken ct){var x=await runs.GetForUpdateAsync(r.RunId,ct)??throw new NotFoundException(nameof(PayrollRun),r.RunId);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);var rows=await payrolls.ListAsync(new Specification<EmployeePayroll>().Where(p=>p.PayrollRunId==x.Id).Tracking(),ct);if(rows.Count==0||rows.Any(p=>p.Status!=EmployeePayrollStatus.Reviewed))throw new ConflictException("employee_payroll_review_incomplete","Every employee payroll must remain reviewed before approval.");foreach(var p in rows){p.MarkApproved(user.UserId,time.GetUtcNow());payrolls.Update(p);}x.Approve(user.UserId,time.GetUtcNow());runs.Update(x);return x.Id;} }
public sealed class ReopenPayrollRunCommandHandler(IRepository<PayrollRun,Guid> runs,IRepository<EmployeePayroll,Guid> payrolls):IRequestHandler<ReopenPayrollRunCommand,Guid>
{ public async Task<Guid> Handle(ReopenPayrollRunCommand r,CancellationToken ct){var x=await runs.GetForUpdateAsync(r.RunId,ct)??throw new NotFoundException(nameof(PayrollRun),r.RunId);PayrollMapping.CheckRowVersion(r.Request.RowVersion,x.RowVersion);var rows=await payrolls.ListAsync(new Specification<EmployeePayroll>().Where(p=>p.PayrollRunId==x.Id).Tracking(),ct);foreach(var p in rows.Where(p=>p.Status is EmployeePayrollStatus.Reviewed or EmployeePayrollStatus.Approved)){p.Reopen();payrolls.Update(p);}x.Reopen();runs.Update(x);return x.Id;} }

public sealed class PostPayrollRunCommandHandler(
    IRepository<PayrollRun,Guid> runs,
    IRepository<EmployeePayroll,Guid> payrolls,
    IReadRepository<EmployeePayrollLine,Guid> lines,
    IRepository<AttendanceRecord,Guid> attendance,
    IRepository<OvertimeRecord,Guid> overtime,
    IRepository<EmployeeLoanInstallment,Guid> installments,
    IRepository<EmployeeAdjustment,Guid> adjustments,
    IReadRepository<PayrollPolicy,Guid> policies,
    IApprovedCommissionSource commissions,
    IHRPayrollAccountingPort accounting,
    ICurrentUser user,
    TimeProvider time):IRequestHandler<PostPayrollRunCommand,Guid>
{
    public async Task<Guid> Handle(PostPayrollRunCommand r,CancellationToken ct)
    {
        var run=await runs.GetForUpdateAsync(r.RunId,ct)??throw new NotFoundException(nameof(PayrollRun),r.RunId);
        PayrollMapping.CheckRowVersion(r.Request.RowVersion,run.RowVersion);
        if(run.Status!=PayrollRunStatus.Approved)throw new ConflictException("payroll_run_not_approved","Payroll run must be approved before posting.");
        var payrollPolicy=await policies.GetByIdAsync(run.PayrollPolicyId,ct)??throw new ConflictException("payroll_policy_missing","Payroll policy does not exist.");
        var eps=await payrolls.ListAsync(new Specification<EmployeePayroll>().Where(x=>x.PayrollRunId==run.Id).Tracking(),ct);
        if(eps.Count==0||eps.Any(x=>x.Status!=EmployeePayrollStatus.Approved))throw new ConflictException("employee_payroll_not_approved","All employee payrolls must be approved before posting.");
        var epIds=eps.Select(x=>x.Id).ToArray();
        var allLines=await lines.ListAsync(new Specification<EmployeePayrollLine>().Where(x=>epIds.Contains(x.EmployeePayrollId)),ct);
        var byPayroll=eps.ToDictionary(x=>x.Id);
        var postingLines=allLines.Where(x=>x.Amount>0).Select(x=>
        {
            var ep=byPayroll[x.EmployeePayrollId];
            return new PayrollPostingLine(ep.EmployeeId,ep.EmployeeNameSnapshot,x.Amount,(byte)x.ComponentType,x.DebitPostingRole??string.Empty,x.CreditPostingRole??string.Empty,x.Description??x.ComponentNameSnapshot,x.SourceDocumentId);
        }).ToArray();
        var postingDate=run.PostingDate??run.CalculationDate;
        var posted=await accounting.PostAsync(new PayrollPostingRequest(run.Id,run.PayrollRunCode,postingDate,run.CurrencyId,postingLines),eps,ct);
        run.SetPostingSnapshot(postingDate,posted.BaseCurrencyId,posted.BaseCurrencyCode,posted.BaseCurrencyDecimalPlaces,posted.ExchangeRate,posted.ExchangeRateDate,posted.ExchangeRateType,posted.ExchangeRateSource,posted.BaseGross,posted.BaseDeductions,posted.BaseEmployerContributions,posted.BaseNet);
        var baseMap=posted.Employees.ToDictionary(x=>x.EmployeePayrollId);
        var now=time.GetUtcNow();
        foreach(var ep in eps)
        {
            if(!baseMap.TryGetValue(ep.Id,out var b))throw new ConflictException("employee_payroll_base_missing","Accounting posting did not return base amounts for every employee payroll.");
            ep.SetBaseAmounts(b.BaseGross,b.BaseDeductions,b.BaseEmployerContributions,b.BaseNet);
            ep.MarkPosted();
            payrolls.Update(ep);

            // Lock every approved attendance record that participated in this payroll coverage,
            // not only records that happened to create a deduction line.
            var attendanceRows=await attendance.ListAsync(new Specification<AttendanceRecord>()
                .Where(x=>x.EmployeeId==ep.EmployeeId&&x.AttendanceDate>=ep.CoverageFrom&&x.AttendanceDate<=ep.CoverageTo)
                .Tracking(),ct);
            foreach(var row in attendanceRows)
            {
                if(row.EmployeePayrollId.HasValue&&row.EmployeePayrollId.Value!=ep.Id)
                    throw new ConflictException("payroll_attendance_already_used","Attendance was consumed by another posted payroll.");
                if(!row.EmployeePayrollId.HasValue)
                {
                    row.LockToPayroll(ep.Id,payrollPolicy.RequireApprovedAttendance);
                    attendance.Update(row);
                }
            }

            var sourceLines=allLines.Where(x=>x.EmployeePayrollId==ep.Id&&x.SourceDocumentId.HasValue).ToArray();
            foreach(var line in sourceLines)
            {
                switch(line.SourceType)
                {
                    case PayrollLineSourceType.Attendance:
                    case PayrollLineSourceType.Leave:
                        break;
                    case PayrollLineSourceType.Overtime:
                    {
                        var o=await overtime.GetForUpdateAsync(line.SourceDocumentId!.Value,ct)??throw new ConflictException("payroll_overtime_missing","An overtime source disappeared before posting.");
                        o.MarkAppliedToPayroll(ep.Id);
                        overtime.Update(o);
                        break;
                    }
                    case PayrollLineSourceType.LoanInstallment:
                    {
                        var i=await installments.GetForUpdateAsync(line.SourceDocumentId!.Value,ct)??throw new ConflictException("payroll_installment_missing","A loan installment source disappeared before posting.");
                        i.MarkDeducted(ep.Id,now);
                        installments.Update(i);
                        break;
                    }
                    case PayrollLineSourceType.Adjustment:
                    {
                        var a=await adjustments.GetForUpdateAsync(line.SourceDocumentId!.Value,ct)??throw new ConflictException("payroll_adjustment_missing","An adjustment source disappeared before posting.");
                        a.MarkAppliedToPayroll(ep.Id);
                        adjustments.Update(a);
                        break;
                    }
                    case PayrollLineSourceType.Commission:
                        await commissions.MarkConsumedAsync(line.SourceDocumentId!.Value,ep.Id,ct);
                        break;
                }
            }
        }
        run.MarkPosted(posted.JournalEntryId,user.UserId,now);
        runs.Update(run);
        return run.Id;
    }
}

public sealed class ClosePayrollRunCommandHandler(IRepository<PayrollRun,Guid> runs,IReadRepository<EmployeePayroll,Guid> payrolls,IReadRepository<PaymentAllocation,Guid> allocations,IReadRepository<PayrollPolicy,Guid> policies,ICurrentUser user,TimeProvider time):IRequestHandler<ClosePayrollRunCommand,Guid>
{ public async Task<Guid> Handle(ClosePayrollRunCommand r,CancellationToken ct){var run=await runs.GetForUpdateAsync(r.RunId,ct)??throw new NotFoundException(nameof(PayrollRun),r.RunId);PayrollMapping.CheckRowVersion(r.Request.RowVersion,run.RowVersion);var policy=await policies.GetByIdAsync(run.PayrollPolicyId,ct)??throw new NotFoundException(nameof(PayrollPolicy),run.PayrollPolicyId);if(policy.RequireFullPaymentBeforeRunClose){var eps=await payrolls.ListAsync(new Specification<EmployeePayroll>().Where(x=>x.PayrollRunId==run.Id),ct);var ids=eps.Select(x=>x.Id).ToHashSet();var alloc=await allocations.ListAsync(new Specification<PaymentAllocation>().Where(x=>x.TargetDocumentType==AllocationTargetDocumentType.EmployeePayroll&&ids.Contains(x.TargetDocumentId)),ct);if(eps.Any(x=>alloc.Where(a=>a.TargetDocumentId==x.Id).Sum(a=>a.AllocatedAmount)<x.NetPay))throw new ConflictException("payroll_run_unpaid","Payroll policy requires full salary payment before run close.");}run.Close(user.UserId,time.GetUtcNow());runs.Update(run);return run.Id;} }
public sealed class CancelPayrollRunCommandHandler(IRepository<PayrollRun,Guid> runs,IRepository<EmployeePayroll,Guid> payrolls):IRequestHandler<CancelPayrollRunCommand,Guid>
{ public async Task<Guid> Handle(CancelPayrollRunCommand r,CancellationToken ct){var run=await runs.GetForUpdateAsync(r.RunId,ct)??throw new NotFoundException(nameof(PayrollRun),r.RunId);PayrollMapping.CheckRowVersion(r.Request.RowVersion,run.RowVersion);var eps=await payrolls.ListAsync(new Specification<EmployeePayroll>().Where(x=>x.PayrollRunId==run.Id).Tracking(),ct);foreach(var ep in eps){ep.Cancel();payrolls.Update(ep);}run.Cancel();runs.Update(run);return run.Id;} }

public sealed class GetEmployeePayrollsQueryHandler(IReadRepository<EmployeePayroll,Guid> payrolls,IReadRepository<EmployeePayrollSalarySegment,Guid> segments,IReadRepository<EmployeePayrollLine,Guid> lines,IReadRepository<PaymentAllocation,Guid> allocations):IRequestHandler<GetEmployeePayrollsQuery,IReadOnlyList<EmployeePayrollDto>>
{ public async Task<IReadOnlyList<EmployeePayrollDto>> Handle(GetEmployeePayrollsQuery r,CancellationToken ct){var rows=await payrolls.ListAsync(new Specification<EmployeePayroll>().Where(x=>(!r.RunId.HasValue||x.PayrollRunId==r.RunId.Value)&&(!r.EmployeeId.HasValue||x.EmployeeId==r.EmployeeId.Value)),ct);var ids=rows.Select(x=>x.Id).ToHashSet();var seg=await segments.ListAsync(new Specification<EmployeePayrollSalarySegment>().Where(x=>ids.Contains(x.EmployeePayrollId)),ct);var ln=await lines.ListAsync(new Specification<EmployeePayrollLine>().Where(x=>ids.Contains(x.EmployeePayrollId)),ct);var al=await allocations.ListAsync(new Specification<PaymentAllocation>().Where(x=>x.TargetDocumentType==AllocationTargetDocumentType.EmployeePayroll&&ids.Contains(x.TargetDocumentId)),ct);return rows.OrderBy(x=>x.EmployeeCodeSnapshot).Select(x=>PayrollMapping.Employee(x,seg.Where(s=>s.EmployeePayrollId==x.Id).ToArray(),ln.Where(l=>l.EmployeePayrollId==x.Id).ToArray(),al.Where(a=>a.TargetDocumentId==x.Id).ToArray())).ToArray();} }
public sealed class GetEmployeePayrollByIdQueryHandler(IReadRepository<EmployeePayroll,Guid> payrolls,IReadRepository<EmployeePayrollSalarySegment,Guid> segments,IReadRepository<EmployeePayrollLine,Guid> lines,IReadRepository<PaymentAllocation,Guid> allocations):IRequestHandler<GetEmployeePayrollByIdQuery,EmployeePayrollDto>
{ public async Task<EmployeePayrollDto> Handle(GetEmployeePayrollByIdQuery r,CancellationToken ct){var x=await payrolls.GetByIdAsync(r.Id,ct)??throw new NotFoundException(nameof(EmployeePayroll),r.Id);var seg=await segments.ListAsync(new Specification<EmployeePayrollSalarySegment>().Where(s=>s.EmployeePayrollId==x.Id),ct);var ln=await lines.ListAsync(new Specification<EmployeePayrollLine>().Where(l=>l.EmployeePayrollId==x.Id),ct);var al=await allocations.ListAsync(new Specification<PaymentAllocation>().Where(a=>a.TargetDocumentType==AllocationTargetDocumentType.EmployeePayroll&&a.TargetDocumentId==x.Id),ct);return PayrollMapping.Employee(x,seg,ln,al);} }
public sealed class PayEmployeePayrollCommandHandler(IReadRepository<EmployeePayroll,Guid> payrolls,IHRSalaryPaymentPort payment,IReadRepository<PaymentAllocation,Guid> allocations,IEmployeeHrOperationLock gate):IRequestHandler<PayEmployeePayrollCommand,SalaryPaymentResultDto>
{ public async Task<SalaryPaymentResultDto> Handle(PayEmployeePayrollCommand r,CancellationToken ct){var ep=await payrolls.GetByIdAsync(r.EmployeePayrollId,ct)??throw new NotFoundException(nameof(EmployeePayroll),r.EmployeePayrollId);if(ep.Status!=EmployeePayrollStatus.Posted)throw new ConflictException("employee_payroll_not_posted","Only posted payroll can be paid.");await gate.AcquireAsync(ep.EmployeeId,ct);var d=r.Request;var existing=await allocations.ListAsync(new Specification<PaymentAllocation>().Where(a=>a.TargetDocumentType==AllocationTargetDocumentType.EmployeePayroll&&a.TargetDocumentId==ep.Id),ct);var paidBefore=existing.Sum(x=>x.AllocatedAmount);var p=await payment.PayAsync(new SalaryPaymentPortRequest(ep.Id,ep.EmployeeId,ep.EmployeeNameSnapshot,d.PaymentDate,d.Amount,d.PaymentMethod,d.CashAccountId,d.BankAccountId,d.SettlementAccountId,d.ExchangeRate,d.ExchangeRateType,d.ReferenceNumber,d.Description),ct);var paid=paidBefore+d.Amount;var outstanding=Math.Max(0m,ep.NetPay-paid);return new(p.PaymentVoucherId,p.PaymentAllocationId,paid,outstanding,paid<=0?"Unpaid":outstanding>0?"PartiallyPaid":"Paid");} }
public sealed class GetPayslipQueryHandler(IMediator mediator,IReadRepository<EmployeePayroll,Guid> payrolls,IReadRepository<PayrollPeriod,Guid> periods,IReadRepository<PayrollRun,Guid> runs,IReadRepository<JournalEntry,Guid> journals,IReadRepository<PaymentAllocation,Guid> allocations,IReadRepository<PaymentVoucherLine,Guid> paymentLines,IReadRepository<PaymentVoucher,Guid> vouchers):IRequestHandler<GetPayslipQuery,PayslipDto>
{ public async Task<PayslipDto> Handle(GetPayslipQuery r,CancellationToken ct){var dto=await mediator.Send(new GetEmployeePayrollByIdQuery(r.EmployeePayrollId),ct);var ep=await payrolls.GetByIdAsync(r.EmployeePayrollId,ct)??throw new NotFoundException(nameof(EmployeePayroll),r.EmployeePayrollId);var period=await periods.GetByIdAsync(ep.PayrollPeriodId,ct)??throw new NotFoundException(nameof(PayrollPeriod),ep.PayrollPeriodId);var run=await runs.GetByIdAsync(ep.PayrollRunId,ct)??throw new NotFoundException(nameof(PayrollRun),ep.PayrollRunId);string? journalNumber=null;if(run.JournalEntryId.HasValue)journalNumber=(await journals.GetByIdAsync(run.JournalEntryId.Value,ct))?.JournalNumber;var alloc=await allocations.ListAsync(new Specification<PaymentAllocation>().Where(a=>a.TargetDocumentType==AllocationTargetDocumentType.EmployeePayroll&&a.TargetDocumentId==ep.Id),ct);var lineIds=alloc.Where(a=>a.PaymentVoucherLineId.HasValue).Select(a=>a.PaymentVoucherLineId!.Value).ToHashSet();var linesFound=await paymentLines.ListAsync(new Specification<PaymentVoucherLine>().Where(x=>lineIds.Contains(x.Id)),ct);var voucherIds=linesFound.Select(x=>x.PaymentVoucherId).ToHashSet();var refs=(await vouchers.ListAsync(new Specification<PaymentVoucher>().Where(x=>voucherIds.Contains(x.Id)),ct)).Select(x=>x.VoucherNumber).OrderBy(x=>x).ToArray();return new(dto,period.PeriodCode,journalNumber,refs);} }
