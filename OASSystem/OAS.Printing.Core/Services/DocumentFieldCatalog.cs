using OAS.Printing.Core.Models;

namespace OAS.Printing.Core.Services;

public static class DocumentFieldCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<DocumentFieldDefinition>> Fields =
        new Dictionary<string, IReadOnlyList<DocumentFieldDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ReceiptVoucher"] =
            [
                new("VoucherNumber", "رقم السند"), new("VoucherDate", "تاريخ السند"),
                new("CustomerName", "العميل"), new("ReceivedFrom", "استلمنا من"),
                new("PaymentMethod", "طريقة القبض"), new("CashAccountName", "الصندوق"),
                new("BankAccountName", "البنك"), new("TotalAmount", "المبلغ"),
                new("TotalAmountWords", "المبلغ كتابة"), new("Description", "البيان"),
                new("Company.Name", "اسم المنشأة", "المنشأة"), new("Company.Phone", "هاتف المنشأة", "المنشأة")
            ],
            ["PaymentVoucher"] =
            [
                new("VoucherNumber", "رقم السند"), new("VoucherDate", "تاريخ السند"),
                new("SupplierName", "المورد"), new("BeneficiaryName", "المستفيد"),
                new("PaymentMethod", "طريقة الدفع"), new("CashAccountName", "الصندوق"),
                new("BankAccountName", "البنك"), new("TotalAmount", "المبلغ"),
                new("TotalAmountWords", "المبلغ كتابة"), new("Description", "البيان"),
                new("Company.Name", "اسم المنشأة", "المنشأة"), new("Company.Phone", "هاتف المنشأة", "المنشأة")
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
