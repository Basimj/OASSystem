using OAS.Contracts.Purchasing.Enums;

namespace OAS.Client.Purchasing.Common;

public static class PurchasingArabicPresenter
{
    public static string RequestStatus(PurchaseRequestStatus s)=>s switch{PurchaseRequestStatus.Draft=>"مسودة",PurchaseRequestStatus.PendingApproval=>"بانتظار الاعتماد",PurchaseRequestStatus.Approved=>"معتمد",PurchaseRequestStatus.PartiallyConverted=>"محول جزئياً",PurchaseRequestStatus.Converted=>"محول بالكامل",PurchaseRequestStatus.Rejected=>"مرفوض",PurchaseRequestStatus.Cancelled=>"ملغي",_=>s.ToString()};
    public static string OrderStatus(PurchaseOrderStatus s)=>s switch{PurchaseOrderStatus.Draft=>"مسودة",PurchaseOrderStatus.PendingApproval=>"بانتظار الاعتماد",PurchaseOrderStatus.Approved=>"معتمد",PurchaseOrderStatus.Sent=>"مرسل للمورد",PurchaseOrderStatus.PartiallyReceived=>"مستلم جزئياً",PurchaseOrderStatus.FullyReceived=>"مستلم بالكامل",PurchaseOrderStatus.Closed=>"مغلق",PurchaseOrderStatus.Rejected=>"مرفوض",PurchaseOrderStatus.Cancelled=>"ملغي",_=>s.ToString()};
    public static string ReceiptStatus(PurchaseReceiptStatus s)=>s switch{PurchaseReceiptStatus.Draft=>"مسودة",PurchaseReceiptStatus.Confirmed=>"مؤكد",PurchaseReceiptStatus.Posted=>"مرحّل",PurchaseReceiptStatus.Cancelled=>"ملغي",_=>s.ToString()};
    public static string InvoiceStatus(PurchaseInvoiceStatus s)=>s switch{PurchaseInvoiceStatus.Draft=>"مسودة",PurchaseInvoiceStatus.Confirmed=>"مؤكد",PurchaseInvoiceStatus.PendingMatchApproval=>"بانتظار اعتماد الفروقات",PurchaseInvoiceStatus.Posted=>"مرحّل",PurchaseInvoiceStatus.Cancelled=>"ملغي",_=>s.ToString()};
    public static string MatchStatus(PurchaseMatchStatus s)=>s switch{PurchaseMatchStatus.Pending=>"قيد المطابقة",PurchaseMatchStatus.Matched=>"مطابق",PurchaseMatchStatus.WithinTolerance=>"ضمن حدود السماح",PurchaseMatchStatus.RequiresApproval=>"يحتاج اعتماد",PurchaseMatchStatus.ApprovedVariance=>"فرق معتمد",PurchaseMatchStatus.Rejected=>"مرفوض",_=>s.ToString()};
    public static string VarianceType(PurchaseVarianceType t)=>t switch{PurchaseVarianceType.Quantity=>"الكمية",PurchaseVarianceType.Price=>"السعر",PurchaseVarianceType.Tax=>"الضريبة",_=>"أخرى"};
}
