using OAS.Application.Spreadsheets;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Enums;
namespace OAS.Application.Accounting.Spreadsheets;
public static class AccountingSpreadsheetDefinitions
{
    public static readonly string[] ImportSections = ["accounts","cost-centers","cash-accounts","bank-accounts","expense-types","posting-profiles","journals"];
    public static readonly string[] ExportSections = [.. ImportSections,"fiscal-years","fiscal-periods","receipt-vouchers","payment-vouchers","payment-allocations","expenses","cash-shifts"];
    private static SpreadsheetColumn Text(string key, bool required=false) => new(key,required);
    private static SpreadsheetColumn Bool(string key, bool value=false) => new(key,false,"bool",value.ToString(),["True","False"]);
    private static SpreadsheetColumn Enum<T>(string key) where T:struct,Enum => new(key,true,"enum",null,System.Enum.GetNames<T>());
    public static IReadOnlyList<SpreadsheetSheet> Get(string section) => section switch
    {
        "accounts" => [new("Accounts",[Text("Code",true),Text("NameAr",true),Text("NameEn"),Text("ParentCode"),Enum<AccountClass>("AccountClass"),Enum<AccountType>("AccountType"),Enum<NormalBalance>("NormalBalance"),Bool("IsPostingAccount",true),Bool("IsControlAccount"),Bool("AllowManualPosting",true),Bool("IsSystemAccount"),Bool("IsActive",true),new("EffectiveDate",false,"date")])],
        "cost-centers" => [new("CostCenters",[Text("Code",true),Text("NameAr",true),Text("NameEn"),Text("ParentCode"),Bool("IsActive",true)])],
        "cash-accounts" => [new("CashAccounts",[Text("Code",true),Text("Name",true),Text("AccountCode",true),Bool("IsDefault"),Bool("IsActive",true)])],
        "bank-accounts" => [new("BankAccounts",[Text("Code",true),Text("BankName",true),Text("AccountName",true),Text("AccountNumber",true),Text("IBAN"),Text("AccountCode",true),Bool("IsActive",true)])],
        "expense-types" => [new("ExpenseTypes",[Text("Code",true),Text("NameAr",true),Text("NameEn"),Text("DefaultExpenseAccountCode"),Bool("IsActive",true)])],
        "posting-profiles" => [new("PostingProfiles",[Text("Code",true),Text("Name",true),Text("Module",true),Text("DocumentType",true),Bool("IsActive",true)]),new("Lines",[Text("ProfileCode",true),Text("AccountRole",true),Text("AccountCode",true),Bool("IsRequired",true)])],
        "journals" => [new("Journals",[Text("JournalKey",true),new("JournalType",true,"enum",null,["Manual","Opening","Closing"]),new("PostingDate",true,"date"),new("DocumentDate",true,"date"),Text("Description",true),Text("AccountCode",true),new("Debit",true,"decimal"),new("Credit",true,"decimal"),Text("CostCenterCode"),Text("LineDescription")])],
        _ => throw new NotFoundException("spreadsheet template",section)
    };
}
