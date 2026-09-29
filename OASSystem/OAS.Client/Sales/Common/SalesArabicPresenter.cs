using OAS.Contracts.Sales.Enums;

namespace OAS.Client.Sales.Common;

public static class SalesArabicPresenter
{
    public static string PrescriptionStatusText(PrescriptionStatus value) => value switch
    {
        PrescriptionStatus.Draft => "مسودة",
        PrescriptionStatus.Active => "نشطة",
        PrescriptionStatus.Superseded => "مستبدلة",
        PrescriptionStatus.Cancelled => "ملغاة",
        _ => value.ToString()
    };

    public static string OrderStatusText(CustomerOrderStatus value) => value switch
    {
        CustomerOrderStatus.Draft => "مسودة",
        CustomerOrderStatus.Confirmed => "مؤكد",
        CustomerOrderStatus.AwaitingStock => "بانتظار المخزون",
        CustomerOrderStatus.PartiallyAvailable => "متاح جزئيًا",
        CustomerOrderStatus.ReadyForProduction => "جاهز للإنتاج",
        CustomerOrderStatus.InProduction => "قيد الإنتاج",
        CustomerOrderStatus.ReadyForDelivery => "جاهز للتسليم",
        CustomerOrderStatus.Completed => "مكتمل",
        CustomerOrderStatus.Cancelled => "ملغى",
        _ => value.ToString()
    };

    public static string InvoiceStatusText(SalesInvoiceStatus value) => value switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.Confirmed => "مؤكدة",
        SalesInvoiceStatus.Posted => "مرحّلة",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        _ => value.ToString()
    };

    public static string StatusCss(string status) => status switch
    {
        "Active" or "Confirmed" or "Posted" or "Completed" or "ReadyForProduction" => "ui-sales-status--active",
        "AwaitingStock" or "PartiallyAvailable" or "InProduction" or "ReadyForDelivery" => "ui-sales-status--warning",
        "Cancelled" or "Superseded" => "ui-sales-status--cancelled",
        _ => "ui-sales-status--draft"
    };

    public static string EyeText(EyeSide value) => value == EyeSide.RightOD ? "العين اليمنى OD" : "العين اليسرى OS";
}
