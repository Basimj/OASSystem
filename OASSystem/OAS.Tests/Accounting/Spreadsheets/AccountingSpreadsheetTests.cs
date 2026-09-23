using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OAS.Application;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Accounting.Spreadsheets;
using OAS.Application.Spreadsheets;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;
using OAS.Infrastructure.Spreadsheets;
using OAS.Tests.Accounting.Application.Common;
namespace OAS.Tests.Accounting.Spreadsheets;
[TestFixture]
public sealed class AccountingSpreadsheetTests
{
    private ServiceProvider _provider=null!;
    private AccountingSpreadsheetService Service=>_provider.GetRequiredService<AccountingSpreadsheetService>();
    private readonly ClosedXmlSpreadsheetWorkbook _workbook=new();
    private FakeRepository<T,Guid> Repo<T>() where T:Entity<Guid> => (FakeRepository<T,Guid>)_provider.GetRequiredService<IReadRepository<T,Guid>>();
    private static void Register<T>(IServiceCollection services) where T:Entity<Guid>
    { var repo=new FakeRepository<T,Guid>();services.AddSingleton<IReadRepository<T,Guid>>(repo);services.AddSingleton<IRepository<T,Guid>>(repo); }
    [SetUp] public async Task Setup()
    {
        var services=new ServiceCollection();services.AddLogging();services.AddApplication();
        services.AddSingleton<ISpreadsheetWorkbook>(_workbook);
        services.AddSingleton<IPermissionChecker,newPermissionChecker>();
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddSingleton<ISequenceNumberGenerator>(new FakeSequenceNumberGenerator());
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        Register<Account>(services);Register<CostCenter>(services);Register<CashAccount>(services);Register<BankAccount>(services);
        Register<ExpenseType>(services);Register<PostingProfile>(services);Register<PostingProfileLine>(services);Register<JournalEntry>(services);Register<JournalEntryLine>(services);
        Register<CashShift>(services);Register<Expense>(services);Register<PaymentAllocation>(services);Register<ReceiptVoucherLine>(services);Register<PaymentVoucherLine>(services);Register<FiscalPeriod>(services);Register<FiscalYear>(services);Register<ReceiptVoucher>(services);Register<PaymentVoucher>(services);Register<Customer>(services);Register<Supplier>(services);
        _provider=services.BuildServiceProvider();
        await Repo<Account>().AddAsync(Account.Create(Guid.NewGuid(),"1110","صندوق",null,null,1,AccountClass.Asset,AccountType.Posting,NormalBalance.Debit,true,false,true,false,true,null));
        await Repo<Account>().AddAsync(Account.Create(Guid.NewGuid(),"3000","رأس المال",null,null,1,AccountClass.Equity,AccountType.Posting,NormalBalance.Credit,true,false,true,false,true,null));
        await Repo<Account>().AddAsync(Account.Create(Guid.NewGuid(),"110200","العملاء",null,null,1,AccountClass.Asset,AccountType.Control,NormalBalance.Debit,false,true,false,false,true,null));
        await Repo<Account>().AddAsync(Account.Create(Guid.NewGuid(),"210200","الموردون",null,null,1,AccountClass.Liability,AccountType.Control,NormalBalance.Credit,false,true,false,false,true,null));
        await Repo<FiscalPeriod>().AddAsync(FiscalPeriod.Create(Guid.NewGuid(),Guid.NewGuid(),1,"2026",new(2026,1,1),new(2026,12,31),FiscalPeriodStatus.Open,false,false,false));
    }
    private sealed class newPermissionChecker:IPermissionChecker { public Task<bool> HasPermissionAsync(string permission,CancellationToken ct=default)=>Task.FromResult(true); }
    [TearDown] public void Teardown()=>_provider.Dispose();
    private static Dictionary<string,string> Row(params string[] pairs)
    { var result=new Dictionary<string,string>();for(var i=0;i<pairs.Length;i+=2) result[pairs[i]]=pairs[i+1];return result; }
    private byte[] Book(string section,params Dictionary<string,string>[] rows)
    {
        var definitions=AccountingSpreadsheetDefinitions.Get(section);
        return _workbook.Write(definitions.Select((d,i)=>new SpreadsheetTable(d,rows.Where(x=>i==0?!x.ContainsKey("ProfileCode"):x.ContainsKey("ProfileCode")).Cast<IReadOnlyDictionary<string,string>>().ToArray())).ToArray());
    }
    private static Dictionary<string,string> AccountRow(string code,string parent="")=>Row("Code",code,"NameAr","حساب "+code,"ParentCode",parent,"AccountClass","Asset","AccountType","Posting","NormalBalance","Debit");
    private static Dictionary<string,string> JournalRow(string debit,string credit,string key="JV1",string account="1110")=>Row("JournalKey",key,"JournalType","Manual","PostingDate","2026-09-21","DocumentDate","2026-09-21","Description","قيد","AccountCode",account,"Debit",debit,"Credit",credit);
    [TestCase("accounts")][TestCase("cost-centers")][TestCase("cash-accounts")][TestCase("bank-accounts")][TestCase("expense-types")][TestCase("posting-profiles")][TestCase("journals")][TestCase("customers")][TestCase("suppliers")]
    public async Task TemplateContainsDefinedSheetsAndNoGuids(string section)
    {
        using var book=new XLWorkbook(new MemoryStream(await Service.TemplateAsync(section,default)));
        foreach(var definition in AccountingSpreadsheetDefinitions.Get(section))
            Assert.That(book.Worksheet(definition.DisplayName ?? definition.Name).Row(1).CellsUsed().Select(x=>x.GetString()),Is.EqualTo(definition.Columns.Select(x=>x.Header ?? x.Key)));
        Assert.That(AccountingSpreadsheetDefinitions.Get(section).SelectMany(x=>x.Columns).Any(x=>x.Key.EndsWith("Id")),Is.False);
    }
    [Test] public async Task CustomerAndSupplierImportsGenerateOperationalAndAccountCodes()
    {
        var customerRow=Row("ParentAccountCode","110200","EntityType","Individual","NameAr","عميل اختبار","Gender","Unspecified","PreferredContactMethod","Mobile","Mobile","777000001","CreditLimit","0","PaymentTermDays","0","IsActive","نعم");
        var supplierRow=Row("ParentAccountCode","210200","EntityType","Organization","SupplierScope","Local","NameAr","مورد اختبار","PreferredContactMethod","Phone","Phone","0123456","CreditLimit","1000","PaymentTermDays","30","IsActive","نعم");

        var customerResult=await Service.ImportAsync("customers",Book("customers",customerRow),default);
        var supplierResult=await Service.ImportAsync("suppliers",Book("suppliers",supplierRow),default);

        Assert.That(customerResult.ImportedRecords,Is.EqualTo(1));
        Assert.That(supplierResult.ImportedRecords,Is.EqualTo(1));
        var customer=Repo<Customer>().Items.Single();
        var supplier=Repo<Supplier>().Items.Single();
        Assert.That(customer.CustomerCode,Does.StartWith("CUS-"));
        Assert.That(supplier.SupplierCode,Does.StartWith("SUP-"));
        Assert.That(Repo<Account>().Items.Single(x=>x.Id==customer.AccountId).Code,Does.StartWith("AR-"));
        Assert.That(Repo<Account>().Items.Single(x=>x.Id==supplier.AccountId).Code,Does.StartWith("AP-"));
    }

    [Test] public async Task AccountForwardParentResolvesAndLevelIsCalculated()
    {
        var bytes=Book("accounts",AccountRow("1201","1200"),AccountRow("1200"));
        var preview=await Service.PreviewAsync("accounts",bytes,default);
        Assert.That(preview.CanImport,Is.True,string.Join(";",preview.Rows.SelectMany(x=>x.Errors)));
        Assert.That(Repo<Account>().Items.Count,Is.EqualTo(4));
        var result=await Service.ImportAsync("accounts",bytes,default);
        Assert.That(result.ImportedRecords,Is.EqualTo(2));
        var parent=Repo<Account>().Items.Single(x=>x.Code=="1200");var child=Repo<Account>().Items.Single(x=>x.Code=="1201");
        Assert.That(child.ParentAccountId,Is.EqualTo(parent.Id));Assert.That(child.Level,Is.EqualTo(2));
    }
    [TestCase("1110","")][TestCase("1200","missing")][TestCase("1200","1200")]
    public async Task InvalidAccountsCannotSave(string code,string parent)
    { var result=await Service.ImportAsync("accounts",Book("accounts",AccountRow(code,parent)),default);Assert.That(result.InvalidRows,Is.GreaterThan(0));Assert.That(Repo<Account>().Items.Count,Is.EqualTo(4)); }
    [Test] public async Task CyclesAndFileDuplicatesAreRejected()
    {
        Assert.That((await Service.PreviewAsync("accounts",Book("accounts",AccountRow("A","B"),AccountRow("B","A")),default)).InvalidRows,Is.EqualTo(2));
        Assert.That((await Service.PreviewAsync("accounts",Book("accounts",AccountRow("A"),AccountRow("a")),default)).InvalidRows,Is.EqualTo(2));
    }
    [Test] public async Task CostCenterForwardParentAndDuplicateValidation()
    {
        var bytes=Book("cost-centers",Row("Code","C2","NameAr","ابن","ParentCode","C1"),Row("Code","C1","NameAr","أب"));
        Assert.That((await Service.ImportAsync("cost-centers",bytes,default)).ImportedRecords,Is.EqualTo(2));
        Assert.That(Repo<CostCenter>().Items.Single(x=>x.Code=="C2").ParentCostCenterId,Is.EqualTo(Repo<CostCenter>().Items.Single(x=>x.Code=="C1").Id));
        Assert.That((await Service.PreviewAsync("cost-centers",bytes,default)).InvalidRows,Is.EqualTo(2));
    }
    [Test] public async Task CashDuplicateAndMissingAccountRejected()
    {
        var row=Row("Code","C","Name","الصندوق","AccountCode","1110");
        Assert.That((await Service.ImportAsync("cash-accounts",Book("cash-accounts",row),default)).ImportedRecords,Is.EqualTo(1));
        Assert.That((await Service.PreviewAsync("cash-accounts",Book("cash-accounts",row),default)).InvalidRows,Is.EqualTo(1));
        row["Code"]="D";row["AccountCode"]="unknown";
        Assert.That((await Service.PreviewAsync("cash-accounts",Book("cash-accounts",row),default)).InvalidRows,Is.EqualTo(1));
    }
    [Test] public async Task BankNumberAndCodeDuplicatesRejected()
    {
        var row=Row("Code","B","BankName","بنك","AccountName","حساب","AccountNumber","001234","AccountCode","1110");
        Assert.That((await Service.ImportAsync("bank-accounts",Book("bank-accounts",row),default)).ImportedRecords,Is.EqualTo(1));
        Assert.That((await Service.PreviewAsync("bank-accounts",Book("bank-accounts",row),default)).Rows[0].Errors.Count,Is.EqualTo(2));
        row["Code"]="B2";Assert.That((await Service.PreviewAsync("bank-accounts",Book("bank-accounts",row),default)).InvalidRows,Is.EqualTo(1));
    }
    [Test] public async Task ExpenseTypeResolvesAccount()
    {
        var result=await Service.ImportAsync("expense-types",Book("expense-types",Row("Code","E","NameAr","مصروف","DefaultExpenseAccountCode","1110")),default);
        Assert.That(result.ImportedRecords,Is.EqualTo(1));Assert.That(Repo<ExpenseType>().Items.Single().DefaultExpenseAccountId,Is.EqualTo(Repo<Account>().Items.First().Id));
    }
    [Test] public async Task PostingProfileMapsLines()
    {
        var bytes=Book("posting-profiles",Row("Code","P","Name","ملف","Module","Sales","DocumentType","Invoice"),Row("ProfileCode","P","AccountRole","Revenue","AccountCode","3000"));
        var result=await Service.ImportAsync("posting-profiles",bytes,default);
        Assert.That(result.ImportedRecords,Is.EqualTo(1));Assert.That(Repo<PostingProfile>().Items.Single().Lines.Single().AccountId,Is.EqualTo(Repo<Account>().Items.Single(x=>x.Code=="3000").Id));
    }
    [Test] public async Task JournalGroupsCreateDraftOnlyWithGeneratedNumbers()
    {
        var result=await Service.ImportAsync("journals",Book("journals",JournalRow("50","0"),JournalRow("0","50",account:"3000"),JournalRow("20","0","JV2"),JournalRow("0","20","JV2","3000")),default);
        Assert.That(result.ImportedRecords,Is.EqualTo(2));
        Assert.That(Repo<JournalEntry>().Items.All(x=>x.Status==JournalEntryStatus.Draft && x.Lines.Count==2 && x.JournalNumber.StartsWith("JV-2026-")),Is.True);
    }
    [Test] public async Task UnbalancedAndInvalidJournalAccountsRejected()
    {
        Assert.That((await Service.PreviewAsync("journals",Book("journals",JournalRow("50","0"),JournalRow("0","49")),default)).InvalidRows,Is.EqualTo(2));
        Assert.That((await Service.PreviewAsync("journals",Book("journals",JournalRow("50","0",account:"missing"),JournalRow("0","50")),default)).CanImport,Is.False);
        Assert.That(Repo<JournalEntry>().Items,Is.Empty);
    }
    [Test] public async Task ConfirmationRevalidatesDuplicates()
    {
        var bytes=Book("cost-centers",Row("Code","C","NameAr","مركز"));
        Assert.That((await Service.PreviewAsync("cost-centers",bytes,default)).CanImport,Is.True);
        await Repo<CostCenter>().AddAsync(CostCenter.Create(Guid.NewGuid(),"C","مركز",null,null,true));
        Assert.That((await Service.ImportAsync("cost-centers",bytes,default)).InvalidRows,Is.EqualTo(1));
    }
    [Test] public async Task ExportIncludesEveryMatchingPageAndCodes()
    {
        for(var i=0;i<225;i++) await Repo<Account>().AddAsync(Account.Create(Guid.NewGuid(),$"X{i:000}","match",null,null,1,AccountClass.Asset,AccountType.Posting,NormalBalance.Debit,true,false,true,false,true,null));
        var bytes=await Service.ExportAsync("accounts",new PageRequest { Search="match",PageSize=25 },null,default);
        using var book=new XLWorkbook(new MemoryStream(bytes));Assert.That(book.Worksheet("الحسابات").RowsUsed().Count(),Is.EqualTo(226));
        Assert.That(book.Worksheet("الحسابات").Cell(1,1).GetString(),Is.EqualTo("كود الحساب"));
    }
        [TestCase("accounts")][TestCase("cost-centers")][TestCase("cash-accounts")][TestCase("bank-accounts")][TestCase("expense-types")][TestCase("posting-profiles")][TestCase("journals")][TestCase("customers")][TestCase("suppliers")]
    [TestCase("fiscal-years")][TestCase("fiscal-periods")][TestCase("receipt-vouchers")][TestCase("payment-vouchers")][TestCase("payment-allocations")][TestCase("expenses")][TestCase("cash-shifts")]
    public async Task EveryExportSectionProducesHeadersEvenWithNoMatches(string section)
    {
        var bytes=await Service.ExportAsync(section,new PageRequest { Search="no-matching-record" },null,default);
        using var book=new XLWorkbook(new MemoryStream(bytes));
        Assert.That(book.Worksheets.First().Row(1).CellsUsed().Any(),Is.True);
    }
    [TestCase("fiscal-years")][TestCase("fiscal-periods")][TestCase("receipt-vouchers")][TestCase("payment-vouchers")][TestCase("payment-allocations")][TestCase("expenses")][TestCase("cash-shifts")]
    public void ExportOnlySectionsRejectTemplates(string section)
    { Assert.ThrowsAsync<OAS.Application.Common.Exceptions.NotFoundException>(async()=>await Service.TemplateAsync(section,default)); }
    [Test] public async Task DecimalAmountsRemainNumericAndTextCodesKeepLeadingZeros()
    {
        var definitions=AccountingSpreadsheetDefinitions.Get("journals");
        var bytes=Book("journals",JournalRow("123.125","0",account:"00110"));
        using var book=new XLWorkbook(new MemoryStream(bytes));var sheet=book.Worksheet("القيود");
        Assert.That(sheet.Cell(2,7).DataType,Is.EqualTo(XLDataType.Number));
        Assert.That(sheet.Cell(2,6).GetString(),Is.EqualTo("00110"));
        Assert.That(_workbook.Read(bytes,definitions)[0].Values["Debit"],Is.EqualTo("123.125"));
        var invalid=await Service.PreviewAsync("journals",Book("journals",JournalRow("0.00001","0"),JournalRow("0","0.00001")),default);
        Assert.That(invalid.CanImport,Is.False);
    }
    [Test] public void MalformedWorkbooksProduceValidationErrors()
    {
        Assert.Throws<OAS.Application.Common.Exceptions.RequestValidationException>(()=>_workbook.Read([1,2,3],AccountingSpreadsheetDefinitions.Get("accounts")));
        var bytes=Book("accounts",AccountRow("X"));
        using var book=new XLWorkbook(new MemoryStream(bytes));book.Worksheet("الحسابات").Cell(2,1).FormulaA1="1+1";
        using var output=new MemoryStream();book.SaveAs(output);
        Assert.Throws<OAS.Application.Common.Exceptions.RequestValidationException>(()=>_workbook.Read(output.ToArray(),AccountingSpreadsheetDefinitions.Get("accounts")));
    }
    [Test] public async Task CostCenterCyclesAndMissingParentsRejected()
    {
        var result=await Service.PreviewAsync("cost-centers",Book("cost-centers",Row("Code","A","NameAr","أ","ParentCode","B"),Row("Code","B","NameAr","ب","ParentCode","A"),Row("Code","C","NameAr","ج","ParentCode","missing")),default);
        Assert.That(result.InvalidRows,Is.EqualTo(3));
    }
    [Test] public async Task JournalClosedPeriodAndDoubleSidedLineRejected()
    {
        Repo<FiscalPeriod>().Items.Single().SetLocks(false,false,true);
        Assert.That((await Service.PreviewAsync("journals",Book("journals",JournalRow("50","0"),JournalRow("0","50")),default)).CanImport,Is.False);
        Assert.That((await Service.PreviewAsync("journals",Book("journals",JournalRow("50","50")),default)).CanImport,Is.False);
    }
    [Test] public void EmployeeExportRetainsHeadersCodesDatesAndArabicBooleans()
    {
        var exporter=new OAS.Infrastructure.Features.Employees.Export.EmployeeExcelExporter(_workbook);
        var bytes=exporter.Export([new("001","أحمد","علي",null,null,null,null,null,null,null,"موظف",new DateTime(2026,9,21),true,false)]);
        using var book=new XLWorkbook(new MemoryStream(bytes));var sheet=book.Worksheet("الموظفون");
        Assert.That(sheet.Cell(1,1).GetString(),Is.EqualTo("كود الموظف"));Assert.That(sheet.Cell(2,1).GetString(),Is.EqualTo("001"));
        Assert.That(sheet.Cell(2,7).GetDateTime(),Is.EqualTo(new DateTime(2026,9,21)));Assert.That(sheet.Cell(2,13).GetString(),Is.EqualTo("نعم"));Assert.That(sheet.Cell(2,14).GetString(),Is.EqualTo("لا"));
    }
    [Test] public async Task ArbitraryExcelReturnsPreviewErrorInsteadOfThrowing()
    {
        using var book=new XLWorkbook();
        var sheet=book.Worksheets.Add("أي ملف");sheet.Cell(1,1).Value="عمود مختلف";sheet.Cell(2,1).Value="بيانات";
        using var stream=new MemoryStream();book.SaveAs(stream);
        var preview=await Service.PreviewAsync("accounts",stream.ToArray(),default);
        Assert.That(preview.CanImport,Is.False);
        Assert.That(preview.Rows.SelectMany(x=>x.Errors).Any(),Is.True);
    }
    [Test] public async Task AccountTemplateUsesArabicHeadersAndBooleanChoices()
    {
        using var book=new XLWorkbook(new MemoryStream(await Service.TemplateAsync("accounts",default)));
        var sheet=book.Worksheet("الحسابات");
        Assert.That(sheet.Cell(1,1).GetString(),Is.EqualTo("كود الحساب"));
        Assert.That(sheet.Cell(1,2).GetString(),Is.EqualTo("الاسم العربي"));
        Assert.That(sheet.Cell(1,8).GetString(),Is.EqualTo("حساب ترحيل"));
        Assert.That(book.Worksheets.Any(x=>x.Name=="التعليمات"),Is.True);
    }

}
