using Microsoft.EntityFrameworkCore;
using OAS.Contracts.Accounting.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Identity.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;
public sealed partial class VoucherOperations
{
    private sealed record Detail(Guid VoucherId, Guid LineId, int LineNo, bool Receipt, string Number, DateOnly Date, int Status, Guid? JournalId, Guid? BaseId, Guid AccountId, Guid? SettlementId, Guid? CashId, Guid? BankId, Guid? CurrencyId, string? CurrencyCode, decimal Amount, decimal? BaseAmount, int? PartyType, Guid? PartyId, string? PartyName, int Method, string? Description, Guid? ReferenceId, string? ReferenceType);
    private IQueryable<Detail> ReceiptDetails(IQueryable<ReceiptVoucher> vouchers) =>
        from v in vouchers join l in db.Set<ReceiptVoucherLine>() on v.Id equals l.ReceiptVoucherId
        select new Detail(v.Id,l.Id,l.LineNumber,true,v.VoucherNumber,v.VoucherDate,(int)v.Status,v.JournalEntryId,v.BaseCurrencyId,l.CounterpartyAccountId??l.AccountId,l.SettlementAccountId,l.CashAccountId??v.CashAccountId,l.BankAccountId??v.BankAccountId,l.CurrencyId,l.CurrencyCodeSnapshot,l.Amount,l.BaseAmount,
            l.PartyType.HasValue?(int?)l.PartyType:v.CustomerId.HasValue?(int?)SettlementPartyType.Customer:null,l.CustomerId??l.SupplierId??l.EmployeeId??v.CustomerId,l.PartyNameSnapshot??v.ReceivedFrom,(int)(l.PaymentMethod??v.PaymentMethod),l.Description??v.Description,l.ReferenceId,l.ReferenceType);
    private IQueryable<Detail> PaymentDetails(IQueryable<PaymentVoucher> vouchers) =>
        from v in vouchers join l in db.Set<PaymentVoucherLine>() on v.Id equals l.PaymentVoucherId
        select new Detail(v.Id,l.Id,l.LineNumber,false,v.VoucherNumber,v.VoucherDate,(int)v.Status,v.JournalEntryId,v.BaseCurrencyId,l.CounterpartyAccountId??l.AccountId,l.SettlementAccountId,l.CashAccountId??v.CashAccountId,l.BankAccountId??v.BankAccountId,l.CurrencyId,l.CurrencyCodeSnapshot,l.Amount,l.BaseAmount,
            l.PartyType.HasValue?(int?)l.PartyType:v.SupplierId.HasValue?(int?)SettlementPartyType.Supplier:null,l.CustomerId??l.SupplierId??l.EmployeeId??v.SupplierId,l.PartyNameSnapshot??v.BeneficiaryName,(int)(l.PaymentMethod??v.PaymentMethod),l.Description??v.Description,l.ReferenceId,l.ReferenceType);
    private sealed record Event(Detail D, DateOnly Date, string Status, bool Posted, bool Reversal, Guid? JournalId, string? JournalNumber, decimal? Base, Guid? CurrencyId, string? CurrencyCode, decimal? Native);
    private static CashBankReportCell T(string? text,Guid? id=null,string? kind=null)=>new(Text:text,LinkId:id,LinkKind:kind);
    private static CashBankReportCell B(decimal? value)=>new(Amount:value,Format:"base");
    private static CashBankReportCell N(decimal? value)=>new(Amount:value,Format:"native");
    private static CashBankReportCell C(int value)=>new(Amount:value,Format:"count");
    private static CashBankReportRow R(params CashBankReportCell[] cells)=>new(cells);
    private static CashBankReportMetric M(string label,CashBankReportCell value)=>new(label,value);
    private static string Party(int? p)=>p switch {1=>"عميل",2=>"مورد",3=>"موظف",4=>"أخرى",_=>"غير محدد"};
    private static string MethodLabel(int m)=>m switch {1=>"نقدي",2=>"بطاقة",3=>"تحويل بنكي",4=>"شيك",5=>"أخرى",_=>"غير محدد"};
    private static decimal? Sum(IEnumerable<decimal?> amounts)=>CashBankReportMath.Sum(amounts);
    private static decimal? Total(IEnumerable<Event> events,bool receipt)=>Sum(events.Where(e=>e.Posted&&e.D.Receipt==receipt).Select(e=>e.Base));
    private static string DateText(DateOnly date)=>date.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
    public async Task<CashBankReportDto> ReportAsync(VoucherReportFilter f,CancellationToken ct)
    {
        if(f.FromDate==default||f.ToDate<f.FromDate||f.Mode is not ("all" or "receipt" or "payment" or "lines" or "customers" or "suppliers" or "employees" or "accounts" or "settlements" or "methods" or "currency")||f.Status is not ("all" or "posted" or "draft" or "approved" or "cancelled" or "reversed")||f.Kind is not ("all" or "receipt" or "payment")||f.Method is <1 or >5||f.PartyType is <1 or >4||f.Party?.Length>200||(f.CashAccountId.HasValue&&f.BankAccountId.HasValue)) throw Error("فلاتر التقرير غير صالحة.");
        var settings=await db.Set<AccountingSettings>().AsNoTracking().SingleAsync(x=>x.Id==AccountingSettings.SingletonId,ct);
        var currencies=await db.Set<Currency>().AsNoTracking().Select(x=>new CashBankReportCurrencyDto(x.Id,x.Code,x.NameAr,x.DecimalPlaces)).ToListAsync(ct);
        var baseCurrency=currencies.Single(x=>x.Id==settings.BaseCurrencyId);
        var journalQuery=db.Set<JournalEntry>().AsNoTracking();
        var journalIdsInPeriod=journalQuery.Where(j=>j.Status==JournalEntryStatus.Posted&&j.PostingDate>=f.FromDate&&j.PostingDate<=f.ToDate);
        var originalIds=journalQuery.Where(j=>journalIdsInPeriod.Any(p=>p.Id==j.Id||p.Id==j.ReversedJournalId)).Select(j=>j.Id);
        var receipts=db.Set<ReceiptVoucher>().AsNoTracking().Where(v=>(v.VoucherDate>=f.FromDate&&v.VoucherDate<=f.ToDate)||(v.JournalEntryId.HasValue&&originalIds.Contains(v.JournalEntryId.Value)));
        var payments=db.Set<PaymentVoucher>().AsNoTracking().Where(v=>(v.VoucherDate>=f.FromDate&&v.VoucherDate<=f.ToDate)||(v.JournalEntryId.HasValue&&originalIds.Contains(v.JournalEntryId.Value)));
        var details=new List<Detail>();
        if(f.Kind!="payment"&&f.Mode!="payment")details.AddRange(await ReceiptDetails(receipts).Take(50001).ToListAsync(ct));
        if(f.Kind!="receipt"&&f.Mode!="receipt")details.AddRange(await PaymentDetails(payments).Take(50001).ToListAsync(ct));
        if(details.Count>50000)throw Error("عدد بنود السندات يتجاوز 50 ألفًا؛ اختر فترة أقصر.");
        var journalIds=details.Where(x=>x.JournalId.HasValue).Select(x=>x.JournalId!.Value).Distinct().ToArray();
        var journals=await journalQuery.Where(j=>journalIds.Contains(j.Id)).ToDictionaryAsync(j=>j.Id,ct);
        var reverseIds=journals.Values.Where(j=>j.ReversedJournalId.HasValue).Select(j=>j.ReversedJournalId!.Value).Distinct().ToArray();
        var reversals=await journalQuery.Where(j=>reverseIds.Contains(j.Id)).ToDictionaryAsync(j=>j.Id,ct);
        var ledgerIds=journalIds.Concat(reverseIds).Distinct().ToArray();
        var allJournals=journals.Values.Concat(reversals.Values).GroupBy(j=>j.Id).ToDictionary(g=>g.Key,g=>g.First());
        var actorIds=allJournals.Values.Where(j=>j.PostedBy.HasValue).Select(j=>j.PostedBy!.Value).Distinct().ToArray();
        var operators=await db.Set<UserAccount>().AsNoTracking().Where(x=>actorIds.Contains(x.Id)).Select(x=>new CashBankReportOperatorDto(x.Id,x.FirstName+" "+x.LastName+" ("+x.UserName+")")).ToListAsync(ct);
        var actorNames=operators.ToDictionary(x=>x.Id,x=>x.Name);
        string? Actor(Event e)=>e.JournalId is Guid jid&&allJournals.TryGetValue(jid,out var j)&&j.PostedBy is Guid actor?actorNames.GetValueOrDefault(actor,actor.ToString()):null;
        Guid? ActorId(Event e)=>e.JournalId is Guid jid&&allJournals.TryGetValue(jid,out var j)?j.PostedBy:null;
        var ledger=await db.Set<JournalEntryLine>().AsNoTracking().Where(x=>ledgerIds.Contains(x.JournalEntryId)).Take(250001).ToListAsync(ct);
        if(ledger.Count>250000)throw Error("عدد القيود كبير؛ اختر فترة أقصر.");
        var byJournal=ledger.ToLookup(x=>x.JournalEntryId);
        var accounts=await db.Set<Account>().AsNoTracking().Select(a=>new CashBankReportAccountDto(a.Id,a.Code,a.NameAr,"account",null,a.IsActive,null)).ToListAsync(ct);
        var accountNames=accounts.ToDictionary(a=>a.AccountId,a=>a.Code+" - "+a.Name);
        var cash=await db.Set<CashAccount>().AsNoTracking().ToDictionaryAsync(x=>x.Id,ct);
        var banks=await db.Set<BankAccount>().AsNoTracking().ToDictionaryAsync(x=>x.Id,ct);
        string AccountName(Guid? id)=>id.HasValue?accountNames.GetValueOrDefault(id.Value,"غير متاح"):"غير محدد";
        string Settlement(Detail d)=>d.CashId is Guid c&&cash.TryGetValue(c,out var ca)?ca.Name:d.BankId is Guid b&&banks.TryGetValue(b,out var ba)?ba.BankName+" - "+ba.AccountName:AccountName(d.SettlementId);
        var warnings=new HashSet<string>{"المجاميع المالية تشمل القيود المرحلة فقط؛ العكس يعرض بقيمة سالبة في تاريخ ترحيله. القيم الأصلية لا تجمع بين عملات مختلفة.","قد تتضمن السندات تسويات غير نقدية؛ صافي السندات ليس إجمالي السيولة. أسماء الحسابات والصناديق من التعريفات الحالية. قيود فروق صرف تخصيصات الفواتير مستقلة وتظهر في اليومية وكشوف الحساب، وليست مبالغ قبض أو صرف إضافية."};
        var events=new List<Event>();
        foreach(var group in details.GroupBy(d=>new {d.Receipt,d.VoucherId}))
        {
            var first=group.First(); var journal=first.JournalId is Guid ji?journals.GetValueOrDefault(ji):null;
            var reverse=journal?.ReversedJournalId is Guid ri?reversals.GetValueOrDefault(ri):null;
            var posted=first.Status==3&&journal?.Status==JournalEntryStatus.Posted&&journal.SourceDocumentId==first.VoucherId&&journal.SourceDocumentType==(first.Receipt?"ReceiptVoucher":"PaymentVoucher");
            var validReverse=posted&&reverse?.Status==JournalEntryStatus.Posted&&reverse.SourceDocumentId==first.VoucherId&&reverse.SourceDocumentType==(first.Receipt?"ReceiptVoucherReversal":"PaymentVoucherReversal");
            var legacy=group.All(d=>!d.SettlementId.HasValue&&!d.BaseAmount.HasValue);
            var originalLines=journal is null?Enumerable.Empty<JournalEntryLine>():byJournal[journal.Id];
            var legacyValid=legacy&&posted&&journal!.BaseCurrencyId==baseCurrency.Id&&originalLines.Sum(l=>l.DebitAmount)==group.Sum(d=>d.Amount)&&originalLines.Sum(l=>l.CreditAmount)==group.Sum(d=>d.Amount)&&group.GroupBy(d=>d.AccountId).All(g=>originalLines.Where(l=>l.AccountId==g.Key).Sum(l=>first.Receipt?l.CreditAmount:l.DebitAmount)==g.Sum(d=>d.Amount));
            foreach(var d in group)
            {
                var pair=originalLines.Where(l=>l.SourceDocumentLineId==d.LineId&&l.AccountId==d.SettlementId).ToArray();
                var match=pair.Length==1&&journal?.BaseCurrencyId==baseCurrency.Id&&d.CurrencyId.HasValue&&pair[0].TransactionCurrencyId==d.CurrencyId&&(d.Receipt?pair[0].DebitAmount>0&&pair[0].CreditAmount==0:pair[0].CreditAmount>0&&pair[0].DebitAmount==0);
                decimal? amount=posted?(match?(d.Receipt?pair[0].DebitAmount:pair[0].CreditAmount):legacyValid?d.Amount:null):null;
                var currencyId=legacyValid?baseCurrency.Id:d.CurrencyId;
                var currencyCode=legacyValid?baseCurrency.Code:d.CurrencyCode;
                decimal? native=posted?(match?(d.Receipt?pair[0].TransactionDebitAmount:pair[0].TransactionCreditAmount):legacyValid?d.Amount:null):d.CurrencyId.HasValue?d.Amount:null;
                if(posted&&!amount.HasValue)warnings.Add("توجد بنود مرحلة لا يمكن مطابقة قيمتها مع القيد؛ تظهر المجاميع المتأثرة غير متاحة.");
                if(first.Status==3&&!posted)warnings.Add("توجد سندات بحالة مرحل دون قيد مرحل صالح؛ استُبعدت من المجاميع المالية.");
                if(journal?.ReversedJournalId.HasValue==true&&!validReverse)warnings.Add("توجد ارتباطات عكس غير مكتملة أو غير مدعومة؛ راجع القيد المصدر.");
                var state=posted?(validReverse?"معكوس":"مرحل"):first.Status switch {1=>"مسودة",2=>"معتمد",4=>"ملغى",_=>"قيد غير متاح"};
                var date=posted?journal!.PostingDate:d.Date;
                events.Add(new(d,date,state,posted,false,journal?.Id,journal?.JournalNumber,amount,currencyId,currencyCode,native));
                if(validReverse)
                {
                    var revPair=byJournal[reverse!.Id].Where(l=>l.SourceDocumentLineId==d.LineId&&l.AccountId==d.SettlementId).ToArray();
                    var exact=match&&revPair.Length==1&&revPair[0].IsExactOppositeOf(pair[0]);
                    if(!exact)warnings.Add("يوجد قيد عكسي لا يمكن مطابقة بنوده؛ لا تعرض له قيمة مفترضة.");
                    events.Add(new(d,reverse.PostingDate,"عكس مرحل",true,true,reverse.Id,reverse.JournalNumber,exact?-amount:null,currencyId,currencyCode,exact?-native:null));
                }
            }
        }
        var selected=events.Where(e=>e.Date>=f.FromDate&&e.Date<=f.ToDate&&(f.Status switch {"posted"=>e.Posted,"reversed"=>e.Status is "معكوس" or "عكس مرحل","draft"=>e.Status=="مسودة","approved"=>e.Status=="معتمد","cancelled"=>e.Status=="ملغى",_=>true})&&(!f.CurrencyId.HasValue||e.CurrencyId==f.CurrencyId)&&(!f.AccountId.HasValue||e.D.AccountId==f.AccountId||e.D.SettlementId==f.AccountId)&&(!f.CashAccountId.HasValue||e.D.CashId==f.CashAccountId)&&(!f.BankAccountId.HasValue||e.D.BankId==f.BankAccountId)&&(!f.Method.HasValue||e.D.Method==f.Method)&&(!f.PartyType.HasValue||e.D.PartyType==f.PartyType)&&(!f.PostedBy.HasValue||ActorId(e)==f.PostedBy)&&(string.IsNullOrWhiteSpace(f.Party)||(e.D.PartyName??Party(e.D.PartyType)).Contains(f.Party.Trim(),StringComparison.CurrentCultureIgnoreCase))&&(f.Mode switch {"customers"=>e.D.PartyType==1,"suppliers"=>e.D.PartyType==2,"employees"=>e.D.PartyType==3,_=>true})).OrderByDescending(e=>e.Date).ThenBy(e=>e.D.Number).ThenBy(e=>e.D.LineNo).ToArray();
        var rows=new List<CashBankReportRow>(); string[] headers;
        int Count(IEnumerable<Event> es)=>es.Where(e=>!e.Reversal).Select(e=>(e.D.Receipt,e.D.VoucherId)).Distinct().Count();
        string DistinctText(IEnumerable<string?> values)=>string.Join("، ",values.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct());
        string VKind(Detail d)=>d.Receipt?"قبض":"صرف";
        string Link(Detail d)=>d.Receipt?"receipt":"payment";
        if(f.Mode is "all" or "receipt" or "payment")
        {
            headers=["رقم السند","التاريخ","النوع","الطرف","الحالة","القبض الدفتري","الصرف الدفتري","القيد الأصلي","القيد العكسي","المستخدم المرحّل","البيان / سبب العكس"];
            foreach(var g in selected.GroupBy(e=>new{e.D.Receipt,e.D.VoucherId,e.Reversal,e.JournalId}))
            {
                var e=g.First(); rows.Add(R(T(e.D.Number,e.D.VoucherId,Link(e.D)),T(DateText(e.Date)),T(VKind(e.D)),T(DistinctText(g.Select(x=>x.D.PartyName??Party(x.D.PartyType)))),T(e.Status),B(e.D.Receipt?Sum(g.Select(x=>x.Base)):null),B(!e.D.Receipt?Sum(g.Select(x=>x.Base)):null),T(e.D.JournalId is Guid originalId?journals.GetValueOrDefault(originalId)?.JournalNumber:null,e.D.JournalId,"journal"),T(e.D.JournalId is Guid oid&&journals.GetValueOrDefault(oid)?.ReversedJournalId is Guid rid?reversals.GetValueOrDefault(rid)?.JournalNumber:null,e.D.JournalId is Guid o?journals.GetValueOrDefault(o)?.ReversedJournalId:null,"journal"),T(Actor(e)),T(e.Reversal&&e.JournalId is Guid revId?allJournals.GetValueOrDefault(revId)?.Description:DistinctText(g.Select(x=>x.D.Description)))));
            }
        }
        else if(f.Mode=="lines")
        {
            headers=["رقم السند","التاريخ","النوع","البند","الطرف","الحساب","التسوية","الوسيلة","العملة","الأصلي","الدفتري","الحالة","القيد"];
            foreach(var e in selected)rows.Add(R(T(e.D.Number,e.D.VoucherId,Link(e.D)),T(DateText(e.Date)),T(VKind(e.D)),C(e.D.LineNo),T(e.D.PartyName??Party(e.D.PartyType)),T(AccountName(e.D.AccountId),e.D.AccountId,"account"),T(Settlement(e.D)),T(MethodLabel(e.D.Method)),T(e.CurrencyCode),N(e.Native),B(e.Base),T(e.Status),T(e.JournalNumber,e.JournalId,"journal")));
        }
        else
        {
            headers=["التصنيف","عدد السندات","عدد البنود والحركات","القبض الدفتري","الصرف الدفتري","الصافي الدفتري"];
            string Key(Event e)=>f.Mode switch {"customers" or "suppliers" or "employees"=>e.D.PartyId?.ToString()??"unknown","accounts"=>e.D.AccountId.ToString(),"settlements"=>e.D.CashId is Guid c?"cash:"+c:e.D.BankId is Guid b?"bank:"+b:e.D.SettlementId?.ToString()??"unknown","methods"=>e.D.Method.ToString(),"currency"=>e.CurrencyId?.ToString()??"unknown",_=>"unknown"};
            string Name(Event e)=>f.Mode switch {"customers" or "suppliers" or "employees"=>e.D.PartyName??Party(e.D.PartyType),"accounts"=>AccountName(e.D.AccountId),"settlements"=>Settlement(e.D),"methods"=>MethodLabel(e.D.Method),"currency"=>e.CurrencyCode??"غير محددة",_=>"غير محدد"};
            foreach(var g in selected.GroupBy(Key).OrderBy(g=>Name(g.First()))) {var r=Total(g,true);var p=Total(g,false); rows.Add(R(T(Name(g.First())),C(Count(g)),C(g.Count()),B(r),B(p),B(r-p)));}
            if(f.Mode=="currency")warnings.Add("التجميع حسب عملة السند، والقيم المعروضة دفترية بالعملة الأساسية؛ راجع تفاصيل البنود للمبالغ الأصلية.");
        }
        var received=Total(selected,true); var paid=Total(selected,false);
        var metrics=new[]{M("عدد سندات القبض",C(Count(selected.Where(e=>e.D.Receipt)))),M("عدد سندات الصرف",C(Count(selected.Where(e=>!e.D.Receipt)))),M("صافي المقبوضات الدفترية",B(received)),M("صافي المدفوعات الدفترية",B(paid)),M("السندات غير المرحلة",C(Count(selected.Where(e=>!e.Posted)))),M("صافي حركة السندات",B(received-paid))};
        if(rows.Count>50000)throw Error("عدد صفوف التقرير يتجاوز الحد المسموح؛ اختر فترة أقصر.");
        var totalCells=Enumerable.Range(0,headers.Length).Select(_=>T(null)).ToArray(); totalCells[0]=T("الإجمالي");
        if(f.Mode is "all" or "receipt" or "payment"){totalCells[5]=B(received);totalCells[6]=B(paid);}
        else if(f.Mode=="lines"){totalCells[10]=B(Sum(selected.Where(e=>e.Posted).Select(e=>e.Base)));}
        else {totalCells[1]=C(Count(selected));totalCells[2]=C(selected.Length);totalCells[3]=B(received);totalCells[4]=B(paid);totalCells[5]=B(received-paid);}
        return new(baseCurrency.Id,baseCurrency.Code,baseCurrency.DecimalPlaces,accounts,currencies,headers,rows,metrics,warnings.ToArray()){TotalRow=R(totalCells),Operators=operators,SettlementAccounts=cash.Values.Select(c=>new CashBankReportAccountDto(c.Id,c.Code,c.Name,"cash",c.CurrencyId,c.IsActive)).Concat(banks.Values.Select(b=>new CashBankReportAccountDto(b.Id,b.Code,b.BankName+" - "+b.AccountName,"bank",b.CurrencyId,b.IsActive))).OrderBy(x=>x.Kind).ThenBy(x=>x.Code).ToArray()};
    }
}
