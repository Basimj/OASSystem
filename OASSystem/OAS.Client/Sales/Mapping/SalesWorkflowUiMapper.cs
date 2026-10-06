using OAS.Contracts.Purchasing.CustomerDemand;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.OpticalJobs;
using OAS.Contracts.Sales.OrderOperations;
using OAS.UiLib.Core.Models.Sales;

namespace OAS.Client.Sales.Mapping;

public static class SalesWorkflowUiMapper
{
    public static UiSalesCheckoutModel ToUi(SalesCheckoutContextDto dto)
    {
        var availability = dto.Availability.Lines.ToDictionary(x => x.CustomerOrderLineId);
        var model = new UiSalesCheckoutModel
        {
            OrderId = dto.Order.Id,
            OrderCode = dto.Order.OrderCode,
            CustomerCode = dto.Customer.CustomerCode,
            CustomerName = dto.Customer.NameAr,
            Mobile = dto.Customer.Mobile,
            PrescriptionText = dto.PrescriptionContext.HasPrescription
                ? $"{dto.PrescriptionContext.LatestPrescriptionCode} / إصدار {dto.PrescriptionContext.LatestRevisionNumber}"
                : "لا يوجد فحص محفوظ",
            StatusText = OrderStatus(dto.Order.Status),
            CurrencyId = dto.Order.CurrencyId.ToString(),
            CurrencyCode = dto.Order.CurrencyCodeSnapshot,
            Subtotal = dto.Order.Subtotal,
            DiscountAmount = dto.Order.DiscountAmount,
            TaxAmount = dto.Order.TaxAmount,
            TotalAmount = dto.Order.TotalAmount,
            PaidAmount = dto.PaymentSummary.PaidAmount,
            ExistingAdvance = dto.PaymentSummary.ExistingAdvanceBalance,
            OutstandingAmount = dto.PaymentSummary.OutstandingAmount,
            PaymentPlan = dto.Order.PaymentPlan.ToString(),
            PaymentPlanLocked = dto.Order.Status != CustomerOrderStatus.Draft,
            CreditAllowed = dto.Credit.IsActive
                && dto.Credit.IsCreditAllowed
                && !string.IsNullOrWhiteSpace(dto.Credit.CustomerCode)
                && dto.Credit.AvailableCredit >= dto.Order.TotalAmount,
            CreditLimit = dto.Credit.CreditLimit,
            CurrentCreditExposure = dto.Credit.CurrentCreditExposure,
            AvailableCredit = dto.Credit.AvailableCredit,
            PaymentTermDays = dto.Credit.PaymentTermDays,
            RowVersion = dto.Order.RowVersion
        };

        foreach (var line in dto.Order.Lines.Where(x => x.IsActive).OrderBy(x => x.LineNumber))
        {
            availability.TryGetValue(line.Id, out var a);
            var status = a?.AvailabilityStatus ?? (line.ProductVariantId.HasValue ? "Unknown" : "Available");
            model.Lines.Add(new UiSalesCheckoutLineModel
            {
                LineId = line.Id,
                TypeText = LineType(line.LineType),
                ProductText = Display(line.ProductCode, line.ProductName) ?? line.DescriptionSnapshot,
                EyeText = line.PrescriptionEye.HasValue ? Eye(line.PrescriptionEye.Value) : null,
                OpticalSummary = Optical(line.OpticalSnapshot),
                WarehouseText = Display(line.WarehouseCode, line.WarehouseName),
                AvailabilityText = Availability(status, a?.ShortageQuantity ?? 0m),
                AvailabilityCss = AvailabilityCss(status, a?.ShortageQuantity ?? 0m),
                Quantity = line.Quantity,
                UnitPrice = line.ActualUnitPrice,
                FinalAmount = line.FinalAmount,
                RequiresProduction = line.RequiresProduction
            });
            if ((a?.ShortageQuantity ?? 0m) > 0m)
            {
                model.Shortages.Add(new UiCheckoutShortageModel
                {
                    CustomerOrderLineId = line.Id,
                    ProductText = Display(line.ProductCode, line.ProductName) ?? line.DescriptionSnapshot,
                    EyeText = line.PrescriptionEye.HasValue ? Eye(line.PrescriptionEye.Value) : null,
                    OpticalSummary = Optical(line.OpticalSnapshot),
                    ShortageQuantity = a!.ShortageQuantity,
                    RequiredDate = dto.Order.RequiredDate
                });
            }
        }
        return model;
    }

    public static UiOrderOperationsRowModel ToUi(CustomerOrderOperationsItemDto x) => new(
        x.CustomerOrderId,x.OrderCode,x.OrderDate.ToString("yyyy-MM-dd"),x.RequiredDate?.ToString("yyyy-MM-dd"),x.CustomerCode,x.CustomerName,x.Mobile,x.ItemsSummary,x.TotalLines,x.AvailableLines,x.ShortageLines,x.SupplySummary.SummaryText ?? SupplySummary(x.SupplySummary),OrderStatus(x.OperationalStatus),StatusCss(x.OperationalStatus.ToString()),x.RequiresProduction,x.OpticalJobCode,x.Technician,x.LastUpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));

    public static UiOrderOperationsDetailsModel ToUi(CustomerOrderOperationsDetailsDto x) => new()
    {
        OrderId=x.CustomerOrderId, OrderCode=x.OrderCode, CustomerText=$"{x.CustomerCode} - {x.CustomerName}", Mobile=x.Mobile,
        StatusText=OrderStatus(x.Status), StatusKey=x.Status.ToString(), RequiredDate=x.RequiredDate?.ToString("yyyy-MM-dd"), Notes=x.Notes,
        SupplyText=x.SupplySummary.SummaryText ?? SupplySummary(x.SupplySummary), JobId=x.OpticalJobId, JobCode=x.OpticalJobCode,
        Lines=x.Lines.Select(l=>new UiOrderLineOperationsModel(l.LineNumber,LineType(l.LineType),Display(l.ProductCode,l.ProductName) ?? l.ProductName,l.Eye.HasValue?Eye(l.Eye.Value):null,l.Quantity,l.WarehouseName,Availability(l.Availability.ToString(),l.ShortageQuantity),AvailabilityCss(l.Availability.ToString(),l.ShortageQuantity),l.ReservedQuantity,l.ShortageQuantity,SupplyStatus(l.ProcurementStatus),l.SupplierName,l.ExpectedDeliveryDate?.ToString("yyyy-MM-dd"),Optical(l.OpticalSnapshot),l.RequiresProduction)).ToList(),
        Timeline=x.Timeline.OrderByDescending(t=>t.OccurredAt).Select(t=>new UiTimelineItemModel(t.DisplayText,t.OccurredAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),t.SourceDocumentCode ?? t.Actor)).ToList()
    };

    public static UiDemandTrackingRowModel ToUi(CustomerDemandTrackingItemDto x) => new(
        x.PurchaseRequestLineId,x.RequestCode,x.RequestDate.ToString("yyyy-MM-dd"),x.CustomerOrderCode,x.CustomerCode,x.CustomerName,x.Mobile,
        Display(x.ProductCode,x.ProductName) ?? x.ProductName,x.Eye.HasValue?Eye(x.Eye.Value):null,x.OpticalSummary,x.WarehouseName,
        x.RequestedQuantity,x.AllocatedToPurchaseOrderQuantity,x.AcceptedReceivedQuantity,x.RemainingQuantity,x.PreferredSupplierName,x.ActualSupplierName,x.PurchaseOrderCode,
        x.ScheduledOrderAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),x.SentAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),x.ExpectedDeliveryDate?.ToString("yyyy-MM-dd"),x.LastReceiptDate?.ToString("yyyy-MM-dd"),
        DemandStatus(x.TrackingStatus),DemandCss(x.TrackingStatus),x.DelayDays,x.UnitPrice,x.ActualUnitCost);

    public static UiDemandTrackingDetailsModel ToUi(CustomerDemandTrackingDetailsDto x) => new()
    {
        LineId=x.Item.PurchaseRequestLineId, CustomerOrderId=x.Item.CustomerOrderId, RequestCode=x.Item.RequestCode, CustomerText=$"{x.Item.CustomerCode} - {x.Item.CustomerName}", Mobile=x.Item.Mobile, OrderId=x.Item.CustomerOrderId, OrderCode=x.Item.CustomerOrderCode,
        ProductVariantId=x.Item.ProductVariantId, ProductText=Display(x.Item.ProductCode,x.Item.ProductName) ?? x.Item.ProductName, OpticalSummary=Optical(x), TrackingText=DemandStatus(x.Item.TrackingStatus), TrackingKey=x.Item.TrackingStatus.ToString(),
        RowVersion=x.RowVersion, PreferredSupplierId=x.Item.PreferredSupplierId?.ToString() ?? string.Empty, PreferredSupplierDisplay=x.Item.PreferredSupplierName, ActualSupplier=x.Item.ActualSupplierName,
        ScheduledAt=x.Item.ScheduledOrderAtUtc, SentAt=x.Item.SentAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), Requested=x.Item.RequestedQuantity, Allocated=x.Item.AllocatedToPurchaseOrderQuantity, Received=x.Item.AcceptedReceivedQuantity, Remaining=x.Item.RemainingQuantity, Warehouse=x.Item.WarehouseName,
        PurchaseOrderId=x.Item.PurchaseOrderId, PurchaseOrderCode=x.Item.PurchaseOrderCode, ExpectedDate=x.Item.ExpectedDeliveryDate?.ToString("yyyy-MM-dd"), ExpectedDeliveryDateInput=x.Item.ExpectedDeliveryDate, LastReceiptDate=x.Item.LastReceiptDate?.ToString("yyyy-MM-dd"), UnitPrice=x.Item.UnitPrice, ActualUnitCost=x.Item.ActualUnitCost
    };

    public static UiOpticalJobRowModel ToUi(OpticalJobWorkQueueDto x) => new(x.Id,x.JobCode,x.CustomerOrderCode,Display(x.CustomerCode,x.CustomerName) ?? x.CustomerName ?? x.CustomerCode ?? "عميل",x.Mobile,x.RequiredDate?.ToString("yyyy-MM-dd"),x.FrameSummary,x.ODSummary,x.OSSummary,x.AssignedTechnicianName,OpticalStatus(x.Status),StatusCss(x.Status.ToString()));

    public static UiOpticalJobDetailsModel ToUi(OpticalJobDetailsDto x) => new()
    {
        Id=x.Id,JobCode=x.JobCode,OrderCode=x.CustomerOrderCode,CustomerText=Display(x.CustomerCode,x.CustomerName) ?? x.CustomerName ?? x.CustomerCode ?? "عميل",Mobile=x.Mobile,RequiredDate=x.RequiredDate?.ToString("yyyy-MM-dd"),StatusText=OpticalStatus(x.Status),StatusKey=x.Status.ToString(),Technician=x.AssignedTechnicianName,TechnicianId=x.AssignedTechnicianId?.ToString() ?? string.Empty,RowVersion=x.RowVersion,Notes=x.Notes,
        Lines=x.Lines.OrderBy(l=>l.LineNumber).Select(l=>new UiOpticalJobLineModel(l.LineNumber,LineType(l.LineType),Display(l.ProductCode,l.ProductName) ?? l.DescriptionSnapshot,l.Eye.HasValue?Eye(l.Eye.Value):null,l.Quantity,Optical(l.OpticalSnapshot),l.Notes)).ToList()
    };

    public static string OrderStatus(CustomerOrderStatus s) => s switch { CustomerOrderStatus.Draft=>"مسودة",CustomerOrderStatus.Confirmed=>"جديد",CustomerOrderStatus.AwaitingStock=>"بانتظار مخزون",CustomerOrderStatus.PartiallyAvailable=>"متاح جزئيًا",CustomerOrderStatus.ReadyForProduction=>"جاهز للمعمل",CustomerOrderStatus.InProduction=>"قيد التجهيز",CustomerOrderStatus.ReadyForDelivery=>"جاهز للتسليم",CustomerOrderStatus.Completed=>"مكتمل",CustomerOrderStatus.Cancelled=>"ملغى",_=>s.ToString()};
    public static string OpticalStatus(OpticalJobStatus s) => s switch { OpticalJobStatus.Approved=>"معتمد",OpticalJobStatus.AwaitingMaterials=>"بانتظار مواد",OpticalJobStatus.MaterialsAvailable=>"جاهز للبدء",OpticalJobStatus.MaterialsIssued=>"تم صرف المواد",OpticalJobStatus.InProduction=>"قيد العمل",OpticalJobStatus.AwaitingQC=>"بانتظار الفحص",OpticalJobStatus.QCPassed=>"اجتاز الفحص",OpticalJobStatus.ReadyForDelivery=>"جاهز للتسليم",OpticalJobStatus.Delivered=>"تم التسليم",OpticalJobStatus.Cancelled=>"ملغى",_=>s.ToString()};
    public static string DemandStatus(CustomerDemandTrackingStatus s)=>s switch { CustomerDemandTrackingStatus.New=>"جديدة",CustomerDemandTrackingStatus.AwaitingSupplier=>"بانتظار المورد",CustomerDemandTrackingStatus.Scheduled=>"مجدولة",CustomerDemandTrackingStatus.Ordered=>"تم طلبها",CustomerDemandTrackingStatus.DueToday=>"تصل اليوم",CustomerDemandTrackingStatus.Overdue=>"متأخرة",CustomerDemandTrackingStatus.PartiallyReceived=>"مستلمة جزئيًا",CustomerDemandTrackingStatus.Received=>"مستلمة",_=>s.ToString()};
    public static string DemandCss(CustomerDemandTrackingStatus s)=>s switch { CustomerDemandTrackingStatus.DueToday=>"is-info",CustomerDemandTrackingStatus.Overdue=>"is-danger",CustomerDemandTrackingStatus.PartiallyReceived=>"is-warning",CustomerDemandTrackingStatus.Received=>"is-success",_=>"is-neutral"};
    public static string StatusCss(string s)=>s.Contains("Ready",StringComparison.OrdinalIgnoreCase)||s.Contains("Completed",StringComparison.OrdinalIgnoreCase)||s.Contains("Delivered",StringComparison.OrdinalIgnoreCase)?"is-success":s.Contains("Cancelled",StringComparison.OrdinalIgnoreCase)||s.Contains("Overdue",StringComparison.OrdinalIgnoreCase)?"is-danger":s.Contains("Awaiting",StringComparison.OrdinalIgnoreCase)||s.Contains("Partial",StringComparison.OrdinalIgnoreCase)?"is-warning":"is-info";
    public static string LineType(SalesLineType t)=>t switch{SalesLineType.Frame=>"إطار",SalesLineType.Lens=>"عدسة",SalesLineType.Accessory=>"إكسسوار",SalesLineType.Service=>"خدمة",_=>"أخرى"};
    public static string Eye(EyeSide e)=>e switch{EyeSide.RightOD=>"OD",EyeSide.LeftOS=>"OS",_=>e.ToString()};
    public static string SupplyStatus(CustomerOrderSupplyStatus s)=>s switch{CustomerOrderSupplyStatus.None=>"—",CustomerOrderSupplyStatus.NotOrdered=>"لم يطلب",CustomerOrderSupplyStatus.Scheduled=>"مجدول",CustomerOrderSupplyStatus.Ordered=>"تم الطلب",CustomerOrderSupplyStatus.DueToday=>"يصل اليوم",CustomerOrderSupplyStatus.Overdue=>"متأخر",CustomerOrderSupplyStatus.PartiallyReceived=>"مستلم جزئيًا",CustomerOrderSupplyStatus.Received=>"مستلم",_=>s.ToString()};
    private static string SupplySummary(CustomerOrderSupplySummaryDto s)=>s.HasShortage?$"ناقص: {s.NotOrderedLines+s.ScheduledLines+s.OrderedLines+s.DueTodayLines+s.OverdueLines+s.PartiallyReceivedLines} • مستلم: {s.ReceivedLines}":"المواد مكتملة";
    private static string Availability(string status,decimal shortage)=>shortage>0?"ناقص":status.Contains("Reserved",StringComparison.OrdinalIgnoreCase)?"محجوز":status.Contains("Available",StringComparison.OrdinalIgnoreCase)?"متوفر":"متوفر";
    private static string AvailabilityCss(string status,decimal shortage)=>shortage>0?"is-danger":status.Contains("Reserved",StringComparison.OrdinalIgnoreCase)?"is-info":"is-success";
    private static string? Display(string? code,string? name)=>string.IsNullOrWhiteSpace(code)?name:string.IsNullOrWhiteSpace(name)?code:$"{code} - {name}";
    private static string? Optical(CustomerOrderLineOpticalSnapshotDto? x)=>x is null?null:$"{Eye(x.Eye)} • SPH {Fmt(x.SPH)} • CYL {Fmt(x.CYL)} • Axis {x.Axis?.ToString()??"—"} • ADD {Fmt(x.ADD)}" + (string.IsNullOrWhiteSpace(x.MaterialSnapshot)?"":$" • {x.MaterialSnapshot}") + (string.IsNullOrWhiteSpace(x.CoatingSnapshot)?"":$" • {x.CoatingSnapshot}");
    private static string? Optical(CustomerDemandTrackingDetailsDto x)=>x.Eye is null?x.Item.OpticalSummary:$"{Eye(x.Eye.Value)} • SPH {Fmt(x.SPH)} • CYL {Fmt(x.CYL)} • Axis {x.Axis?.ToString()??"—"} • ADD {Fmt(x.ADD)}" + (string.IsNullOrWhiteSpace(x.Material)?"":$" • {x.Material}") + (string.IsNullOrWhiteSpace(x.Coating)?"":$" • {x.Coating}");
    private static string Fmt(decimal? v)=>v?.ToString("0.##")??"—";
}
