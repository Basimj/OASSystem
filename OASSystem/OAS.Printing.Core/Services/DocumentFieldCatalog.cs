using OAS.Printing.Core.Models;

namespace OAS.Printing.Core.Services;

public static class DocumentFieldCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<DocumentFieldDefinition>> Fields =
        new Dictionary<string, IReadOnlyList<DocumentFieldDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ReceiptVoucher"] =
            [
                new("VoucherNumber", "رقم السند", "السند"),
                new("VoucherDate", "تاريخ السند", "السند"),
                new("StatusText", "الحالة", "السند"),
                new("Description", "البيان العام", "السند"),
                new("CustomerName", "العميل", "الطرف"),
                new("ReceivedFrom", "استلمنا من", "الطرف"),
                new("PaymentMethod", "طرق القبض", "الدفع"),
                new("CashAccountName", "الصندوق", "الدفع"),
                new("BankAccountName", "البنك", "الدفع"),
                new("BaseCurrencyCode", "كود العملة الأساسية", "العملة"),
                new("BaseCurrencyName", "اسم العملة الأساسية", "العملة"),
                new("BaseCurrencySymbol", "رمز العملة الأساسية", "العملة"),
                new("BaseCurrencyDecimalPlaces", "منازل العملة العشرية", "العملة"),
                new("TotalAmount", "الإجمالي الأساسي", "الإجماليات"),
                new("TotalAmountWords", "المبلغ كتابة", "الإجماليات"),
                new("Company.Name", "اسم المنشأة", "المنشأة"),
                new("Company.Address", "عنوان المنشأة", "المنشأة"),
                new("Company.Phone", "هاتف المنشأة", "المنشأة"),
                new("Company.CommercialRegistration", "السجل التجاري", "المنشأة"),
                new("Company.TaxNumber", "الرقم الضريبي", "المنشأة"),
                new("CreatedBy", "أنشئ بواسطة", "التدقيق"),
                new("CreatedAt", "وقت الإنشاء", "التدقيق"),
                new("PrintedAt", "وقت الطباعة", "الطباعة"),
                new("Signatures.Accountant", "توقيع المحاسب", "التواقيع"),
                new("Signatures.Receiver", "توقيع المستلم", "التواقيع"),
                new("Signatures.Approval", "توقيع الاعتماد", "التواقيع")
            ],
            ["PaymentVoucher"] =
            [
                new("VoucherNumber", "رقم السند", "السند"),
                new("VoucherDate", "تاريخ السند", "السند"),
                new("StatusText", "الحالة", "السند"),
                new("Description", "البيان العام", "السند"),
                new("SupplierName", "المورد", "الطرف"),
                new("BeneficiaryName", "المستفيد", "الطرف"),
                new("PaymentMethod", "طرق الدفع", "الدفع"),
                new("CashAccountName", "الصندوق", "الدفع"),
                new("BankAccountName", "البنك", "الدفع"),
                new("BaseCurrencyCode", "كود العملة الأساسية", "العملة"),
                new("BaseCurrencyName", "اسم العملة الأساسية", "العملة"),
                new("BaseCurrencySymbol", "رمز العملة الأساسية", "العملة"),
                new("BaseCurrencyDecimalPlaces", "منازل العملة العشرية", "العملة"),
                new("TotalAmount", "الإجمالي الأساسي", "الإجماليات"),
                new("TotalAmountWords", "المبلغ كتابة", "الإجماليات"),
                new("Company.Name", "اسم المنشأة", "المنشأة"),
                new("Company.Address", "عنوان المنشأة", "المنشأة"),
                new("Company.Phone", "هاتف المنشأة", "المنشأة"),
                new("Company.CommercialRegistration", "السجل التجاري", "المنشأة"),
                new("Company.TaxNumber", "الرقم الضريبي", "المنشأة"),
                new("CreatedBy", "أنشئ بواسطة", "التدقيق"),
                new("CreatedAt", "وقت الإنشاء", "التدقيق"),
                new("PrintedAt", "وقت الطباعة", "الطباعة"),
                new("Signatures.Accountant", "توقيع المحاسب", "التواقيع"),
                new("Signatures.Beneficiary", "توقيع المستفيد", "التواقيع"),
                new("Signatures.Approval", "توقيع الاعتماد", "التواقيع")
            ],

            ["Payslip"] =
            [
                new("PayslipNumber", "رقم القسيمة", "القسيمة"),
                new("PeriodCode", "فترة الرواتب", "القسيمة"),
                new("EmployeeCode", "كود الموظف", "الموظف"),
                new("EmployeeName", "اسم الموظف", "الموظف"),
                new("Department", "القسم", "الموظف"),
                new("JobTitle", "المسمى الوظيفي", "الموظف"),
                new("CoverageFrom", "بداية التغطية", "الفترة"),
                new("CoverageTo", "نهاية التغطية", "الفترة"),
                new("CurrencyCode", "العملة", "الإجماليات"),
                new("ConfiguredBasicSalary", "الراتب الأساسي المهيأ", "الإجماليات"),
                new("CalculatedBasicSalary", "الراتب الأساسي المحتسب", "الإجماليات"),
                new("GrossEarnings", "إجمالي الاستحقاقات", "الإجماليات"),
                new("TotalDeductions", "إجمالي الخصومات", "الإجماليات"),
                new("TotalEmployerContributions", "مساهمات صاحب العمل", "الإجماليات"),
                new("NetPay", "صافي الراتب", "الإجماليات"),
                new("PaidAmount", "المدفوع", "الدفع"),
                new("OutstandingAmount", "المتبقي", "الدفع"),
                new("PaymentStatus", "حالة الدفع", "الدفع"),
                new("JournalNumber", "رقم القيد", "المحاسبة"),
                new("PaymentReferences", "مراجع الدفع", "الدفع"),
                new("Company.Name", "اسم المنشأة", "المنشأة"),
                new("Company.Address", "عنوان المنشأة", "المنشأة"),
                new("Company.Phone", "هاتف المنشأة", "المنشأة"),
                new("PrintedAt", "وقت الطباعة", "الطباعة"),
                new("Signatures.Employee", "توقيع الموظف", "التواقيع"),
                new("Signatures.Accountant", "توقيع المحاسب", "التواقيع"),
                new("Signatures.Approval", "توقيع الاعتماد", "التواقيع")
            ],

            ["SalesInvoice"] =
            [
                new("InvoiceNumber", "رقم الفاتورة", "الفاتورة"),
                new("InvoiceDate", "تاريخ الفاتورة", "الفاتورة"),
                new("PostingDate", "تاريخ الترحيل", "الفاتورة"),
                new("StatusText", "الحالة", "الفاتورة"),
                new("SaleType", "نوع البيع فوري/آجل", "السداد"),
                new("PaymentTermDays", "أيام الأجل", "السداد"),
                new("DueDate", "تاريخ الاستحقاق", "السداد"),
                new("CustomerCode", "كود العميل", "العميل"),
                new("CustomerName", "اسم العميل", "العميل"),
                new("CustomerOrderNumber", "رقم طلب العميل", "المراجع"),
                new("PrescriptionNumber", "رقم الوصفة", "المراجع"),
                new("JournalEntryNumber", "رقم القيد", "المراجع"),
                new("CurrencyCode", "العملة", "العملة"),
                new("CurrencyName", "اسم العملة", "العملة"),
                new("CurrencySymbol", "رمز العملة", "العملة"),
                new("ExchangeRate", "سعر الصرف", "العملة"),
                new("ExchangeRateDate", "تاريخ سعر الصرف", "العملة"),
                new("TaxCalculationMode", "طريقة احتساب الضريبة", "الضريبة"),
                new("Subtotal", "الإجمالي قبل الخصم", "الإجماليات"),
                new("DiscountAmount", "الخصم", "الإجماليات"),
                new("TotalAfterDiscount", "الإجمالي بعد الخصم", "الإجماليات"),
                new("TaxAmount", "الضريبة", "الإجماليات"),
                new("TotalAmount", "المبلغ النهائي", "الإجماليات"),
                new("PaidAmount", "المبلغ المدفوع", "الإجماليات"),
                new("OutstandingAmount", "المبلغ المتبقي", "الإجماليات"),
                new("TotalAmountWords", "المبلغ كتابة", "الإجماليات"),
                new("Description", "الوصف/الملاحظات", "الفاتورة"),
                new("Company.Name", "اسم المنشأة", "المنشأة"),
                new("Company.Address", "عنوان المنشأة", "المنشأة"),
                new("Company.Phone", "هاتف المنشأة", "المنشأة"),
                new("Company.CommercialRegistration", "السجل التجاري", "المنشأة"),
                new("Company.TaxNumber", "الرقم الضريبي", "المنشأة"),
                new("CreatedBy", "أنشئت بواسطة", "التدقيق"),
                new("CreatedAt", "وقت الإنشاء", "التدقيق"),
                new("PostedBy", "رُحلت بواسطة", "التدقيق"),
                new("PostedAt", "وقت الترحيل", "التدقيق"),
                new("PrintedAt", "وقت الطباعة", "الطباعة"),
                new("Signatures.Cashier", "توقيع أمين الصندوق", "التواقيع"),
                new("Signatures.Accountant", "توقيع المحاسب", "التواقيع"),
                new("Signatures.SalesManager", "توقيع مدير المبيعات", "التواقيع"),
                new("Signatures.Customer", "استلام العميل", "التواقيع")

            ],
            ["Expense"] =
            [
                new("ExpenseNumber", "رقم المصروف"), new("ExpenseDate", "التاريخ"),
                new("ExpenseTypeName", "نوع المصروف"), new("Beneficiary", "المستفيد"),
                new("Amount", "المبلغ"), new("AmountWords", "المبلغ كتابة"),
                new("PaymentMethod", "طريقة الدفع"), new("Description", "البيان")
            ],
            ["JournalEntry"] =
            [
                new("JournalNumber", "رقم القيد"), new("PostingDate", "تاريخ الترحيل"),
                new("Description", "البيان"), new("Status", "الحالة"),
                new("TotalDebit", "إجمالي المدين"), new("TotalCredit", "إجمالي الدائن")
            ]
        };

    public static IReadOnlyList<string> DocumentTypes => Fields.Keys.OrderBy(x => x).ToArray();

    public static IReadOnlyList<DocumentFieldDefinition> GetFields(string? documentType)
    {
        if (documentType is not null && Fields.TryGetValue(documentType, out var fields))
            return fields;
        return [];
    }
}
