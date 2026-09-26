using OAS.Application.Spreadsheets;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Enums;
namespace OAS.Application.Accounting.Spreadsheets;
public static class AccountingSpreadsheetDefinitions
{
    public static readonly string[] ImportSections = ["accounts","cost-centers","cash-accounts","bank-accounts","expense-types","posting-profiles","journals","customers","suppliers","currencies","exchange-rates"];
    public static readonly string[] ExportSections = [.. ImportSections,"fiscal-years","fiscal-periods","receipt-vouchers","payment-vouchers","payment-allocations","expenses","cash-shifts"];

    private static SpreadsheetColumn Text(string key, string header, bool required=false) => new(key,required,Header:header);
    private static SpreadsheetColumn Bool(string key, string header, bool value=false) => new(key,false,"bool",value ? "نعم" : "لا",["نعم","لا"],Header:header);
    private static SpreadsheetColumn Enum<T>(string key, string header) where T:struct,Enum => new(key,true,"enum",null,System.Enum.GetNames<T>(),Header:header);
    private static SpreadsheetColumn Date(string key,string header,bool required=false)=>new(key,required,"date",Format:"yyyy-mm-dd",Header:header);
    private static SpreadsheetColumn Decimal(string key,string header,bool required=false)=>new(key,required,"decimal",Header:header);

    public static string SheetName(string section) => section switch
    {
        "accounts"=>"الحسابات",
        "cost-centers"=>"مراكز التكلفة",
        "cash-accounts"=>"الصناديق",
        "bank-accounts"=>"الحسابات البنكية",
        "expense-types"=>"أنواع المصروفات",
        "posting-profiles"=>"ملفات الترحيل",
        "journals"=>"القيود",
        "fiscal-years"=>"السنوات المالية",
        "fiscal-periods"=>"الفترات المالية",
        "receipt-vouchers"=>"سندات القبض",
        "payment-vouchers"=>"سندات الصرف",
        "payment-allocations"=>"تخصيصات الدفعات",
        "expenses"=>"المصروفات",
        "cash-shifts"=>"ورديات الصندوق",
        "customers"=>"العملاء",
        "suppliers"=>"الموردون",
        "currencies"=>"العملات",
        "exchange-rates"=>"أسعار الصرف",
        _=>section
    };

    public static string Header(string key) => key switch
    {
        "Code"=>"الكود", "Name"=>"الاسم", "NameAr"=>"الاسم العربي", "NameEn"=>"الاسم الإنجليزي", "Level"=>"المستوى",
        "ParentCode"=>"كود الأب", "AccountClass"=>"تصنيف الحساب", "AccountType"=>"نوع الحساب", "NormalBalance"=>"طبيعة الرصيد",
        "IsPostingAccount"=>"حساب ترحيل", "IsControlAccount"=>"حساب مراقبة", "AllowManualPosting"=>"يسمح بالقيد اليدوي", "IsSystemAccount"=>"حساب نظام", "IsActive"=>"نشط", "EffectiveDate"=>"تاريخ السريان",
        "AccountCode"=>"كود الحساب العام", "CurrencyCode"=>"كود العملة", "Symbol"=>"رمز العملة", "DecimalPlaces"=>"المنازل العشرية",
        "RateDate"=>"تاريخ السعر", "Rate"=>"سعر الصرف", "RateType"=>"نوع السعر", "ExchangeRate"=>"سعر الصرف", "BaseDebit"=>"مدين أساسي", "BaseCredit"=>"دائن أساسي",
        "IsDefault"=>"افتراضي", "BankName"=>"اسم البنك", "AccountName"=>"اسم الحساب", "AccountNumber"=>"رقم الحساب", "IBAN"=>"الآيبان",
        "DefaultExpenseAccountCode"=>"كود حساب المصروف الافتراضي", "Module"=>"الموديول", "DocumentType"=>"نوع المستند", "ProfileCode"=>"كود ملف الترحيل", "AccountRole"=>"دور الحساب", "IsRequired"=>"إلزامي",
        "JournalKey"=>"مفتاح القيد", "JournalType"=>"نوع القيد", "PostingDate"=>"تاريخ الترحيل", "DocumentDate"=>"تاريخ المستند", "Description"=>"البيان", "Debit"=>"مدين", "Credit"=>"دائن", "CostCenterCode"=>"كود مركز التكلفة", "LineDescription"=>"بيان السطر",
        "Status"=>"الحالة", "JournalNumber"=>"رقم القيد", "PeriodNumber"=>"رقم الفترة", "StartDate"=>"تاريخ البداية", "EndDate"=>"تاريخ النهاية",
        "SalesLocked"=>"المبيعات مقفلة", "InventoryLocked"=>"المخزون مقفل", "AccountingLocked"=>"الحسابات مقفلة", "ClosedAtUtc"=>"وقت الإقفال", "ClosedBy"=>"مغلق بواسطة",
        "VoucherNumber"=>"رقم السند", "VoucherDate"=>"تاريخ السند", "PartyType"=>"نوع الطرف", "CustomerId"=>"معرف العميل", "SupplierId"=>"معرف المورد", "ReceivedFrom"=>"مستلم من", "BeneficiaryName"=>"اسم المستفيد", "PaymentMethod"=>"طريقة الدفع", "TotalAmount"=>"الإجمالي",
        "CashAccountCode"=>"كود الصندوق", "BankAccountCode"=>"كود الحساب البنكي", "JournalEntryCode"=>"رقم القيد", "JournalEntryNumber"=>"رقم القيد", "ReceiptVoucherId"=>"معرف سند القبض", "PaymentVoucherId"=>"معرف سند الصرف", "PostingProfileId"=>"معرف ملف الترحيل", "JournalEntryId"=>"معرف القيد", "ReferenceType"=>"نوع المرجع", "ReferenceId"=>"معرف المرجع", "LineNumber"=>"رقم السطر",
        "PaymentSourceType"=>"نوع مصدر الدفعة", "PaymentSourceNumber"=>"رقم مصدر الدفعة", "TargetDocumentType"=>"نوع المستند المستهدف", "TargetDocumentId"=>"معرف المستند المستهدف", "AllocatedAmount"=>"المبلغ المخصص", "AllocatedAtUtc"=>"وقت التخصيص", "CreatedBy"=>"أنشئ بواسطة", "CreatedAtUtc"=>"وقت الإنشاء",
        "ExpenseNumber"=>"رقم المصروف", "ExpenseDate"=>"تاريخ المصروف", "ExpenseTypeCode"=>"كود نوع المصروف", "ExpenseAccountCode"=>"كود حساب المصروف", "Beneficiary"=>"المستفيد", "Amount"=>"المبلغ", "PostedAtUtc"=>"وقت الترحيل",
        "ShiftNumber"=>"رقم الوردية", "OpenedBy"=>"فتح بواسطة", "OpenedAtUtc"=>"وقت الفتح", "OpeningBalance"=>"رصيد البداية", "ExpectedClosingBalance"=>"الرصيد المتوقع", "ActualClosingBalance"=>"الرصيد الفعلي", "DifferenceAmount"=>"الفرق", "CashAccountId"=>"معرف الصندوق", "BankAccountId"=>"معرف الحساب البنكي",
        "FiscalYearCode"=>"كود السنة المالية", "FiscalPeriodCode"=>"كود الفترة المالية", "FiscalYearId"=>"معرف السنة المالية", "FiscalPeriodId"=>"معرف الفترة المالية", "ExpenseTypeId"=>"معرف نوع المصروف", "ExpenseAccountId"=>"معرف حساب المصروف",
        "ParentAccountCode"=>"كود الحساب الأب", "ParentCostCenterCode"=>"كود مركز التكلفة الأب", "ReversedJournalCode"=>"رقم القيد المعكوس",
        "ApprovedBy"=>"اعتمد بواسطة", "ApprovedAtUtc"=>"وقت الاعتماد", "PostedBy"=>"رحّل بواسطة", "SourceModule"=>"الموديول المصدر", "SourceDocumentType"=>"نوع المستند المصدر", "SourceDocumentId"=>"معرف المستند المصدر",
        "CustomerCode"=>"كود العميل", "SupplierCode"=>"كود المورد", "EntityType"=>"نوع الطرف", "SupplierScope"=>"نطاق المورد", "TradeName"=>"الاسم التجاري",
        "NationalId"=>"رقم الهوية", "CommercialRegistrationNo"=>"السجل التجاري", "TaxNumber"=>"الرقم الضريبي", "DateOfBirth"=>"تاريخ الميلاد", "Gender"=>"الجنس",
        "ContactPersonName"=>"مسؤول التواصل", "ContactPersonTitle"=>"صفة مسؤول التواصل", "Phone"=>"الهاتف", "Mobile"=>"الجوال", "AlternatePhone"=>"هاتف بديل", "WhatsAppNumber"=>"واتساب",
        "Email"=>"البريد الإلكتروني", "Website"=>"الموقع", "PreferredContactMethod"=>"وسيلة التواصل المفضلة", "Country"=>"الدولة", "Governorate"=>"المحافظة", "City"=>"المدينة", "District"=>"الحي/المنطقة",
        "Street"=>"الشارع", "Building"=>"المبنى", "PostalCode"=>"الرمز البريدي", "AddressDetails"=>"تفاصيل العنوان", "IsCreditAllowed"=>"السماح بالآجل", "CreditLimit"=>"الحد الائتماني",
        "PaymentTermDays"=>"أيام السداد", "CustomerSince"=>"بداية التعامل", "SupplierSince"=>"بداية التعامل", "DefaultLeadTimeDays"=>"مدة التوريد الافتراضية", "Notes"=>"ملاحظات",
        _=>key
    };

    public static IReadOnlyList<SpreadsheetSheet> Get(string section) => section switch
    {
        "accounts" => [new("Accounts",[Text("Code","كود الحساب",true),Text("NameAr",Header("NameAr"),true),Text("NameEn",Header("NameEn")),Text("ParentCode","كود الحساب الأب"),Enum<AccountClass>("AccountClass",Header("AccountClass")),Enum<AccountType>("AccountType",Header("AccountType")),Enum<NormalBalance>("NormalBalance",Header("NormalBalance")),Bool("IsPostingAccount",Header("IsPostingAccount"),true),Bool("IsControlAccount",Header("IsControlAccount")),Bool("AllowManualPosting",Header("AllowManualPosting"),true),Bool("IsSystemAccount",Header("IsSystemAccount")),Bool("IsActive",Header("IsActive"),true),Date("EffectiveDate",Header("EffectiveDate"))],SheetName(section))],
        "cost-centers" => [new("CostCenters",[Text("Code","كود مركز التكلفة",true),Text("NameAr",Header("NameAr"),true),Text("NameEn",Header("NameEn")),Text("ParentCode","كود مركز التكلفة الأب"),Bool("IsActive",Header("IsActive"),true)],SheetName(section))],
        "cash-accounts" => [new("CashAccounts",[Text("Name",Header("Name"),true),Text("CurrencyCode",Header("CurrencyCode"),true),Bool("IsDefault",Header("IsDefault")),Bool("IsActive",Header("IsActive"),true)],SheetName(section))],
        "bank-accounts" => [new("BankAccounts",[Text("BankName",Header("BankName"),true),Text("AccountName",Header("AccountName"),true),Text("AccountNumber",Header("AccountNumber"),true),Text("IBAN",Header("IBAN")),Text("CurrencyCode",Header("CurrencyCode"),true),Bool("IsActive",Header("IsActive"),true)],SheetName(section))],
        "expense-types" => [new("ExpenseTypes",[Text("Code","كود نوع المصروف",true),Text("NameAr",Header("NameAr"),true),Text("NameEn",Header("NameEn")),Text("DefaultExpenseAccountCode",Header("DefaultExpenseAccountCode")),Bool("IsActive",Header("IsActive"),true)],SheetName(section))],
        "posting-profiles" => [new("PostingProfiles",[Text("Code","كود ملف الترحيل",true),Text("Name",Header("Name"),true),Text("Module",Header("Module"),true),Text("DocumentType",Header("DocumentType"),true),Bool("IsActive",Header("IsActive"),true)],SheetName(section)),new("Lines",[Text("ProfileCode",Header("ProfileCode"),true),Text("AccountRole",Header("AccountRole"),true),Text("AccountCode",Header("AccountCode"),true),Bool("IsRequired",Header("IsRequired"),true)],"أدوار الحسابات")],
        "journals" => [new("Journals",[Text("JournalKey",Header("JournalKey"),true),new("JournalType",true,"enum",null,["Manual","Opening","Closing"],Header:Header("JournalType")),Date("PostingDate",Header("PostingDate"),true),Date("DocumentDate",Header("DocumentDate"),true),Text("Description",Header("Description"),true),Text("AccountCode",Header("AccountCode"),true),Decimal("Debit",Header("Debit"),true),Decimal("Credit",Header("Credit"),true),Text("CurrencyCode",Header("CurrencyCode")),Decimal("ExchangeRate",Header("ExchangeRate")),Text("CostCenterCode",Header("CostCenterCode")),Text("LineDescription",Header("LineDescription"))],SheetName(section))],
        "customers" => [new("Customers",[Text("ParentAccountCode",Header("ParentAccountCode"),true),new("EntityType",true,"enum",null,["Individual","Organization"],Header:Header("EntityType")),Text("NameAr",Header("NameAr"),true),Text("NameEn",Header("NameEn")),Text("TradeName",Header("TradeName")),Text("NationalId",Header("NationalId")),Text("CommercialRegistrationNo",Header("CommercialRegistrationNo")),Text("TaxNumber",Header("TaxNumber")),Date("DateOfBirth",Header("DateOfBirth")),Enum<Gender>("Gender",Header("Gender")),Text("ContactPersonName",Header("ContactPersonName")),Text("ContactPersonTitle",Header("ContactPersonTitle")),Text("Phone",Header("Phone")),Text("Mobile",Header("Mobile")),Text("AlternatePhone",Header("AlternatePhone")),Text("WhatsAppNumber",Header("WhatsAppNumber")),Text("Email",Header("Email")),Text("Website",Header("Website")),new("PreferredContactMethod",true,"enum",null,["Phone","Mobile","WhatsApp","Email"],Header:Header("PreferredContactMethod")),Text("Country",Header("Country")),Text("Governorate",Header("Governorate")),Text("City",Header("City")),Text("District",Header("District")),Text("Street",Header("Street")),Text("Building",Header("Building")),Text("PostalCode",Header("PostalCode")),Text("AddressDetails",Header("AddressDetails")),Bool("IsCreditAllowed",Header("IsCreditAllowed")),Decimal("CreditLimit",Header("CreditLimit"),true),Decimal("PaymentTermDays",Header("PaymentTermDays"),true),Date("CustomerSince",Header("CustomerSince")),Bool("IsActive",Header("IsActive"),true),Text("Notes",Header("Notes"))],SheetName(section))],
        "suppliers" => [new("Suppliers",[Text("ParentAccountCode",Header("ParentAccountCode"),true),new("EntityType",true,"enum",null,["Individual","Organization"],Header:Header("EntityType")),new("SupplierScope",true,"enum",null,["Local","International"],Header:Header("SupplierScope")),Text("NameAr",Header("NameAr"),true),Text("NameEn",Header("NameEn")),Text("TradeName",Header("TradeName")),Text("NationalId",Header("NationalId")),Text("CommercialRegistrationNo",Header("CommercialRegistrationNo")),Text("TaxNumber",Header("TaxNumber")),Text("ContactPersonName",Header("ContactPersonName")),Text("ContactPersonTitle",Header("ContactPersonTitle")),Text("Phone",Header("Phone")),Text("Mobile",Header("Mobile")),Text("AlternatePhone",Header("AlternatePhone")),Text("WhatsAppNumber",Header("WhatsAppNumber")),Text("Email",Header("Email")),Text("Website",Header("Website")),new("PreferredContactMethod",true,"enum",null,["Phone","Mobile","WhatsApp","Email"],Header:Header("PreferredContactMethod")),Text("Country",Header("Country")),Text("Governorate",Header("Governorate")),Text("City",Header("City")),Text("District",Header("District")),Text("Street",Header("Street")),Text("Building",Header("Building")),Text("PostalCode",Header("PostalCode")),Text("AddressDetails",Header("AddressDetails")),Decimal("CreditLimit",Header("CreditLimit"),true),Decimal("PaymentTermDays",Header("PaymentTermDays"),true),Decimal("DefaultLeadTimeDays",Header("DefaultLeadTimeDays")),Date("SupplierSince",Header("SupplierSince")),Bool("IsActive",Header("IsActive"),true),Text("Notes",Header("Notes"))],SheetName(section))],
        "currencies" => [new("Currencies",[Text("Code",Header("Code"),true),Text("NameAr",Header("NameAr"),true),Text("NameEn",Header("NameEn")),Text("Symbol",Header("Symbol")),Decimal("DecimalPlaces",Header("DecimalPlaces"),true),Bool("IsActive",Header("IsActive"),true)],SheetName(section))],
        "exchange-rates" => [new("ExchangeRates",[Text("CurrencyCode",Header("CurrencyCode"),true),Date("RateDate",Header("RateDate"),true),Decimal("Rate",Header("Rate"),true),Enum<ExchangeRateType>("RateType",Header("RateType")),Bool("IsActive",Header("IsActive"),true)],SheetName(section))],
        _ => throw new NotFoundException("spreadsheet template",section)
    };
}
