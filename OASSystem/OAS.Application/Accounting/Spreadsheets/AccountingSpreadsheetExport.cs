using OAS.Application.Accounting.Accounts.Queries.GetAccounts;
using OAS.Application.Accounting.BankAccounts.Queries.GetBankAccounts;
using OAS.Application.Accounting.CashAccounts.Queries.GetCashAccounts;
using OAS.Application.Accounting.CashShifts.Queries.GetCashShifts;
using OAS.Application.Accounting.CostCenters.Queries.GetCostCenters;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Queries.GetExpenseTypes;
using OAS.Application.Accounting.Expenses.Queries.GetExpenses;
using OAS.Application.Accounting.FiscalPeriods.Queries.GetFiscalPeriods;
using OAS.Application.Accounting.FiscalYears.Queries.GetFiscalYears;
using OAS.Application.Accounting.Journals.Queries.GetJournalEntries;
using OAS.Application.Accounting.PaymentAllocations.Queries.GetPaymentAllocations;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVouchers;
using OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfiles;
using OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVouchers;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Accounting.ReceiptVouchers;
using System.Collections;
using System.Globalization;
using MediatR;
using OAS.Application.Spreadsheets;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
namespace OAS.Application.Accounting.Spreadsheets;
public sealed partial class AccountingSpreadsheetService
{
    private async Task<IReadOnlyList<object>> ReadAll<T>(Func<PageRequest,IRequest<PagedResult<T>>> query, PageRequest request,CancellationToken ct)
    {
        var result=new List<object>();
        for(var number=1;;number++)
        {
            var page=await sender.Send(query(request with { PageNumber=number,PageSize=PageRequest.MaximumPageSize }),ct);
            result.AddRange(page.Items.Cast<object>());
            if(page.Items.Count==0 || number*PageRequest.MaximumPageSize>=page.TotalCount) break;
        }
        return result;
    }
    public async Task<byte[]> ExportAsync(string section,PageRequest request,string? sourceType,CancellationToken ct)
    {
        await Authorize(section,"view",ct);
        var items=section switch
        {
            "accounts" => await ReadAll<AccountDto>(p=>new GetAccountsQuery(p),request,ct),
            "cost-centers" => await ReadAll<CostCenterDto>(p=>new GetCostCentersQuery(p),request,ct),
            "cash-accounts" => await ReadAll<CashAccountDto>(p=>new GetCashAccountsQuery(p),request,ct),
            "bank-accounts" => await ReadAll<BankAccountDto>(p=>new GetBankAccountsQuery(p),request,ct),
            "expense-types" => await ReadAll<ExpenseTypeDto>(p=>new GetExpenseTypesQuery(p),request,ct),
            "posting-profiles" => await ReadAll<PostingProfileDto>(p=>new GetPostingProfilesQuery(p),request,ct),
            "journals" => await ReadAll<JournalEntryDto>(p=>new GetJournalEntriesQuery(p),request,ct),
            "fiscal-years" => await ReadAll<FiscalYearDto>(p=>new GetFiscalYearsQuery(p),request,ct),
            "fiscal-periods" => await ReadAll<FiscalPeriodDto>(p=>new GetFiscalPeriodsQuery(p),request,ct),
            "receipt-vouchers" => await ReadAll<ReceiptVoucherDto>(p=>new GetReceiptVouchersQuery(p),request,ct),
            "payment-vouchers" => await ReadAll<PaymentVoucherDto>(p=>new GetPaymentVouchersQuery(p),request,ct),
            "payment-allocations" => await ReadAll<PaymentAllocationDto>(p=>new GetPaymentAllocationsQuery(p),request,ct),
            "expenses" => await ReadAll<ExpenseDto>(p=>new GetExpensesQuery(p),request,ct),
            "cash-shifts" => await ReadAll<CashShiftDto>(p=>new GetCashShiftsQuery(p),request,ct),
            _=>throw new NotFoundException("spreadsheet",section)
        };
        // The same queries/specifications as the screen are used for every matching page.
        if(section=="payment-allocations" && !string.IsNullOrWhiteSpace(sourceType))
            items=items.Where(x=>string.Equals(Value(x,"PaymentSourceType"),sourceType,StringComparison.OrdinalIgnoreCase)).ToArray();
        var accounts=(await List<Account>(ct)).ToDictionary(x=>x.Id,x=>x.Code);
        var centers=(await List<CostCenter>(ct)).ToDictionary(x=>x.Id,x=>x.Code);
        var cash=(await List<CashAccount>(ct)).ToDictionary(x=>x.Id,x=>x.Code);
        var banks=(await List<BankAccount>(ct)).ToDictionary(x=>x.Id,x=>x.Code);
        var expenseTypes=(await List<ExpenseType>(ct)).ToDictionary(x=>x.Id,x=>x.Code);
        var years=(await List<FiscalYear>(ct)).ToDictionary(x=>x.Id,x=>x.Code);
        var periods=(await List<FiscalPeriod>(ct)).ToDictionary(x=>x.Id,x=>$"{years.GetValueOrDefault(x.FiscalYearId)}/{x.PeriodNumber}");
        var journals=(await List<JournalEntry>(ct)).ToDictionary(x=>x.Id,x=>x.JournalNumber);
        var receipts=(await List<ReceiptVoucher>(ct)).ToDictionary(x=>x.Id,x=>x.VoucherNumber);
        var payments=(await List<PaymentVoucher>(ct)).ToDictionary(x=>x.Id,x=>x.VoucherNumber);
        var references=new Dictionary<string,Dictionary<Guid,string>>
        {
            ["ParentAccountId"]=accounts,["AccountId"]=accounts,["ExpenseAccountId"]=accounts,["DefaultExpenseAccountId"]=accounts,
            ["ParentCostCenterId"]=centers,["CostCenterId"]=centers,["CashAccountId"]=cash,["BankAccountId"]=banks,
            ["FiscalYearId"]=years,["FiscalPeriodId"]=periods,["ExpenseTypeId"]=expenseTypes,["JournalEntryId"]=journals,["ReversedJournalId"]=journals
        };
        Dictionary<string,string> Map(object item)
        {
            var values=new Dictionary<string,string>();
            foreach(var property in item.GetType().GetProperties())
            {
                if(property.Name is "RowVersion" or "Lines" or "Id") continue;
                var value=property.GetValue(item);var key=property.Name;
                if(references.TryGetValue(key,out var lookup))
                { key=key is "ParentAccountId" or "ParentCostCenterId"?"ParentCode":key[..^2]+"Code";values[key]=value is Guid id?lookup.GetValueOrDefault(id)??"":""; }
                else if(key=="PaymentSourceId" && value is Guid source)
                { values["PaymentSourceNumber"]=(Value(item,"PaymentSourceType")=="ReceiptVoucher"?receipts:payments).GetValueOrDefault(source)??""; }
                else values[key]=Format(value);
            }
            return values;
        }
        var tables=new List<SpreadsheetTable>();
        if(section=="journals")
        {
            var definition=AccountingSpreadsheetDefinitions.Get(section)[0];
            var rows=new List<IReadOnlyDictionary<string,string>>();
            foreach(var item in items.Cast<JournalEntryDto>())
            {
                var detail=await sender.Send(new OAS.Application.Accounting.Journals.Queries.GetJournalEntryById.GetJournalEntryByIdQuery(item.Id),ct);
                foreach(var line in detail.Lines)
                    rows.Add(new Dictionary<string,string> { ["JournalKey"]=item.JournalNumber,["JournalType"]=item.JournalType.ToString(),["PostingDate"]=Format(item.PostingDate),["DocumentDate"]=Format(item.DocumentDate),["Description"]=item.Description,["AccountCode"]=accounts.GetValueOrDefault(line.AccountId)??"",["Debit"]=Format(line.DebitAmount),["Credit"]=Format(line.CreditAmount),["CostCenterCode"]=line.CostCenterId is Guid id?centers.GetValueOrDefault(id)??"":"",["LineDescription"]=line.Description??"",["Status"]=item.Status.ToString(),["JournalNumber"]=item.JournalNumber });
            }
            tables.Add(new(definition with { Columns=[..definition.Columns,new("Status",Header:AccountingSpreadsheetDefinitions.Header("Status")),new("JournalNumber",Header:AccountingSpreadsheetDefinitions.Header("JournalNumber"))] },rows));
        }
        else if(section=="posting-profiles")
        {
            var definitions=AccountingSpreadsheetDefinitions.Get(section);
            tables.Add(new(definitions[0],items.Select(x=>(IReadOnlyDictionary<string,string>)Map(x)).ToArray()));
            var lines=new List<IReadOnlyDictionary<string,string>>();
            foreach(var item in items.Cast<PostingProfileDto>())
            {
                var detail=await sender.Send(new OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfileById.GetPostingProfileByIdQuery(item.Id),ct);
                lines.AddRange(detail.Lines.Select(x=>(IReadOnlyDictionary<string,string>)new Dictionary<string,string> { ["ProfileCode"]=item.Code,["AccountRole"]=x.AccountRole,["AccountCode"]=accounts.GetValueOrDefault(x.AccountId)??"",["IsRequired"]=Format(x.IsRequired) }));
            }
            tables.Add(new(definitions[1],lines));
        }
        else
        {
            var mapped=items.Select(x=>(IReadOnlyDictionary<string,string>)Map(x)).ToArray();
            SpreadsheetSheet definition;
            if(AccountingSpreadsheetDefinitions.ImportSections.Contains(section)) definition=AccountingSpreadsheetDefinitions.Get(section)[0];
            else
            {
                // Reflect the actual DTO, including on an empty result set, so headers never disappear.
                var type=ExportDtoType(section);
                var keys=type.GetProperties().Where(x=>x.Name is not ("Id" or "RowVersion" or "Lines")).Select(x=>references.ContainsKey(x.Name)?x.Name[..^2]+"Code":x.Name=="PaymentSourceId"?"PaymentSourceNumber":x.Name);
                definition=new(section.Replace("-",""),keys.Select(x=>new SpreadsheetColumn(x,DataType:ExportDataType(type,x),Header:AccountingSpreadsheetDefinitions.Header(x))).ToArray(),AccountingSpreadsheetDefinitions.SheetName(section));
            }
            tables.Add(new(definition,mapped));
            if(section is "receipt-vouchers" or "payment-vouchers")
            {
                var lineRows=new List<IReadOnlyDictionary<string,string>>();
                foreach(var item in items)
                {
                    var id=(Guid)item.GetType().GetProperty("Id")!.GetValue(item)!;
                    object detail=section=="receipt-vouchers"
                        ?await sender.Send(new OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById.GetReceiptVoucherByIdQuery(id),ct)
                        :await sender.Send(new OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById.GetPaymentVoucherByIdQuery(id),ct);
                    foreach(var line in (IEnumerable)detail.GetType().GetProperty("Lines")!.GetValue(detail)!)
                    { var row=Map(line);row["VoucherNumber"]=Value(item,"VoucherNumber");lineRows.Add(row); }
                }
                if(lineRows.Count>0) tables.Add(new(new("Lines",lineRows[0].Keys.Select(x=>new SpreadsheetColumn(x,Header:AccountingSpreadsheetDefinitions.Header(x))).ToArray(),"تفاصيل السند"),lineRows));
            }
        }
        return workbook.Write(tables);
    }
    private static string ExportDataType(Type dto,string key)
    {
        var type=dto.GetProperty(key)?.PropertyType;
        type=type is null?null:Nullable.GetUnderlyingType(type)??type;
        return type==typeof(decimal) || type==typeof(int) || type==typeof(byte)?"decimal":type==typeof(DateOnly)?"date":"text";
    }
    private static string Value(object item,string property)=>Format(item.GetType().GetProperty(property)?.GetValue(item));
    private static string Format(object? value)=>value switch { null=>"",bool b=>b?"نعم":"لا",DateOnly date=>date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),IFormattable f=>f.ToString(null,CultureInfo.InvariantCulture),_=>value.ToString()??"" };
    private static Type ExportDtoType(string section)=>section switch
    {
        "accounts"=>typeof(AccountDto),
        "cost-centers"=>typeof(CostCenterDto),
        "cash-accounts"=>typeof(CashAccountDto),
        "bank-accounts"=>typeof(BankAccountDto),
        "expense-types"=>typeof(ExpenseTypeDto),
        "posting-profiles"=>typeof(PostingProfileDto),
        "journals"=>typeof(JournalEntryDto),
        "fiscal-years"=>typeof(FiscalYearDto),
        "fiscal-periods"=>typeof(FiscalPeriodDto),
        "receipt-vouchers"=>typeof(ReceiptVoucherDto),
        "payment-vouchers"=>typeof(PaymentVoucherDto),
        "payment-allocations"=>typeof(PaymentAllocationDto),
        "expenses"=>typeof(ExpenseDto),
        "cash-shifts"=>typeof(CashShiftDto),
        _=>throw new NotFoundException("spreadsheet",section)
    };
}
