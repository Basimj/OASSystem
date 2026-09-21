using OAS.Application.Accounting.Accounts.Commands.CreateAccount;
using OAS.Contracts.Accounting.Accounts;
using OAS.Application.Accounting.CostCenters.Commands.CreateCostCenter;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Application.Accounting.BankAccounts.Commands.CreateBankAccount;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Application.Accounting.PostingProfiles.Commands.CreatePostingProfile;
using OAS.Contracts.Accounting.PostingProfiles;
using System.Globalization;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Spreadsheets;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.CreateExpenseType;
using OAS.Application.Accounting.Journals.Commands.CreateJournalEntry;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Spreadsheets;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Common.Entities;
namespace OAS.Application.Accounting.Spreadsheets;
public sealed partial class AccountingSpreadsheetService(
    ISpreadsheetWorkbook workbook, IServiceProvider services, ISender sender,
    IUnitOfWork unitOfWork, IPermissionChecker permissions)
{
    private async Task Authorize(string section, string action, CancellationToken ct)
    {
        if (!AccountingSpreadsheetDefinitions.ExportSections.Contains(section)) throw new NotFoundException("spreadsheet",section);
        if (!await permissions.HasPermissionAsync($"accounting.{section.Replace('-', '_')}.{action}",ct)) throw new ForbiddenException();
    }
    public async Task<byte[]> TemplateAsync(string section, CancellationToken ct)
    {
        await Authorize(section,"create",ct);
        return workbook.Write(AccountingSpreadsheetDefinitions.Get(section).Select(x=>new SpreadsheetTable(x,[])).ToArray(),true);
    }
    private Task<IReadOnlyList<T>> List<T>(CancellationToken ct) where T:Entity<Guid> => services.GetRequiredService<IReadRepository<T,Guid>>().ListAsync(cancellationToken:ct);
    private sealed class Row(SpreadsheetRow source)
    {
        public SpreadsheetRow Source { get; } = source;
        public List<string> Errors { get; } = [];
        public List<string> Warnings { get; } = [];
        public string Get(string key) => Source.Values.GetValueOrDefault(key)?.Trim() ?? "";
        public string? Optional(string key) => Get(key) is { Length: > 0 } value ? value : null;
        public bool Bool(string key,bool fallback=false)
        {
            var value=Get(key);
            if(value.Length==0) return fallback;
            return value.Trim().ToLowerInvariant() switch { "نعم" or "yes" or "true" or "1" => true, "لا" or "no" or "false" or "0" => false, _ => throw new FormatException($"{key}: قيمة منطقية غير صالحة.") };
        }
        public T Enum<T>(string key) where T:struct,Enum => System.Enum.Parse<T>(Get(key),true);
        public DateOnly Date(string key) => DateOnly.ParseExact(Get(key),"yyyy-MM-dd",CultureInfo.InvariantCulture);
        public decimal Decimal(string key) => decimal.Parse(Get(key),NumberStyles.Number,CultureInfo.InvariantCulture);
        public SpreadsheetRowResult Result() => new(Source.Sheet,Source.Number,Source.Values,Errors,Warnings);
    }
    private sealed record Prepared(IReadOnlyList<Row> Rows,IReadOnlyList<object> Commands);
    public async Task<SpreadsheetPreview> PreviewAsync(string section,byte[] bytes,CancellationToken ct)
    {
        await Authorize(section,"create",ct);
        try
        {
            var plan=await Prepare(section,bytes,ct);
            return new(plan.Rows.Select(x=>x.Result()).ToArray());
        }
        catch(RequestValidationException ex)
        {
            return FileValidationPreview(ex);
        }
        catch(Exception ex) when (ex is InvalidDataException or IOException or ArgumentException or FormatException or InvalidOperationException or System.Xml.XmlException)
        {
            return FileValidationPreview("تعذر قراءة ملف Excel. استخدم قالب Excel الخاص بهذه الشاشة.");
        }
    }
    private static SpreadsheetPreview FileValidationPreview(RequestValidationException ex)
        => FileValidationPreview(ex.Errors.SelectMany(x=>x.Value).FirstOrDefault() ?? "ملف Excel غير صالح.");
    private static SpreadsheetPreview FileValidationPreview(string message)
        => new([new SpreadsheetRowResult("الملف",1,new Dictionary<string,string>(),[message],[])]);
    public async Task<SpreadsheetPreview> ImportAsync(string section,byte[] bytes,CancellationToken ct)
    {
        await Authorize(section,"create",ct);
        // Re-parse and validate authoritative bytes on every confirmation; never trust client preview.
        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            Prepared plan;
            try { plan=await Prepare(section,bytes,token); }
            catch(RequestValidationException ex) { return FileValidationPreview(ex); }
            catch(Exception ex) when (ex is InvalidDataException or IOException or ArgumentException or FormatException or InvalidOperationException or System.Xml.XmlException)
            { return FileValidationPreview("تعذر قراءة ملف Excel. استخدم قالب Excel الخاص بهذه الشاشة."); }
            var preview=new SpreadsheetPreview(plan.Rows.Select(x=>x.Result()).ToArray());
            if(!preview.CanImport) return preview;
            var accountIds=new Dictionary<string,Guid>(StringComparer.OrdinalIgnoreCase);
            var centerIds=new Dictionary<string,Guid>(StringComparer.OrdinalIgnoreCase);
            foreach(var original in plan.Commands)
            {
                object command=original;
                if(original is CreateAccountCommand a)
                {
                    var row=plan.Rows.Single(x=>x.Get("Code")==a.Data.Code);
                    if(accountIds.TryGetValue(row.Get("ParentCode"),out var parent)) command=new CreateAccountCommand(a.Data with { ParentAccountId=parent });
                }
                if(original is CreateCostCenterCommand c)
                {
                    var row=plan.Rows.Single(x=>x.Get("Code")==c.Data.Code);
                    if(centerIds.TryGetValue(row.Get("ParentCode"),out var parent)) command=new CreateCostCenterCommand(c.Data with { ParentCostCenterId=parent });
                }
                var result=await sender.Send(command,token);
                if(result is Account account) accountIds[account.Code]=account.Id;
                if(result is CostCenter center) centerIds[center.Code]=center.Id;
            }
            return preview with { ImportedRecords=plan.Commands.Count };
        },ct);
    }
    private async Task<Prepared> Prepare(string section,byte[] bytes,CancellationToken ct)
    {
        var definitions=AccountingSpreadsheetDefinitions.Get(section);
        var rows=workbook.Read(bytes,definitions).Select(x=>new Row(x)).ToList();
        foreach(var row in rows)
        {
            var definition=definitions.Single(x=>x.Name==row.Source.Sheet);
            foreach(var column in definition.Columns)
            {
                var value=row.Get(column.Key);
                if(value.Length==0) { if(column.Required) row.Errors.Add($"{column.Header ?? column.Key}: مطلوب."); continue; }
                if(column.DataType=="bool" && value.Trim().ToLowerInvariant() is not ("نعم" or "لا" or "yes" or "no" or "true" or "false" or "1" or "0")) row.Errors.Add($"{column.Header ?? column.Key}: استخدم نعم أو لا.");
                if(column.DataType=="date" && !DateOnly.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _)) row.Errors.Add($"{column.Header ?? column.Key}: التاريخ غير صالح؛ استخدم yyyy-MM-dd.");
                if(column.DataType=="decimal")
                {
                    if(!decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)) row.Errors.Add($"{column.Header ?? column.Key}: رقم غير صالح.");
                    else if(amount<0 || amount>999999999999999m || decimal.Round(amount,4)!=amount) row.Errors.Add($"{column.Header ?? column.Key}: استخدم مبلغاً موجباً لا يتجاوز 15 رقماً و4 منازل عشرية.");
                }
                if(column.DataType!="bool" && column.AllowedValues is not null && !column.AllowedValues.Contains(value,StringComparer.OrdinalIgnoreCase)) row.Errors.Add($"{column.Header ?? column.Key}: قيمة غير مسموحة.");
            }
        }
        var accounts=await List<Account>(ct);
        var centers=await List<CostCenter>(ct);
        var accountMap=accounts.ToDictionary(x=>x.Code,StringComparer.OrdinalIgnoreCase);
        var centerMap=centers.ToDictionary(x=>x.Code,StringComparer.OrdinalIgnoreCase);
        var codeLabel=section switch { "accounts"=>"كود الحساب", "cost-centers"=>"كود مركز التكلفة", "cash-accounts"=>"كود الصندوق", "bank-accounts"=>"كود الحساب البنكي", "expense-types"=>"كود نوع المصروف", "posting-profiles"=>"كود ملف الترحيل", _=>"الكود" };
        var headers=rows.Where(x=>x.Source.Sheet==definitions[0].Name).ToList();
        IReadOnlyList<string> existing=section switch
        {
            "accounts"=>accounts.Select(x=>x.Code).ToArray(),
            "cost-centers"=>centers.Select(x=>x.Code).ToArray(),
            "cash-accounts"=>(await List<CashAccount>(ct)).Select(x=>x.Code).ToArray(),
            "bank-accounts"=>(await List<BankAccount>(ct)).Select(x=>x.Code).ToArray(),
            "expense-types"=>(await List<ExpenseType>(ct)).Select(x=>x.Code).ToArray(),
            "posting-profiles"=>(await List<PostingProfile>(ct)).Select(x=>x.Code).ToArray(),
            _=>[]
        };
        if(section!="journals")
            foreach(var group in headers.GroupBy(x=>x.Get("Code"),StringComparer.OrdinalIgnoreCase))
                foreach(var row in group)
                {
                    if(group.Count()>1) row.Errors.Add($"الكود {group.Key} مكرر داخل الملف.");
                    if(existing.Contains(group.Key,StringComparer.OrdinalIgnoreCase)) row.Errors.Add($"{codeLabel} {group.Key} مستخدم مسبقاً.");
                }
        if(section=="bank-accounts")
        {
            var banks=await List<BankAccount>(ct);
            foreach(var group in headers.GroupBy(x=>x.Get("AccountNumber"),StringComparer.OrdinalIgnoreCase))
                foreach(var row in group)
                    if(group.Count()>1 || banks.Any(x=>string.Equals(x.AccountNumber,group.Key,StringComparison.OrdinalIgnoreCase))) row.Errors.Add("رقم الحساب البنكي مستخدم مسبقاً.");
        }
        var levels=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        if(section is "accounts" or "cost-centers")
        {
            var parents=headers.GroupBy(x=>x.Get("Code"),StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.First(),StringComparer.OrdinalIgnoreCase);
            foreach(var row in headers)
            {
                var path=new HashSet<string>(StringComparer.OrdinalIgnoreCase) { row.Get("Code") };
                var current=row;var depth=1;
                while(current.Get("ParentCode") is { Length:>0 } parent)
                {
                    if(!path.Add(parent)) { row.Errors.Add("تسلسل الآباء يحتوي دورة أو الحساب أب لنفسه."); break; }
                    if(parents.TryGetValue(parent,out var next)) { current=next;depth++;continue; }
                    if(section=="accounts" && accountMap.TryGetValue(parent,out var a)) { depth+=a.Level;break; }
                    if(section=="cost-centers" && centerMap.ContainsKey(parent)) break;
                    row.Errors.Add($"الأب {parent} غير موجود.");break;
                }
                if(depth>255) row.Errors.Add("تجاوز الحد الأقصى لمستوى الحساب.");
                levels[row.Get("Code")]=depth;
            }
            headers=headers.OrderBy(x=>levels[x.Get("Code")]).ToList();
        }
        Guid? Resolve(Row row,string field,bool optional=false)
        {
            var code=row.Get(field);
            if(code.Length==0 && optional) return null;
            if(!accountMap.TryGetValue(code,out var account)) { row.Errors.Add($"الحساب العام {code} غير موجود ({field})."); return null; }
            return account.Id;
        }
        foreach(var row in rows)
        {
            if(row.Source.Values.ContainsKey("AccountCode")) Resolve(row,"AccountCode");
            if(row.Source.Values.ContainsKey("DefaultExpenseAccountCode")) Resolve(row,"DefaultExpenseAccountCode",true);
        }
        var commands=new List<object>();
        async Task Add(Row row,object command)
        {
            var validatorType=typeof(IValidator<>).MakeGenericType(command.GetType());
            foreach(var validator in services.GetServices(validatorType).Cast<IValidator>())
            {
                var contextType=typeof(ValidationContext<>).MakeGenericType(command.GetType());
                var validation=await validator.ValidateAsync((IValidationContext)Activator.CreateInstance(contextType,command)!,ct);
                row.Errors.AddRange(validation.Errors.Select(x=>$"{x.PropertyName}: {x.ErrorMessage}"));
            }
            commands.Add(command);
        }
        if(section=="posting-profiles")
        {
            foreach(var line in rows.Where(x=>x.Source.Sheet=="Lines"))
                if(!headers.Any(x=>string.Equals(x.Get("Code"),line.Get("ProfileCode"),StringComparison.OrdinalIgnoreCase))) line.Errors.Add("ProfileCode غير موجود في ورقة PostingProfiles.");
        }
        var periods=section=="journals"?await List<FiscalPeriod>(ct):Array.Empty<FiscalPeriod>();
        if(section=="journals")
        {
            foreach(var group in headers.GroupBy(x=>x.Get("JournalKey"),StringComparer.OrdinalIgnoreCase))
            {
                var first=group.First();
                foreach(var row in group)
                {
                    foreach(var key in new[]{"JournalType","PostingDate","DocumentDate","Description"})
                        if(row.Get(key)!=first.Get(key)) row.Errors.Add($"{key} يجب أن يتطابق لجميع أسطر JournalKey.");
                    if(accountMap.TryGetValue(row.Get("AccountCode"),out var account) && (!account.CanReceiveManualPosting() || (account.EffectiveDate.HasValue && DateOnly.TryParse(row.Get("PostingDate"),out var posting) && account.EffectiveDate.Value>posting))) row.Errors.Add("الحساب لا يسمح بالترحيل اليدوي في هذا التاريخ.");
                    if(row.Get("CostCenterCode").Length>0 && (!centerMap.TryGetValue(row.Get("CostCenterCode"),out var center) || !center.IsActive)) row.Errors.Add("مركز التكلفة غير موجود أو غير نشط.");
                    if(row.Errors.Count==0 && (row.Decimal("Debit")<0 || row.Decimal("Credit")<0 || (row.Decimal("Debit")==0)==(row.Decimal("Credit")==0))) row.Errors.Add("السطر يجب أن يحتوي مبلغاً موجباً في المدين أو الدائن فقط.");
                }
                if(group.Any(x=>x.Errors.Count>0)) continue;
                if(group.Sum(x=>x.Decimal("Debit"))!=group.Sum(x=>x.Decimal("Credit"))) { foreach(var row in group) row.Errors.Add("القيد غير متوازن: مجموع المدين لا يساوي مجموع الدائن.");continue; }
                var matching=periods.Where(x=>x.StartDate<=first.Date("PostingDate") && x.EndDate>=first.Date("PostingDate")).ToArray();
                if(matching.Length!=1 || !matching[0].CanPostAccounting() || matching[0].Status!=OAS.Domain.Accounting.Enums.FiscalPeriodStatus.Open) { foreach(var row in group) row.Errors.Add("لا توجد فترة مالية واحدة مفتوحة مناسبة لتاريخ الترحيل.");continue; }
                var lines=group.Select(x=>new CreateJournalEntryLineRequest(accountMap[x.Get("AccountCode")].Id,x.Decimal("Debit"),x.Decimal("Credit"),x.Optional("LineDescription"),CostCenterId:x.Optional("CostCenterCode") is { } code?centerMap[code].Id:null)).ToArray();
                await Add(first,new CreateJournalEntryCommand(new(first.Enum<JournalType>("JournalType"),first.Date("PostingDate"),first.Date("DocumentDate"),matching[0].Id,first.Get("Description"),null,null,null,lines)));
            }
        }
        else foreach(var row in headers.Where(x=>x.Errors.Count==0))
        {
            var code=row.Get("Code");var parent=row.Get("ParentCode");
            object? command=section switch
            {
                "accounts"=>new CreateAccountCommand(new(code,row.Get("NameAr"),row.Optional("NameEn"),accountMap.GetValueOrDefault(parent)?.Id,(byte)levels[code],row.Enum<AccountClass>("AccountClass"),row.Enum<AccountType>("AccountType"),row.Enum<NormalBalance>("NormalBalance"),row.Bool("IsPostingAccount",true),row.Bool("IsControlAccount"),row.Bool("AllowManualPosting",true),row.Bool("IsSystemAccount"),row.Bool("IsActive",true),row.Optional("EffectiveDate") is null?null:row.Date("EffectiveDate"))),
                "cost-centers"=>new CreateCostCenterCommand(new(code,row.Get("NameAr"),row.Optional("NameEn"),centerMap.GetValueOrDefault(parent)?.Id,row.Bool("IsActive",true))),
                "cash-accounts"=>new CreateCashAccountCommand(new(code,row.Get("Name"),Resolve(row,"AccountCode")!.Value,row.Bool("IsDefault"),row.Bool("IsActive",true))),
                "bank-accounts"=>new CreateBankAccountCommand(new(code,row.Get("BankName"),row.Get("AccountName"),row.Get("AccountNumber"),row.Optional("IBAN"),Resolve(row,"AccountCode")!.Value,row.Bool("IsActive",true))),
                "expense-types"=>new CreateExpenseTypeCommand(new(code,row.Get("NameAr"),row.Optional("NameEn"),Resolve(row,"DefaultExpenseAccountCode",true),row.Bool("IsActive",true))),
                _=>null
            };
            if(section=="posting-profiles")
            {
                var lines=rows.Where(x=>x.Source.Sheet=="Lines" && string.Equals(x.Get("ProfileCode"),code,StringComparison.OrdinalIgnoreCase)).ToArray();
                if(lines.Length==0) row.Errors.Add("ملف الترحيل يحتاج أدواراً محاسبية.");
                foreach(var duplicate in lines.GroupBy(x=>x.Get("AccountRole"),StringComparer.OrdinalIgnoreCase).Where(x=>x.Count()>1)) foreach(var line in duplicate) line.Errors.Add("الدور المحاسبي مكرر داخل ملف الترحيل.");
                if(lines.Any(x=>x.Errors.Count>0) || row.Errors.Count>0) continue;
                command=new CreatePostingProfileCommand(new(code,row.Get("Name"),row.Get("Module"),row.Get("DocumentType"),lines.Select(x=>new CreatePostingProfileLineRequest(x.Get("AccountRole"),Resolve(x,"AccountCode")!.Value,x.Bool("IsRequired",true))).ToArray(),row.Bool("IsActive",true)));
            }
            if(command is not null) await Add(row,command);
        }
        return new(rows,commands);
    }
}
