using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Identity.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.EndOfService;
using OAS.Domain.Features.Employees.Payroll;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Features.Employees.Services;

public sealed class NullApprovedCommissionSource : IApprovedCommissionSource
{
    public Task<IReadOnlyList<ApprovedCommissionItem>> GetApprovedAsync(Guid employeeId, DateOnly from, DateOnly to, Guid currencyId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ApprovedCommissionItem>>([]);
    public Task MarkConsumedAsync(Guid sourceDocumentId, Guid employeePayrollId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class HRPayrollAccountingPort(
    OasDbContext db,
    ISequenceNumberGenerator sequences,
    IExchangeRateResolver rates,
    ICurrentUser currentUser,
    TimeProvider time) : IHRPayrollAccountingPort
{
    public async Task<PayrollPostingResult> PostAsync(PayrollPostingRequest request, IReadOnlyList<EmployeePayroll> employeePayrolls, CancellationToken ct = default)
    {
        if (await db.Set<JournalEntry>().AsNoTracking().AnyAsync(x => x.SourceModule == "HR" && x.SourceDocumentType == "PayrollRun" && x.SourceDocumentId == request.PayrollRunId, ct))
            throw new ConflictException("payroll_already_posted", "A journal already exists for this payroll run.");
        var period = await ResolvePeriod(db, request.PostingDate, ct);
        var profile = await ResolveProfile(db, "PayrollRun", ct);
        var accounts = await ResolveAccounts(db, profile, request.Lines.SelectMany(x => new[] { x.DebitPostingRole, x.CreditPostingRole }), ct);
        var (settings, baseCurrency) = await ResolveBase(db, ct);
        var txCurrency = await db.Set<Currency>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.CurrencyId, ct) ?? throw new NotFoundException(nameof(Currency), request.CurrencyId);
        var rate = await rates.ResolveAsync(request.CurrencyId, request.PostingDate, settings.DefaultExchangeRateType, cancellationToken: ct);
        var journal = await CreateJournal(db, sequences, request.PostingDate, period.Id, $"Payroll {request.PayrollRunCode}", "PayrollRun", request.PayrollRunId, baseCurrency, ct);
        var n = 1;
        var baseByEmployee = employeePayrolls.ToDictionary(x => x.EmployeeId, _ => new PayrollBaseAccumulator());
        foreach (var source in request.Lines.Where(x => x.Amount > 0))
        {
            if (string.IsNullOrWhiteSpace(source.DebitPostingRole) || string.IsNullOrWhiteSpace(source.CreditPostingRole))
                throw new ConflictException("payroll_posting_role_missing", "Payroll line is missing debit or credit posting role.");

            var baseAmount = RoundBase(source.Amount * rate.Rate, baseCurrency.DecimalPlaces);
            journal.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), journal.Id, n++, accounts[source.DebitPostingRole].Id, baseAmount, 0m,
                txCurrency.Id, txCurrency.Code, txCurrency.DecimalPlaces, source.Amount, 0m, rate.Rate, rate.RateDate, rate.RateType, rate.Source,
                source.Description, null, null, source.EmployeeId, source.EmployeeName, null, null, null, source.SourceDocumentId));
            journal.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), journal.Id, n++, accounts[source.CreditPostingRole].Id, 0m, baseAmount,
                txCurrency.Id, txCurrency.Code, txCurrency.DecimalPlaces, 0m, source.Amount, rate.Rate, rate.RateDate, rate.RateType, rate.Source,
                source.Description, null, null, source.EmployeeId, source.EmployeeName, null, null, null, source.SourceDocumentId));

            if (!baseByEmployee.TryGetValue(source.EmployeeId, out var acc))
                throw new ConflictException("payroll_employee_posting_mismatch", "A payroll posting line references an employee outside the payroll run.");

            switch ((OAS.Domain.Features.Employees.Enums.SalaryComponentType)source.ComponentType)
            {
                case OAS.Domain.Features.Employees.Enums.SalaryComponentType.Earning:
                    acc.BaseGross += baseAmount;
                    break;
                case OAS.Domain.Features.Employees.Enums.SalaryComponentType.Deduction:
                    acc.BaseDeductions += baseAmount;
                    break;
                case OAS.Domain.Features.Employees.Enums.SalaryComponentType.EmployerContribution:
                    acc.BaseEmployer += baseAmount;
                    break;
            }

            if (string.Equals(source.CreditPostingRole, OAS.Domain.Features.Employees.HRPostingRoles.SalariesPayable, StringComparison.OrdinalIgnoreCase))
            {
                acc.PayableTransaction += source.Amount;
                acc.PayableBase += baseAmount;
            }
            if (string.Equals(source.DebitPostingRole, OAS.Domain.Features.Employees.HRPostingRoles.SalariesPayable, StringComparison.OrdinalIgnoreCase))
            {
                acc.PayableTransaction -= source.Amount;
                acc.PayableBase -= baseAmount;
            }
        }

        EnsureBalanced(journal, "payroll_journal_unbalanced");
        var payrollByEmployee = employeePayrolls.ToDictionary(x => x.EmployeeId);
        var employeeBase = new List<EmployeePayrollBaseAmounts>(employeePayrolls.Count);
        foreach (var pair in baseByEmployee)
        {
            var payroll = payrollByEmployee[pair.Key];
            var acc = pair.Value;
            var payableTransaction = Math.Round(acc.PayableTransaction, txCurrency.DecimalPlaces, MidpointRounding.AwayFromZero);
            if (payableTransaction != payroll.NetPay)
                throw new ConflictException("payroll_salaries_payable_mismatch", $"Payroll posting roles for employee '{payroll.EmployeeCodeSnapshot}' do not produce Salaries Payable equal to net pay.");
            if (acc.PayableBase < 0m)
                throw new ConflictException("payroll_salaries_payable_base_invalid", "Payroll Salaries Payable carrying amount cannot be negative.");

            employeeBase.Add(new EmployeePayrollBaseAmounts(
                payroll.Id,
                acc.BaseGross,
                acc.BaseDeductions,
                acc.BaseEmployer,
                acc.PayableBase));
        }

        Complete(journal, UserGuid(currentUser), time.GetUtcNow().UtcDateTime);
        await db.Set<JournalEntry>().AddAsync(journal, ct);
        return new PayrollPostingResult(
            journal.Id,
            baseCurrency.Id,
            baseCurrency.Code,
            baseCurrency.DecimalPlaces,
            rate.Rate,
            rate.RateDate,
            (byte)rate.RateType,
            (byte)rate.Source,
            employeeBase.Sum(x => x.BaseGross),
            employeeBase.Sum(x => x.BaseDeductions),
            employeeBase.Sum(x => x.BaseEmployerContributions),
            employeeBase.Sum(x => x.BaseNet),
            employeeBase);
    }

    private sealed class PayrollBaseAccumulator
    {
        public decimal BaseGross { get; set; }
        public decimal BaseDeductions { get; set; }
        public decimal BaseEmployer { get; set; }
        public decimal PayableTransaction { get; set; }
        public decimal PayableBase { get; set; }
    }

    internal static async Task<FiscalPeriod> ResolvePeriod(OasDbContext db, DateOnly date, CancellationToken ct)
    {
        var rows = await db.Set<FiscalPeriod>().AsNoTracking().Where(x => x.StartDate <= date && x.EndDate >= date).Take(2).ToListAsync(ct);
        if (rows.Count == 0) throw new ConflictException("fiscal_period_not_found", "No fiscal period contains the posting date.");
        if (rows.Count > 1) throw new ConflictException("fiscal_period_overlap", "More than one fiscal period contains the posting date.");
        if (!rows[0].CanPostAccounting()) throw new ConflictException("fiscal_period_closed", "The fiscal period is closed for accounting posting.");
        return rows[0];
    }
    internal static async Task<PostingProfile> ResolveProfile(OasDbContext db, string documentType, CancellationToken ct)
    {
        var rows = await db.Set<PostingProfile>().Include(x => x.Lines).Where(x => x.IsActive && x.Module == "HR" && x.DocumentType == documentType).Take(2).ToListAsync(ct);
        if (rows.Count != 1) throw new ConflictException("hr_posting_profile_incomplete", $"Exactly one active HR/{documentType} posting profile is required.");
        return rows[0];
    }
    internal static async Task<Dictionary<string, Account>> ResolveAccounts(OasDbContext db, PostingProfile profile, IEnumerable<string> roles, CancellationToken ct)
    {
        var result = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var matches = profile.Lines.Where(x => string.Equals(x.AccountRole, role, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new ConflictException("hr_posting_role_missing", $"Posting role '{role}' is not mapped exactly once.");
            var account = await db.Set<Account>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == matches[0].AccountId, ct) ?? throw new NotFoundException(nameof(Account), matches[0].AccountId);
            if (!account.CanReceivePosting()) throw new ConflictException("hr_posting_account_invalid", $"Account '{account.Code}' cannot receive posting.");
            result[role] = account;
        }
        return result;
    }
    internal static async Task<(AccountingSettings Settings, Currency BaseCurrency)> ResolveBase(OasDbContext db, CancellationToken ct)
    {
        var settings = await db.Set<AccountingSettings>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == AccountingSettings.SingletonId, ct) ?? throw new ConflictException("accounting_settings_required", "Accounting settings are required.");
        var baseCurrency = await db.Set<Currency>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == settings.BaseCurrencyId, ct) ?? throw new ConflictException("base_currency_missing", "Base currency does not exist.");
        return (settings, baseCurrency);
    }
    internal static async Task<JournalEntry> CreateJournal(OasDbContext db, ISequenceNumberGenerator sequences, DateOnly date, Guid periodId, string description, string documentType, Guid documentId, Currency baseCurrency, CancellationToken ct)
    {
        var seq = await sequences.NextAsync($"JournalEntry-{date.Year}", ct);
        var journal = JournalEntry.Create(Guid.NewGuid(), $"JV-{date.Year:0000}-{seq:000000}", JournalType.Automatic, date, date, periodId, description, "HR", documentType, documentId, JournalEntryStatus.Draft);
        journal.SetBaseCurrencySnapshot(baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces);
        return journal;
    }
    internal static void Complete(JournalEntry j, Guid userId, DateTime now) { j.SetPendingApproval(); j.Approve(userId, now); j.Post(userId, now); }
    internal static Guid UserGuid(ICurrentUser u) => Guid.TryParse(u.UserId, out var id) && id != Guid.Empty ? id : throw new ForbiddenException();
    internal static decimal RoundBase(decimal x, byte d) => Math.Round(x, d, MidpointRounding.AwayFromZero);
    internal static void EnsureBalanced(JournalEntry j, string code) { if (!j.IsBalanced()) throw new ConflictException(code, "The generated journal is not balanced."); }
}

public sealed class HREndOfServiceAccountingPort(OasDbContext db, ISequenceNumberGenerator sequences, IExchangeRateResolver rates, ICurrentUser currentUser, TimeProvider time) : IHREndOfServiceAccountingPort
{
    public async Task<EndOfServicePostingResult> PostAsync(EndOfServicePostingRequest request, CancellationToken ct = default)
    {
        if (await db.Set<JournalEntry>().AsNoTracking().AnyAsync(x => x.SourceModule == "HR" && x.SourceDocumentType == "EndOfService" && x.SourceDocumentId == request.SettlementId, ct))
            throw new ConflictException("eos_already_posted", "A journal already exists for this end-of-service settlement.");
        var period = await HRPayrollAccountingPort.ResolvePeriod(db, request.PostingDate, ct);
        var profile = await HRPayrollAccountingPort.ResolveProfile(db, "EndOfService", ct);
        var accounts = await HRPayrollAccountingPort.ResolveAccounts(db, profile, request.Lines.SelectMany(x => new[] { x.DebitPostingRole, x.CreditPostingRole }), ct);
        var (settings, baseCurrency) = await HRPayrollAccountingPort.ResolveBase(db, ct);
        var tx = await db.Set<Currency>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.CurrencyId, ct) ?? throw new NotFoundException(nameof(Currency), request.CurrencyId);
        var rate = await rates.ResolveAsync(request.CurrencyId, request.PostingDate, settings.DefaultExchangeRateType, cancellationToken: ct);
        var j = await HRPayrollAccountingPort.CreateJournal(db, sequences, request.PostingDate, period.Id, $"End of service {request.SettlementCode}", "EndOfService", request.SettlementId, baseCurrency, ct);
        var n=1; decimal gross=0, net=0;
        foreach(var source in request.Lines.Where(x=>x.Amount>0))
        {
            var b=HRPayrollAccountingPort.RoundBase(source.Amount*rate.Rate,baseCurrency.DecimalPlaces);
            j.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),j.Id,n++,accounts[source.DebitPostingRole].Id,b,0,tx.Id,tx.Code,tx.DecimalPlaces,source.Amount,0,rate.Rate,rate.RateDate,rate.RateType,rate.Source,source.Description,null,null,source.EmployeeId,null,null,null,null,source.SourceDocumentId));
            j.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),j.Id,n++,accounts[source.CreditPostingRole].Id,0,b,tx.Id,tx.Code,tx.DecimalPlaces,0,source.Amount,rate.Rate,rate.RateDate,rate.RateType,rate.Source,source.Description,null,null,source.EmployeeId,null,null,null,null,source.SourceDocumentId));
            if(!source.IsDeduction)gross+=b; net += source.IsDeduction ? -b : b;
        }
        HRPayrollAccountingPort.EnsureBalanced(j,"eos_journal_unbalanced"); HRPayrollAccountingPort.Complete(j,HRPayrollAccountingPort.UserGuid(currentUser),time.GetUtcNow().UtcDateTime); await db.Set<JournalEntry>().AddAsync(j,ct);
        return new EndOfServicePostingResult(j.Id,rate.Rate,rate.RateDate,(byte)rate.RateType,(byte)rate.Source,gross,Math.Max(0,net));
    }
}

public abstract class HRPayablePaymentBase
{
    protected static async Task<(PostingProfile Profile, Account Payable)> ResolvePayable(OasDbContext db, string documentType, string role, CancellationToken ct)
    {
        var profile=await HRPayrollAccountingPort.ResolveProfile(db,documentType,ct);
        var map=await HRPayrollAccountingPort.ResolveAccounts(db,profile,[role],ct);
        return (profile,map[role]);
    }
    protected static async Task<string> NextPaymentNumber(OasDbContext db,ISequenceNumberGenerator seq,int year,CancellationToken ct)
    {
        for(var i=0;i<100;i++){var n=$"PV-{year:0000}-{await seq.NextAsync($"PaymentVoucher-{year}",ct):000000}";if(!await db.Set<PaymentVoucher>().AnyAsync(x=>x.VoucherNumber==n,ct))return n;}
        throw new ConflictException("payment_voucher_number_duplicate","Could not allocate a unique payment voucher number.");
    }
}

public sealed class HRSalaryPaymentPort(OasDbContext db,ISequenceNumberGenerator sequences,ISettlementAccountResolver settlement,IExchangeRateResolver rates,IEnumerable<IPaymentAllocationTargetValidator> validators,ICurrentUser currentUser,TimeProvider time):HRPayablePaymentBase,IHRSalaryPaymentPort
{
    public async Task<SalaryPaymentPortResult> PayAsync(SalaryPaymentPortRequest r,CancellationToken ct=default)
    {
        var ep=await db.Set<EmployeePayroll>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==r.EmployeePayrollId,ct)??throw new NotFoundException(nameof(EmployeePayroll),r.EmployeePayrollId);
        if(ep.Status!=OAS.Domain.Features.Employees.Enums.EmployeePayrollStatus.Posted)throw new ConflictException("employee_payroll_not_posted","Payroll must be posted before payment.");
        var run=await db.Set<PayrollRun>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==ep.PayrollRunId,ct)??throw new NotFoundException(nameof(PayrollRun),ep.PayrollRunId);
        if(!run.PostingExchangeRateDate.HasValue||!run.PostingExchangeRateType.HasValue||!run.PostingExchangeRateSource.HasValue)
            throw new ConflictException("payroll_posting_snapshot_missing","Posted payroll is missing its exchange-rate snapshot.");
        var (_,payable)=await ResolvePayable(db,"PayrollRun",OAS.Domain.Features.Employees.HRPostingRoles.SalariesPayable,ct);
        return await PayCore(ep.CurrencyId,ep.CurrencyCodeSnapshot,ep.CurrencySymbolSnapshot,ep.CurrencyDecimalPlacesSnapshot,r.EmployeePayrollId,AllocationTargetDocumentType.EmployeePayroll,r.EmployeeId,r.EmployeeName,r.PaymentDate,r.Amount,(PaymentMethod)r.PaymentMethod,r.CashAccountId,r.BankAccountId,r.SettlementAccountId,r.ExchangeRate,(ExchangeRateType)r.ExchangeRateType,r.ReferenceNumber,r.Description,payable,run.PostingExchangeRateDate.Value,(ExchangeRateType)run.PostingExchangeRateType.Value,(ExchangeRateSource)run.PostingExchangeRateSource.Value,ct);
    }
    private async Task<SalaryPaymentPortResult> PayCore(Guid currencyId,string currencyCode,string? symbol,byte decimals,Guid targetId,AllocationTargetDocumentType targetType,Guid employeeId,string employeeName,DateOnly date,decimal amount,PaymentMethod method,Guid? cashId,Guid? bankId,Guid otherAccountId,decimal? manualRate,ExchangeRateType rateType,string? reference,string? description,Account payable,DateOnly targetRateDate,ExchangeRateType targetRateType,ExchangeRateSource targetRateSource,CancellationToken ct)
    {
        var set=await settlement.ResolveAsync(method,currencyId,cashId,bankId,otherAccountId,reference,ct);
        var (settings,baseCurrency)=await HRPayrollAccountingPort.ResolveBase(db,ct); var fx=await rates.ResolveAsync(currencyId,date,rateType,manualRate,manualRate.HasValue,ct); var sourceBase=HRPayrollAccountingPort.RoundBase(amount*fx.Rate,baseCurrency.DecimalPlaces);
        var validator=validators.SingleOrDefault(x=>x.TargetDocumentType==targetType)??throw new ConflictException("payment_allocation_target_unsupported","Payment allocation target is not supported.");
        var target=await validator.ValidateAsync(targetId,currencyId,amount,sourceBase,null,ct); var targetBase=target.TargetBaseAllocatedAmount; var diff=sourceBase-targetBase;
        var id=Guid.NewGuid(); var number=await NextPaymentNumber(db,sequences,date.Year,ct); var voucher=PaymentVoucher.CreateSettlementDocument(id,number,date,baseCurrency.Id,baseCurrency.Code,baseCurrency.DecimalPlaces,sourceBase,description);
        var line=PaymentVoucherLine.CreateSettlement(Guid.NewGuid(),id,1,SettlementPartyType.Employee,null,null,employeeId,employeeName,payable.Id,method,set.CashAccountId,set.BankAccountId,set.AccountId,currencyId,currencyCode,symbol,decimals,amount,fx.Rate,fx.RateDate,fx.RateType,fx.Source,sourceBase,reference,date,"EmployeePayroll",targetId,description,targetBase,diff);
        voucher.AddLine(line); voucher.Approve([line]);
        var period=await HRPayrollAccountingPort.ResolvePeriod(db,date,ct); var journal=await HRPayrollAccountingPort.CreateJournal(db,sequences,date,period.Id,$"Salary payment {employeeName}","SalaryPayment",id,baseCurrency,ct); var n=1;
        var targetRate=amount>0?targetBase/amount:fx.Rate;
        journal.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),journal.Id,n++,payable.Id,targetBase,0,currencyId,currencyCode,decimals,amount,0,targetRate,targetRateDate,targetRateType,targetRateSource,"Salary payable settlement",null,null,employeeId,employeeName,null,null,null,line.Id));
        if(diff>0){var loss=settings.ExchangeLossAccountId??throw new ConflictException("exchange_loss_account_required","Exchange loss account must be configured.");journal.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),journal.Id,n++,loss,diff,0,baseCurrency.Id,baseCurrency.Code,baseCurrency.DecimalPlaces,diff,0,1m,date,fx.RateType,ExchangeRateSource.System,"Realized exchange loss",null,null,employeeId,employeeName,null,null,null,line.Id));}
        journal.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),journal.Id,n++,set.AccountId,0,sourceBase,currencyId,currencyCode,decimals,0,amount,fx.Rate,fx.RateDate,fx.RateType,fx.Source,"Salary payment",null,null,employeeId,employeeName,null,null,null,line.Id));
        if(diff<0){var gain=settings.ExchangeGainAccountId??throw new ConflictException("exchange_gain_account_required","Exchange gain account must be configured.");var v=Math.Abs(diff);journal.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),journal.Id,n++,gain,0,v,baseCurrency.Id,baseCurrency.Code,baseCurrency.DecimalPlaces,0,v,1m,date,fx.RateType,ExchangeRateSource.System,"Realized exchange gain",null,null,employeeId,employeeName,null,null,null,line.Id));}
        HRPayrollAccountingPort.EnsureBalanced(journal,"salary_payment_journal_unbalanced"); var user=HRPayrollAccountingPort.UserGuid(currentUser);var now=time.GetUtcNow().UtcDateTime;HRPayrollAccountingPort.Complete(journal,user,now);voucher.SetJournalEntry(journal.Id);voucher.Post(user,now,[line]);
        var allocation=PaymentAllocation.CreateLineAllocation(Guid.NewGuid(),null,line.Id,targetType,targetId,currencyId,currencyCode,amount,fx.Rate,sourceBase,now,targetBase);
        await db.Set<JournalEntry>().AddAsync(journal,ct);await db.Set<PaymentVoucher>().AddAsync(voucher,ct);await db.Set<PaymentAllocation>().AddAsync(allocation,ct);
        return new SalaryPaymentPortResult(voucher.Id,line.Id,allocation.Id,sourceBase,targetBase,diff);
    }
}

public sealed class HREndOfServicePaymentPort(OasDbContext db,ISequenceNumberGenerator sequences,ISettlementAccountResolver settlement,IExchangeRateResolver rates,IEnumerable<IPaymentAllocationTargetValidator> validators,ICurrentUser currentUser,TimeProvider time):HRPayablePaymentBase,IHREndOfServicePaymentPort
{
    public async Task<EndOfServicePaymentPortResult> PayAsync(EndOfServicePaymentPortRequest r,CancellationToken ct=default)
    {
        var x=await db.Set<EndOfServiceSettlement>().AsNoTracking().SingleOrDefaultAsync(v=>v.Id==r.SettlementId,ct)??throw new NotFoundException(nameof(EndOfServiceSettlement),r.SettlementId);
        if(x.Status!=OAS.Domain.Features.Employees.Enums.EndOfServiceStatus.Posted)throw new ConflictException("eos_not_posted","End-of-service settlement must be posted before payment.");
        if(!x.PostingExchangeRateDate.HasValue||!x.PostingExchangeRateType.HasValue||!x.PostingExchangeRateSource.HasValue)
            throw new ConflictException("eos_posting_snapshot_missing","Posted end-of-service settlement is missing its exchange-rate snapshot.");
        var (_,payable)=await ResolvePayable(db,"EndOfService",OAS.Domain.Features.Employees.HRPostingRoles.EndOfServicePayable,ct);
        var set=await settlement.ResolveAsync((PaymentMethod)r.PaymentMethod,x.CurrencyId,r.CashAccountId,r.BankAccountId,r.SettlementAccountId,r.ReferenceNumber,ct);var(settings,baseCurrency)=await HRPayrollAccountingPort.ResolveBase(db,ct);var fx=await rates.ResolveAsync(x.CurrencyId,r.PaymentDate,(ExchangeRateType)r.ExchangeRateType,r.ExchangeRate,r.ExchangeRate.HasValue,ct);var sourceBase=HRPayrollAccountingPort.RoundBase(r.Amount*fx.Rate,baseCurrency.DecimalPlaces);
        var validator=validators.Single(v=>v.TargetDocumentType==AllocationTargetDocumentType.EndOfServiceSettlement);var target=await validator.ValidateAsync(x.Id,x.CurrencyId,r.Amount,sourceBase,null,ct);var targetBase=target.TargetBaseAllocatedAmount;var diff=sourceBase-targetBase;
        var id=Guid.NewGuid();var number=await NextPaymentNumber(db,sequences,r.PaymentDate.Year,ct);var voucher=PaymentVoucher.CreateSettlementDocument(id,number,r.PaymentDate,baseCurrency.Id,baseCurrency.Code,baseCurrency.DecimalPlaces,sourceBase,r.Description);var line=PaymentVoucherLine.CreateSettlement(Guid.NewGuid(),id,1,SettlementPartyType.Employee,null,null,r.EmployeeId,r.EmployeeName,payable.Id,(PaymentMethod)r.PaymentMethod,set.CashAccountId,set.BankAccountId,set.AccountId,x.CurrencyId,x.CurrencyCodeSnapshot,x.CurrencySymbolSnapshot,x.CurrencyDecimalPlacesSnapshot,r.Amount,fx.Rate,fx.RateDate,fx.RateType,fx.Source,sourceBase,r.ReferenceNumber,r.PaymentDate,"EndOfService",x.Id,r.Description,targetBase,diff);voucher.AddLine(line);voucher.Approve([line]);
        var period=await HRPayrollAccountingPort.ResolvePeriod(db,r.PaymentDate,ct);var j=await HRPayrollAccountingPort.CreateJournal(db,sequences,r.PaymentDate,period.Id,$"End-of-service payment {r.EmployeeName}","EndOfServicePayment",id,baseCurrency,ct);var n=1;var targetRate=targetBase/r.Amount;j.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),j.Id,n++,payable.Id,targetBase,0,x.CurrencyId,x.CurrencyCodeSnapshot,x.CurrencyDecimalPlacesSnapshot,r.Amount,0,targetRate,x.PostingExchangeRateDate.Value,(ExchangeRateType)x.PostingExchangeRateType.Value,(ExchangeRateSource)x.PostingExchangeRateSource.Value,"End-of-service payable settlement",null,null,r.EmployeeId,r.EmployeeName,null,null,null,line.Id));if(diff>0){var a=settings.ExchangeLossAccountId??throw new ConflictException("exchange_loss_account_required","Exchange loss account must be configured.");j.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),j.Id,n++,a,diff,0,baseCurrency.Id,baseCurrency.Code,baseCurrency.DecimalPlaces,diff,0,1,r.PaymentDate,fx.RateType,ExchangeRateSource.System,"Realized exchange loss",null,null,r.EmployeeId,r.EmployeeName,null,null,null,line.Id));}j.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),j.Id,n++,set.AccountId,0,sourceBase,x.CurrencyId,x.CurrencyCodeSnapshot,x.CurrencyDecimalPlacesSnapshot,0,r.Amount,fx.Rate,fx.RateDate,fx.RateType,fx.Source,"End-of-service payment",null,null,r.EmployeeId,r.EmployeeName,null,null,null,line.Id));if(diff<0){var a=settings.ExchangeGainAccountId??throw new ConflictException("exchange_gain_account_required","Exchange gain account must be configured.");var v=Math.Abs(diff);j.AddLine(JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(),j.Id,n++,a,0,v,baseCurrency.Id,baseCurrency.Code,baseCurrency.DecimalPlaces,0,v,1,r.PaymentDate,fx.RateType,ExchangeRateSource.System,"Realized exchange gain",null,null,r.EmployeeId,r.EmployeeName,null,null,null,line.Id));}HRPayrollAccountingPort.EnsureBalanced(j,"eos_payment_journal_unbalanced");var user=HRPayrollAccountingPort.UserGuid(currentUser);var now=time.GetUtcNow().UtcDateTime;HRPayrollAccountingPort.Complete(j,user,now);voucher.SetJournalEntry(j.Id);voucher.Post(user,now,[line]);var alloc=PaymentAllocation.CreateLineAllocation(Guid.NewGuid(),null,line.Id,AllocationTargetDocumentType.EndOfServiceSettlement,x.Id,x.CurrencyId,x.CurrencyCodeSnapshot,r.Amount,fx.Rate,sourceBase,now,targetBase);await db.Set<JournalEntry>().AddAsync(j,ct);await db.Set<PaymentVoucher>().AddAsync(voucher,ct);await db.Set<PaymentAllocation>().AddAsync(alloc,ct);return new(voucher.Id,line.Id,alloc.Id);
    }
}

public sealed class HREmployeeIdentityPort(OasDbContext db,IIdentityRepository identity):IHREmployeeIdentityPort
{
    public async Task DisableLinkedUserAsync(Guid employeeId,CancellationToken ct=default)
    {
        var employee=await db.Set<Employee>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==employeeId,ct)??throw new NotFoundException(nameof(Employee),employeeId);if(!employee.UserAccountId.HasValue)return;var user=await identity.GetUserAsync(employee.UserAccountId.Value,true,ct)??throw new NotFoundException("UserAccount",employee.UserAccountId.Value);user.User.SetActive(false);identity.UpdateUser(user.User);
    }
}
