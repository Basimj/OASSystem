using OAS.Client.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.UiLib.Core.Models.Sales;

namespace OAS.Client.Sales.Mapping;

public static class SalesUiMapper
{
    public static UiPrescriptionFormModel ToUi(PrescriptionDto dto) => new()
    {
        Id = dto.Id,
        PrescriptionCode = dto.PrescriptionCode,
        CustomerId = dto.CustomerId.ToString(),
        CustomerDisplay = Display(dto.CustomerCode, dto.CustomerName),
        PrescriptionDate = dto.PrescriptionDate,
        Status = dto.Status.ToString(),
        StatusText = SalesArabicPresenter.PrescriptionStatusText(dto.Status),
        PrescribedBy = dto.PrescribedBy,
        ClinicName = dto.ClinicName,
        Notes = dto.Notes,
        IsActive = dto.IsActive,
        RowVersion = dto.RowVersion,
        Revisions = dto.Revisions.Select(ToUi).ToList()
    };

    public static UiPrescriptionRevisionModel ToUi(PrescriptionRevisionDto dto) => new()
    {
        Id = dto.Id,
        RevisionNumber = dto.RevisionNumber,
        EffectiveDate = dto.EffectiveDate,
        Reason = dto.Reason,
        IsCurrent = dto.IsCurrent,
        RowVersion = dto.RowVersion,
        Eyes = dto.EyeDetails.Select(x => new UiPrescriptionEyeModel
        {
            Eye = x.Eye.ToString(),
            EyeText = SalesArabicPresenter.EyeText(x.Eye),
            SPH = x.SPH,
            CYL = x.CYL,
            Axis = x.Axis,
            ADD = x.ADD,
            Prism = x.Prism,
            PrismBase = (x.PrismBase ?? PrismBaseDirection.None).ToString(),
            PD = x.PD,
            MonocularPD = x.MonocularPD,
            VA = x.VA,
            FittingHeight = x.FittingHeight,
            Notes = x.Notes
        }).ToList()
    };

    public static UiCustomerOrderFormModel ToUi(CustomerOrderDto dto) => new()
    {
        Id = dto.Id,
        OrderCode = dto.OrderCode,
        CustomerId = dto.CustomerId.ToString(),
        CustomerCode = dto.CustomerCode ?? string.Empty,
        CustomerDisplay = Display(dto.CustomerCode, dto.CustomerName),
        PrescriptionRevisionId = dto.PrescriptionRevisionId?.ToString() ?? string.Empty,
        PrescriptionCode = dto.PrescriptionCode ?? string.Empty,
        PrescriptionDisplay = PrescriptionDisplay(dto.PrescriptionCode, dto.PrescriptionRevisionNumber),
        OrderDate = dto.OrderDate,
        RequiredDate = dto.RequiredDate,
        Status = dto.Status.ToString(),
        StatusText = SalesArabicPresenter.OrderStatusText(dto.Status),
        CurrencyId = dto.CurrencyId.ToString(),
        CurrencyCode = dto.CurrencyCodeSnapshot,
        CurrencyDisplay = dto.CurrencyCodeSnapshot,
        CurrencyDecimalPlaces = dto.CurrencyDecimalPlacesSnapshot,
        ExchangeRate = dto.ExchangeRate,
        TaxCalculationMode = dto.TaxCalculationMode.ToString(),
        PaymentTermType = dto.PaymentTermType.ToString(),
        PaymentTermDays = dto.PaymentTermDaysSnapshot,
        Subtotal = dto.Subtotal,
        DiscountAmount = dto.DiscountAmount,
        TaxAmount = dto.TaxAmount,
        TotalAmount = dto.TotalAmount,
        Notes = dto.Notes,
        RowVersion = dto.RowVersion,
        Lines = dto.Lines.OrderBy(x => x.LineNumber).Select(ToUi).ToList()
    };

    public static UiSalesInvoiceFormModel ToUi(SalesInvoiceDto dto) => new()
    {
        Id = dto.Id,
        InvoiceCode = dto.InvoiceCode,
        CustomerId = dto.CustomerId.ToString(),
        CustomerCode = dto.CustomerCode ?? string.Empty,
        CustomerDisplay = Display(dto.CustomerCode, dto.CustomerName),
        CustomerOrderId = dto.CustomerOrderId?.ToString() ?? string.Empty,
        CustomerOrderDisplay = dto.CustomerOrderCode,
        PrescriptionRevisionId = dto.PrescriptionRevisionId?.ToString() ?? string.Empty,
        PrescriptionCode = dto.PrescriptionCode ?? string.Empty,
        PrescriptionDisplay = PrescriptionDisplay(dto.PrescriptionCode, dto.PrescriptionRevisionNumber),
        InvoiceDate = dto.InvoiceDate,
        PostingDate = dto.PostingDate,
        DueDate = dto.DueDate,
        Status = dto.Status.ToString(),
        StatusText = SalesArabicPresenter.InvoiceStatusText(dto.Status),
        CurrencyId = dto.CurrencyId.ToString(),
        CurrencyCode = dto.CurrencyCodeSnapshot,
        CurrencyDisplay = dto.CurrencyCodeSnapshot,
        CurrencyDecimalPlaces = dto.CurrencyDecimalPlacesSnapshot,
        ExchangeRate = dto.ExchangeRate,
        TaxCalculationMode = dto.TaxCalculationMode.ToString(),
        PaymentTermType = dto.PaymentTermType.ToString(),
        PaymentTermDays = dto.PaymentTermDaysSnapshot,
        Subtotal = dto.Subtotal,
        DiscountAmount = dto.DiscountAmount,
        TaxAmount = dto.TaxAmount,
        TotalAmount = dto.TotalAmount,
        PaidAmount = dto.PaymentSummary.PaidAmount,
        OutstandingAmount = dto.PaymentSummary.OutstandingAmount,
        JournalEntryId = dto.JournalEntryId,
        JournalEntryNumber = dto.JournalEntryNumber,
        Description = dto.Description,
        RowVersion = dto.RowVersion,
        Lines = dto.Lines.OrderBy(x => x.LineNumber).Select(ToUi).ToList()
    };

    /// <summary>
    /// Applies the selected customer order to a new invoice preview.
    /// Invoice code/date/posting date/description are intentionally preserved because
    /// they belong to the invoice itself, while all source-order business data is copied.
    /// </summary>
    public static void ApplyOrderToInvoice(UiSalesInvoiceFormModel invoice, CustomerOrderDto order)
    {
        invoice.CustomerOrderId = order.Id.ToString();
        invoice.CustomerOrderDisplay = order.OrderCode;

        invoice.CustomerId = order.CustomerId.ToString();
        invoice.CustomerCode = order.CustomerCode ?? string.Empty;
        invoice.CustomerDisplay = Display(order.CustomerCode, order.CustomerName);

        invoice.PrescriptionRevisionId = order.PrescriptionRevisionId?.ToString() ?? string.Empty;
        invoice.PrescriptionCode = order.PrescriptionCode ?? string.Empty;
        invoice.PrescriptionDisplay = PrescriptionDisplay(order.PrescriptionCode, order.PrescriptionRevisionNumber);

        invoice.CurrencyId = order.CurrencyId.ToString();
        invoice.CurrencyCode = order.CurrencyCodeSnapshot;
        invoice.CurrencyDisplay = order.CurrencyCodeSnapshot;
        invoice.CurrencyDecimalPlaces = order.CurrencyDecimalPlacesSnapshot;
        invoice.ExchangeRate = order.ExchangeRate;

        invoice.TaxCalculationMode = order.TaxCalculationMode.ToString();
        invoice.PaymentTermType = order.PaymentTermType.ToString();
        invoice.PaymentTermDays = order.PaymentTermDaysSnapshot;
        invoice.DueDate = invoice.PaymentTermType == SalesPaymentTermType.Credit.ToString() && invoice.InvoiceDate.HasValue
            ? invoice.InvoiceDate.Value.AddDays(order.PaymentTermDaysSnapshot)
            : null;

        invoice.Subtotal = order.Subtotal;
        invoice.DiscountAmount = order.DiscountAmount;
        invoice.TaxAmount = order.TaxAmount;
        invoice.TotalAmount = order.TotalAmount;
        invoice.PaidAmount = 0m;
        invoice.OutstandingAmount = order.TotalAmount;

        invoice.Lines = order.Lines
            .Where(x => x.IsActive)
            .OrderBy(x => x.LineNumber)
            .Select(ToInvoiceDraftLineFromOrder)
            .ToList();
    }

    public static void ClearOrderFromNewInvoice(UiSalesInvoiceFormModel invoice)
    {
        invoice.CustomerOrderId = string.Empty;
        invoice.CustomerOrderDisplay = null;
        invoice.CustomerId = string.Empty;
        invoice.CustomerCode = string.Empty;
        invoice.CustomerDisplay = null;
        invoice.PrescriptionRevisionId = string.Empty;
        invoice.PrescriptionCode = string.Empty;
        invoice.PrescriptionDisplay = null;
        invoice.CurrencyId = string.Empty;
        invoice.CurrencyDisplay = null;
        invoice.CurrencyCode = string.Empty;
        invoice.CurrencyDecimalPlaces = 2;
        invoice.ExchangeRate = 1m;
        invoice.TaxCalculationMode = TaxCalculationMode.Exclusive.ToString();
        invoice.PaymentTermType = SalesPaymentTermType.Immediate.ToString();
        invoice.PaymentTermDays = 0;
        invoice.DueDate = null;
        invoice.Subtotal = 0m;
        invoice.DiscountAmount = 0m;
        invoice.TaxAmount = 0m;
        invoice.TotalAmount = 0m;
        invoice.PaidAmount = 0m;
        invoice.OutstandingAmount = 0m;
        invoice.Lines.Clear();
    }

    private static UiSalesLineModel ToUi(CustomerOrderLineDto x) => new()
    {
        Id = x.Id,
        GroupId = x.GroupId,
        LineNumber = x.LineNumber,
        LineType = x.LineType.ToString(),
        ProductCategoryId = x.ProductCategoryId?.ToString() ?? string.Empty,
        ProductCategoryDisplay = Display(x.ProductCategoryCode, x.ProductCategoryName),
        ProductVariantId = x.ProductVariantId?.ToString() ?? string.Empty,
        ProductDisplay = Display(x.ProductCode, x.ProductName) ?? x.DescriptionSnapshot,
        ProductCodeSnapshot = x.ProductCode,
        ProductNameSnapshot = x.ProductName,
        WarehouseId = x.WarehouseId?.ToString() ?? string.Empty,
        WarehouseDisplay = Display(x.WarehouseCode, x.WarehouseName),
        Description = x.DescriptionSnapshot,
        Quantity = x.Quantity,
        BaseUnitPrice = x.BaseUnitPrice,
        ActualUnitPrice = x.ActualUnitPrice,
        DiscountType = x.DiscountType.ToString(),
        DiscountValue = x.DiscountValue,
        DiscountAmount = x.DiscountAmount,
        TaxRate = x.TaxRate,
        TaxAmount = x.TaxAmount,
        NetAmount = x.NetAmount,
        FinalAmount = x.FinalAmount,
        PrescriptionRevisionId = x.PrescriptionRevisionId?.ToString() ?? string.Empty,
        PrescriptionRevisionDisplay = PrescriptionDisplay(x.PrescriptionCode, x.PrescriptionRevisionNumber),
        PrescriptionEye = x.PrescriptionEye?.ToString() ?? string.Empty,
        PrescriptionRequired = x.PrescriptionRevisionId.HasValue || x.PrescriptionEye.HasValue,
        RequiresProduction = x.RequiresProduction,
        Notes = x.Notes,
        RowVersion = x.RowVersion
    };

    private static UiSalesLineModel ToInvoiceDraftLineFromOrder(CustomerOrderLineDto x)
    {
        var line = ToUi(x);
        line.Id = null;
        line.CustomerOrderLineId = x.Id;
        line.RowVersion = string.Empty;
        return line;
    }

    private static UiSalesLineModel ToUi(SalesInvoiceLineDto x) => new()
    {
        Id = x.Id,
        CustomerOrderLineId = x.CustomerOrderLineId,
        GroupId = x.GroupId,
        LineNumber = x.LineNumber,
        LineType = x.LineType.ToString(),
        ProductCategoryId = x.ProductCategoryId?.ToString() ?? string.Empty,
        ProductCategoryDisplay = Display(x.ProductCategoryCode, x.ProductCategoryName),
        ProductVariantId = x.ProductVariantId?.ToString() ?? string.Empty,
        ProductDisplay = Display(x.ProductCodeSnapshot, x.ProductNameSnapshot) ?? x.ProductNameSnapshot,
        WarehouseId = x.WarehouseId?.ToString() ?? string.Empty,
        WarehouseDisplay = Display(x.WarehouseCode, x.WarehouseName),
        ProductCodeSnapshot = x.ProductCodeSnapshot,
        ProductNameSnapshot = x.ProductNameSnapshot,
        UnitSnapshot = x.UnitSnapshot,
        Description = x.DescriptionSnapshot,
        Quantity = x.Quantity,
        BaseUnitPrice = x.BaseUnitPrice,
        ActualUnitPrice = x.ActualUnitPrice,
        DiscountType = x.DiscountType.ToString(),
        DiscountValue = x.DiscountValue,
        DiscountAmount = x.DiscountAmount,
        TaxRate = x.TaxRate,
        TaxAmount = x.TaxAmount,
        NetAmount = x.NetAmount,
        FinalAmount = x.FinalAmount,
        UnitCostSnapshot = x.UnitCostSnapshot,
        TotalCostSnapshot = x.TotalCostSnapshot,
        PrescriptionRevisionId = x.PrescriptionRevisionId?.ToString() ?? string.Empty,
        PrescriptionRevisionDisplay = PrescriptionDisplay(x.PrescriptionCode, x.PrescriptionRevisionNumber),
        PrescriptionEye = x.PrescriptionEye?.ToString() ?? string.Empty,
        PrescriptionRequired = x.PrescriptionRevisionId.HasValue || x.PrescriptionEye.HasValue,
        RequiresProduction = x.RequiresProduction,
        Notes = x.Notes,
        RowVersion = x.RowVersion
    };

    public static CreatePrescriptionRequest ToCreate(
        UiPrescriptionFormModel m,
        UiPrescriptionRevisionModel initialRevision) =>
        new(
            m.PrescriptionCode,
            Guid.Parse(m.CustomerId),
            m.PrescriptionDate ?? DateOnly.FromDateTime(DateTime.Today),
            m.PrescribedBy,
            m.ClinicName,
            m.Notes,
            new CreatePrescriptionInitialRevisionRequest(
                initialRevision.EffectiveDate ?? DateOnly.FromDateTime(DateTime.Today),
                initialRevision.Reason,
                initialRevision.Eyes.Select(ToEyeRequest).ToArray()));

    public static UpdatePrescriptionRequest ToUpdate(UiPrescriptionFormModel m) =>
        new(m.PrescriptionDate ?? DateOnly.FromDateTime(DateTime.Today), m.PrescribedBy, m.ClinicName, m.Notes, m.RowVersion);

    public static CreatePrescriptionRevisionRequest ToRevisionRequest(
        UiPrescriptionRevisionModel m,
        string prescriptionRowVersion) =>
        new(
            m.EffectiveDate ?? DateOnly.FromDateTime(DateTime.Today),
            m.Reason,
            m.Eyes.Select(ToEyeRequest).ToArray(),
            prescriptionRowVersion);

    private static PrescriptionEyeDetailRequest ToEyeRequest(UiPrescriptionEyeModel x) =>
        new(
            Parse<EyeSide>(x.Eye, EyeSide.RightOD),
            x.SPH,
            x.CYL,
            x.Axis,
            x.ADD,
            x.Prism,
            ParseNullable<PrismBaseDirection>(x.PrismBase),
            x.PD,
            x.MonocularPD,
            x.VA,
            x.FittingHeight,
            x.Notes);

    public static CreateCustomerOrderRequest ToCreate(UiCustomerOrderFormModel m) =>
        new(m.OrderCode, Guid.Parse(m.CustomerId), ParseGuid(m.PrescriptionRevisionId), m.OrderDate ?? DateOnly.FromDateTime(DateTime.Today), m.RequiredDate, Guid.Parse(m.CurrencyId), Parse<TaxCalculationMode>(m.TaxCalculationMode, TaxCalculationMode.Exclusive), Parse<SalesPaymentTermType>(m.PaymentTermType, SalesPaymentTermType.Immediate), m.Notes, m.Lines.Select(ToOrderLineRequest).ToArray());

    public static UpdateCustomerOrderRequest ToUpdate(UiCustomerOrderFormModel m) =>
        new(Guid.Parse(m.CustomerId), ParseGuid(m.PrescriptionRevisionId), m.OrderDate ?? DateOnly.FromDateTime(DateTime.Today), m.RequiredDate, Guid.Parse(m.CurrencyId), Parse<TaxCalculationMode>(m.TaxCalculationMode, TaxCalculationMode.Exclusive), Parse<SalesPaymentTermType>(m.PaymentTermType, SalesPaymentTermType.Immediate), m.Notes, m.Lines.Select(ToOrderLineRequest).ToArray(), m.RowVersion);

    public static CreateSalesInvoiceRequest ToCreate(UiSalesInvoiceFormModel m) =>
        new(m.InvoiceCode, Guid.Parse(m.CustomerId), ParseGuid(m.PrescriptionRevisionId), m.InvoiceDate ?? DateOnly.FromDateTime(DateTime.Today), m.PostingDate ?? DateOnly.FromDateTime(DateTime.Today), Guid.Parse(m.CurrencyId), Parse<TaxCalculationMode>(m.TaxCalculationMode, TaxCalculationMode.Exclusive), Parse<SalesPaymentTermType>(m.PaymentTermType, SalesPaymentTermType.Immediate), m.Description, m.Lines.Select(ToInvoiceLineRequest).ToArray());

    public static UpdateSalesInvoiceRequest ToUpdate(UiSalesInvoiceFormModel m) =>
        new(Guid.Parse(m.CustomerId), ParseGuid(m.PrescriptionRevisionId), m.InvoiceDate ?? DateOnly.FromDateTime(DateTime.Today), m.PostingDate ?? DateOnly.FromDateTime(DateTime.Today), Guid.Parse(m.CurrencyId), Parse<TaxCalculationMode>(m.TaxCalculationMode, TaxCalculationMode.Exclusive), Parse<SalesPaymentTermType>(m.PaymentTermType, SalesPaymentTermType.Immediate), m.Description, m.Lines.Select(ToInvoiceLineRequest).ToArray(), m.RowVersion);

    private static CustomerOrderLineRequest ToOrderLineRequest(UiSalesLineModel x) =>
        new(x.Id, x.GroupId, Parse<SalesLineType>(x.LineType, SalesLineType.Frame), ParseGuid(x.ProductVariantId), ParseGuid(x.WarehouseId), x.Description, x.Quantity, x.ActualUnitPrice, Parse<SalesDiscountType>(x.DiscountType, SalesDiscountType.None), x.DiscountValue, x.TaxRate, ParseGuid(x.PrescriptionRevisionId), ParseNullable<EyeSide>(x.PrescriptionEye), x.RequiresProduction, x.Notes, string.IsNullOrWhiteSpace(x.RowVersion) ? null : x.RowVersion);

    private static SalesInvoiceLineRequest ToInvoiceLineRequest(UiSalesLineModel x) =>
        new(x.Id, x.CustomerOrderLineId, x.GroupId, Parse<SalesLineType>(x.LineType, SalesLineType.Frame), ParseGuid(x.ProductVariantId), ParseGuid(x.WarehouseId), x.Description, x.Quantity, x.ActualUnitPrice, Parse<SalesDiscountType>(x.DiscountType, SalesDiscountType.None), x.DiscountValue, x.TaxRate, ParseGuid(x.PrescriptionRevisionId), ParseNullable<EyeSide>(x.PrescriptionEye), x.RequiresProduction, x.Notes, string.IsNullOrWhiteSpace(x.RowVersion) ? null : x.RowVersion);

    private static string? Display(string? code, string? name)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.IsNullOrWhiteSpace(name) ? null : name;
        if (string.IsNullOrWhiteSpace(name))
            return code;
        return $"{code} - {name}";
    }

    private static string? PrescriptionDisplay(string? code, int? revisionNumber)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;
        return revisionNumber.HasValue
            ? $"{code} / إصدار {revisionNumber.Value}"
            : code;
    }

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;

    private static T Parse<T>(string? value, T fallback) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) ? parsed : fallback;

    private static T? ParseNullable<T>(string? value) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) ? parsed : null;
}
