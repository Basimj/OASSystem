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
                new("TotalAmount", "الإجمالي الأساسي", "الإجماليات"),
                new("TotalAmountWords", "المبلغ كتابة", "الإجماليات"),
                new("Company.Name", "اسم المنشأة", "المنشأة"),
                new("Company.Address", "عنوان المنشأة", "المنشأة"),
                new("Company.Phone", "هاتف المنشأة", "المنشأة"),
                new("Company.CommercialRegistration", "السجل التجاري", "المنشأة"),
                new("Company.TaxNumber", "الرقم الضريبي", "المنشأة"),
                new("PrintedAt", "وقت الطباعة", "الطباعة")
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
                new("TotalAmount", "الإجمالي الأساسي", "الإجماليات"),
                new("TotalAmountWords", "المبلغ كتابة", "الإجماليات"),
                new("Company.Name", "اسم المنشأة", "المنشأة"),
                new("Company.Address", "عنوان المنشأة", "المنشأة"),
                new("Company.Phone", "هاتف المنشأة", "المنشأة"),
                new("Company.CommercialRegistration", "السجل التجاري", "المنشأة"),
                new("Company.TaxNumber", "الرقم الضريبي", "المنشأة"),
                new("PrintedAt", "وقت الطباعة", "الطباعة")
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
