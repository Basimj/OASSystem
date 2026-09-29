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
        CustomerDisplay = string.Join(" - ", new[] { dto.CustomerCode, dto.CustomerName }.Where(x => !string.IsNullOrWhiteSpace(x))),
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
            Eye = x.Eye.ToString(), EyeText = SalesArabicPresenter.EyeText(x.Eye), SPH = x.SPH, CYL = x.CYL, Axis = x.Axis,
            ADD = x.ADD, Prism = x.Prism, PrismBase = (x.PrismBase ?? PrismBaseDirection.None).ToString(), PD = x.PD,
            MonocularPD = x.MonocularPD, VA = x.VA, FittingHeight = x.FittingHeight, Notes = x.Notes
        }).ToList()
    };

    public static UiCustomerOrderFormModel ToUi(CustomerOrderDto dto) => new()
    {
        Id = dto.Id, OrderCode = dto.OrderCode, CustomerId = dto.CustomerId.ToString(),
        CustomerDisplay = string.Join(" - ", new[] { dto.CustomerCode, dto.CustomerName }.Where(x => !string.IsNullOrWhiteSpace(x))),
        PrescriptionRevisionId = dto.PrescriptionRevisionId?.ToString() ?? string.Empty,
        OrderDate = dto.OrderDate, RequiredDate = dto.RequiredDate, Status = dto.Status.ToString(), StatusText = SalesArabicPresenter.OrderStatusText(dto.Status),
        CurrencyId = dto.CurrencyId.ToString(), CurrencyCode = dto.CurrencyCodeSnapshot, CurrencyDisplay = dto.CurrencyCodeSnapshot,
        CurrencyDecimalPlaces = dto.CurrencyDecimalPlacesSnapshot, ExchangeRate = dto.ExchangeRate,
        TaxCalculationMode = dto.TaxCalculationMode.ToString(), PaymentTermType = dto.PaymentTermType.ToString(), PaymentTermDays = dto.PaymentTermDaysSnapshot,
        Subtotal = dto.Subtotal, DiscountAmount = dto.DiscountAmount, TaxAmount = dto.TaxAmount, TotalAmount = dto.TotalAmount,
        Notes = dto.Notes, RowVersion = dto.RowVersion, Lines = dto.Lines.OrderBy(x => x.LineNumber).Select(ToUi).ToList()
    };

    public static UiSalesInvoiceFormModel ToUi(SalesInvoiceDto dto) => new()
    {
        Id = dto.Id, InvoiceCode = dto.InvoiceCode, CustomerId = dto.CustomerId.ToString(),
        CustomerDisplay = string.Join(" - ", new[] { dto.CustomerCode, dto.CustomerName }.Where(x => !string.IsNullOrWhiteSpace(x))),
        CustomerOrderId = dto.CustomerOrderId?.ToString() ?? string.Empty, CustomerOrderDisplay = dto.CustomerOrderCode,
        PrescriptionRevisionId = dto.PrescriptionRevisionId?.ToString() ?? string.Empty,
        InvoiceDate = dto.InvoiceDate, PostingDate = dto.PostingDate, DueDate = dto.DueDate,
        Status = dto.Status.ToString(), StatusText = SalesArabicPresenter.InvoiceStatusText(dto.Status), CurrencyId = dto.CurrencyId.ToString(),
        CurrencyCode = dto.CurrencyCodeSnapshot, CurrencyDisplay = dto.CurrencyCodeSnapshot, CurrencyDecimalPlaces = dto.CurrencyDecimalPlacesSnapshot,
        ExchangeRate = dto.ExchangeRate, TaxCalculationMode = dto.TaxCalculationMode.ToString(), PaymentTermType = dto.PaymentTermType.ToString(),
        PaymentTermDays = dto.PaymentTermDaysSnapshot, Subtotal = dto.Subtotal, DiscountAmount = dto.DiscountAmount, TaxAmount = dto.TaxAmount,
        TotalAmount = dto.TotalAmount, PaidAmount = dto.PaymentSummary.PaidAmount, OutstandingAmount = dto.PaymentSummary.OutstandingAmount,
        JournalEntryId = dto.JournalEntryId, Description = dto.Description, RowVersion = dto.RowVersion,
        Lines = dto.Lines.OrderBy(x => x.LineNumber).Select(ToUi).ToList()
    };

    private static UiSalesLineModel ToUi(CustomerOrderLineDto x) => new()
    {
        Id = x.Id, GroupId = x.GroupId, LineNumber = x.LineNumber, LineType = x.LineType.ToString(), ProductVariantId = x.ProductVariantId?.ToString() ?? string.Empty,
        WarehouseId = x.WarehouseId?.ToString() ?? string.Empty, Description = x.DescriptionSnapshot, Quantity = x.Quantity, BaseUnitPrice = x.BaseUnitPrice,
        ActualUnitPrice = x.ActualUnitPrice, DiscountType = x.DiscountType.ToString(), DiscountValue = x.DiscountValue, DiscountAmount = x.DiscountAmount,
        TaxRate = x.TaxRate, TaxAmount = x.TaxAmount, NetAmount = x.NetAmount, FinalAmount = x.FinalAmount,
        PrescriptionRevisionId = x.PrescriptionRevisionId?.ToString() ?? string.Empty, PrescriptionEye = x.PrescriptionEye?.ToString() ?? string.Empty,
        RequiresProduction = x.RequiresProduction, Notes = x.Notes, RowVersion = x.RowVersion
    };

    private static UiSalesLineModel ToUi(SalesInvoiceLineDto x) => new()
    {
        Id = x.Id, CustomerOrderLineId = x.CustomerOrderLineId, GroupId = x.GroupId, LineNumber = x.LineNumber, LineType = x.LineType.ToString(),
        ProductVariantId = x.ProductVariantId?.ToString() ?? string.Empty, ProductDisplay = x.ProductNameSnapshot, WarehouseId = x.WarehouseId?.ToString() ?? string.Empty,
        ProductCodeSnapshot = x.ProductCodeSnapshot, ProductNameSnapshot = x.ProductNameSnapshot, UnitSnapshot = x.UnitSnapshot,
        Description = x.DescriptionSnapshot, Quantity = x.Quantity, BaseUnitPrice = x.BaseUnitPrice, ActualUnitPrice = x.ActualUnitPrice,
        DiscountType = x.DiscountType.ToString(), DiscountValue = x.DiscountValue, DiscountAmount = x.DiscountAmount, TaxRate = x.TaxRate, TaxAmount = x.TaxAmount,
        NetAmount = x.NetAmount, FinalAmount = x.FinalAmount, UnitCostSnapshot = x.UnitCostSnapshot, TotalCostSnapshot = x.TotalCostSnapshot,
        PrescriptionRevisionId = x.PrescriptionRevisionId?.ToString() ?? string.Empty, PrescriptionEye = x.PrescriptionEye?.ToString() ?? string.Empty,
        RequiresProduction = x.RequiresProduction, Notes = x.Notes, RowVersion = x.RowVersion
    };

    public static CreatePrescriptionRequest ToCreate(UiPrescriptionFormModel m) => new(m.PrescriptionCode, Guid.Parse(m.CustomerId), m.PrescriptionDate ?? DateOnly.FromDateTime(DateTime.Today), m.PrescribedBy, m.ClinicName, m.Notes);
    public static UpdatePrescriptionRequest ToUpdate(UiPrescriptionFormModel m) => new(m.PrescriptionDate ?? DateOnly.FromDateTime(DateTime.Today), m.PrescribedBy, m.ClinicName, m.Notes, m.RowVersion);
    public static CreatePrescriptionRevisionRequest ToRevisionRequest(UiPrescriptionRevisionModel m, string prescriptionRowVersion) => new(
        m.EffectiveDate ?? DateOnly.FromDateTime(DateTime.Today), m.Reason,
        m.Eyes.Select(x => new PrescriptionEyeDetailRequest(Parse<EyeSide>(x.Eye, EyeSide.RightOD), x.SPH, x.CYL, x.Axis, x.ADD, x.Prism, ParseNullable<PrismBaseDirection>(x.PrismBase), x.PD, x.MonocularPD, x.VA, x.FittingHeight, x.Notes)).ToArray(), prescriptionRowVersion);

    public static CreateCustomerOrderRequest ToCreate(UiCustomerOrderFormModel m) => new(m.OrderCode, Guid.Parse(m.CustomerId), ParseGuid(m.PrescriptionRevisionId), m.OrderDate ?? DateOnly.FromDateTime(DateTime.Today), m.RequiredDate, Guid.Parse(m.CurrencyId), Parse<TaxCalculationMode>(m.TaxCalculationMode, TaxCalculationMode.Exclusive), Parse<SalesPaymentTermType>(m.PaymentTermType, SalesPaymentTermType.Immediate), m.Notes, m.Lines.Select(ToOrderLineRequest).ToArray());
    public static UpdateCustomerOrderRequest ToUpdate(UiCustomerOrderFormModel m) => new(Guid.Parse(m.CustomerId), ParseGuid(m.PrescriptionRevisionId), m.OrderDate ?? DateOnly.FromDateTime(DateTime.Today), m.RequiredDate, Guid.Parse(m.CurrencyId), Parse<TaxCalculationMode>(m.TaxCalculationMode, TaxCalculationMode.Exclusive), Parse<SalesPaymentTermType>(m.PaymentTermType, SalesPaymentTermType.Immediate), m.Notes, m.Lines.Select(ToOrderLineRequest).ToArray(), m.RowVersion);

    public static CreateSalesInvoiceRequest ToCreate(UiSalesInvoiceFormModel m) => new(m.InvoiceCode, Guid.Parse(m.CustomerId), ParseGuid(m.PrescriptionRevisionId), m.InvoiceDate ?? DateOnly.FromDateTime(DateTime.Today), m.PostingDate ?? DateOnly.FromDateTime(DateTime.Today), Guid.Parse(m.CurrencyId), Parse<TaxCalculationMode>(m.TaxCalculationMode, TaxCalculationMode.Exclusive), Parse<SalesPaymentTermType>(m.PaymentTermType, SalesPaymentTermType.Immediate), m.Description, m.Lines.Select(ToInvoiceLineRequest).ToArray());
    public static UpdateSalesInvoiceRequest ToUpdate(UiSalesInvoiceFormModel m) => new(Guid.Parse(m.CustomerId), ParseGuid(m.PrescriptionRevisionId), m.InvoiceDate ?? DateOnly.FromDateTime(DateTime.Today), m.PostingDate ?? DateOnly.FromDateTime(DateTime.Today), Guid.Parse(m.CurrencyId), Parse<TaxCalculationMode>(m.TaxCalculationMode, TaxCalculationMode.Exclusive), Parse<SalesPaymentTermType>(m.PaymentTermType, SalesPaymentTermType.Immediate), m.Description, m.Lines.Select(ToInvoiceLineRequest).ToArray(), m.RowVersion);

    private static CustomerOrderLineRequest ToOrderLineRequest(UiSalesLineModel x) => new(x.Id, x.GroupId, Parse<SalesLineType>(x.LineType, SalesLineType.Frame), ParseGuid(x.ProductVariantId), ParseGuid(x.WarehouseId), x.Description, x.Quantity, x.ActualUnitPrice, Parse<SalesDiscountType>(x.DiscountType, SalesDiscountType.None), x.DiscountValue, x.TaxRate, ParseGuid(x.PrescriptionRevisionId), ParseNullable<EyeSide>(x.PrescriptionEye), x.RequiresProduction, x.Notes, string.IsNullOrWhiteSpace(x.RowVersion) ? null : x.RowVersion);
    private static SalesInvoiceLineRequest ToInvoiceLineRequest(UiSalesLineModel x) => new(x.Id, x.CustomerOrderLineId, x.GroupId, Parse<SalesLineType>(x.LineType, SalesLineType.Frame), ParseGuid(x.ProductVariantId), ParseGuid(x.WarehouseId), x.Description, x.Quantity, x.ActualUnitPrice, Parse<SalesDiscountType>(x.DiscountType, SalesDiscountType.None), x.DiscountValue, x.TaxRate, ParseGuid(x.PrescriptionRevisionId), ParseNullable<EyeSide>(x.PrescriptionEye), x.RequiresProduction, x.Notes, string.IsNullOrWhiteSpace(x.RowVersion) ? null : x.RowVersion);

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
    private static T Parse<T>(string? value, T fallback) where T : struct, Enum => Enum.TryParse<T>(value, true, out var parsed) ? parsed : fallback;
    private static T? ParseNullable<T>(string? value) where T : struct, Enum => Enum.TryParse<T>(value, true, out var parsed) ? parsed : null;
}
