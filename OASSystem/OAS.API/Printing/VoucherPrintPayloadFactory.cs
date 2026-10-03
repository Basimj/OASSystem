using MediatR;
using OAS.Application.Accounting.BankAccounts.Queries.GetBankAccountById;
using OAS.Application.Accounting.CashAccounts.Queries.GetCashAccountById;
using OAS.Application.Accounting.Currencies.Queries.GetCurrencyById;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;
using OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.SalesInvoices.Queries;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Accounting.Currencies;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Printing;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.SalesInvoices;

namespace OAS.API.Printing;

public sealed class VoucherPrintPayloadFactory(
    ISender sender,
    IConfiguration configuration)
{
    public async Task<object> BuildAsync(
        string documentType,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return documentType switch
        {
            PrintDocumentTypes.ReceiptVoucher =>
                await BuildReceiptAsync(
                    await sender.Send(new GetReceiptVoucherByIdQuery(documentId), cancellationToken),
                    cancellationToken),

            PrintDocumentTypes.PaymentVoucher =>
                await BuildPaymentAsync(
                    await sender.Send(new GetPaymentVoucherByIdQuery(documentId), cancellationToken),
                    cancellationToken),

            PrintDocumentTypes.SalesInvoice =>
                await BuildSalesInvoiceAsync(
                    await sender.Send(new GetSalesInvoiceByIdQuery(documentId), cancellationToken),
                    cancellationToken),

            _ => throw new ArgumentOutOfRangeException(
                nameof(documentType),
                documentType,
                "نوع المستند غير مدعوم للطباعة.")
        };
    }

    private async Task<object> BuildReceiptAsync(
        ReceiptVoucherDto voucher,
        CancellationToken cancellationToken)
    {
        var total = ResolveReceiptTotal(voucher);
        var currency = await TryGetCurrencyAsync(voucher.BaseCurrencyId, cancellationToken);
        var cashAccounts = await ResolveCashAccountsAsync(voucher.Lines.Select(x => x.CashAccountId), cancellationToken);
        var bankAccounts = await ResolveBankAccountsAsync(voucher.Lines.Select(x => x.BankAccountId), cancellationToken);

        var partyNames = voucher.Lines
            .Select(x => x.PartyNameSnapshot)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var customerName = voucher.Lines
            .Where(x => x.PartyType == SettlementPartyType.Customer)
            .Select(x => x.PartyNameSnapshot)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var lines = voucher.Lines
            .OrderBy(x => x.LineNumber)
            .Select(x => MapReceiptLine(x, cashAccounts, bankAccounts))
            .ToArray();

        return new
        {
            DocumentTitleAr = "سند قبض",
            DocumentTitleEn = "RECEIPT VOUCHER",
            voucher.VoucherNumber,
            VoucherDate = voucher.VoucherDate.ToString("dd/MM/yyyy"),
            CustomerName = customerName ?? string.Empty,
            ReceivedFrom = partyNames.Length > 0 ? string.Join("، ", partyNames) : string.Empty,
            PaymentMethod = DescribePaymentMethods(voucher.Lines.Select(x => x.PaymentMethod)),
            CashAccountName = JoinDistinct(voucher.Lines.Select(x => ResolveCashName(x.CashAccountId, cashAccounts))),
            BankAccountName = JoinDistinct(voucher.Lines.Select(x => ResolveBankName(x.BankAccountId, bankAccounts))),
            BaseCurrencyCode = voucher.BaseCurrencyCodeSnapshot ?? currency?.Code ?? string.Empty,
            BaseCurrencyName = currency?.NameAr ?? string.Empty,
            BaseCurrencySymbol = currency?.Symbol ?? string.Empty,
            BaseCurrencyDecimalPlaces = voucher.BaseCurrencyDecimalPlacesSnapshot ?? currency?.DecimalPlaces ?? 2,
            TotalAmount = total,
            TotalAmountWords = ArabicAmountTextFormatter.Format(total, currency?.NameAr),
            Description = voucher.Description ?? string.Empty,
            Status = voucher.Status.ToString(),
            StatusText = DescribeReceiptStatus(voucher.Status),
            CreatedBy = voucher.CreatedBy ?? string.Empty,
            CreatedAt = voucher.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            PrintedAt = DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm"),
            Company = Company(),
            Lines = lines,
            CurrencySummary = BuildReceiptCurrencySummary(voucher.Lines),
            Signatures = new
            {
                Accountant = "المحاسب",
                Receiver = "المستلم",
                Approval = "الاعتماد"
            }
        };
    }

    private async Task<object> BuildPaymentAsync(
        PaymentVoucherDto voucher,
        CancellationToken cancellationToken)
    {
        var total = ResolvePaymentTotal(voucher);
        var currency = await TryGetCurrencyAsync(voucher.BaseCurrencyId, cancellationToken);
        var cashAccounts = await ResolveCashAccountsAsync(voucher.Lines.Select(x => x.CashAccountId), cancellationToken);
        var bankAccounts = await ResolveBankAccountsAsync(voucher.Lines.Select(x => x.BankAccountId), cancellationToken);

        var partyNames = voucher.Lines
            .Select(x => x.PartyNameSnapshot)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var supplierName = voucher.Lines
            .Where(x => x.PartyType == SettlementPartyType.Supplier)
            .Select(x => x.PartyNameSnapshot)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var lines = voucher.Lines
            .OrderBy(x => x.LineNumber)
            .Select(x => MapPaymentLine(x, cashAccounts, bankAccounts))
            .ToArray();

        return new
        {
            DocumentTitleAr = "سند صرف",
            DocumentTitleEn = "PAYMENT VOUCHER",
            voucher.VoucherNumber,
            VoucherDate = voucher.VoucherDate.ToString("dd/MM/yyyy"),
            SupplierName = supplierName ?? string.Empty,
            BeneficiaryName = partyNames.Length > 0 ? string.Join("، ", partyNames) : string.Empty,
            PaymentMethod = DescribePaymentMethods(voucher.Lines.Select(x => x.PaymentMethod)),
            CashAccountName = JoinDistinct(voucher.Lines.Select(x => ResolveCashName(x.CashAccountId, cashAccounts))),
            BankAccountName = JoinDistinct(voucher.Lines.Select(x => ResolveBankName(x.BankAccountId, bankAccounts))),
            BaseCurrencyCode = voucher.BaseCurrencyCodeSnapshot ?? currency?.Code ?? string.Empty,
            BaseCurrencyName = currency?.NameAr ?? string.Empty,
            BaseCurrencySymbol = currency?.Symbol ?? string.Empty,
            BaseCurrencyDecimalPlaces = voucher.BaseCurrencyDecimalPlacesSnapshot ?? currency?.DecimalPlaces ?? 2,
            TotalAmount = total,
            TotalAmountWords = ArabicAmountTextFormatter.Format(total, currency?.NameAr),
            Description = voucher.Description ?? string.Empty,
            Status = voucher.Status.ToString(),
            StatusText = DescribePaymentStatus(voucher.Status),
            CreatedBy = voucher.CreatedBy ?? string.Empty,
            CreatedAt = voucher.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            PrintedAt = DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm"),
            Company = Company(),
            Lines = lines,
            CurrencySummary = BuildPaymentCurrencySummary(voucher.Lines),
            Signatures = new
            {
                Accountant = "المحاسب",
                Beneficiary = "المستفيد",
                Approval = "الاعتماد"
            }
        };
    }

    private async Task<object> BuildSalesInvoiceAsync(
        SalesInvoiceDto invoice,
        CancellationToken cancellationToken)
    {
        if (invoice.Status != SalesInvoiceStatus.Posted)
            throw new ConflictException(
                "SALES_INVOICE_NOT_POSTED",
                "يمكن طباعة فاتورة المبيعات بعد الترحيل فقط.");

        var currency = await TryGetCurrencyAsync(invoice.CurrencyId, cancellationToken);
        var saleTypeText = invoice.PaymentTermType == SalesPaymentTermType.Immediate ? "فوري" : "آجل";
        var lines = invoice.Lines
            .OrderBy(x => x.LineNumber)
            .Select(MapSalesInvoiceLine)
            .ToArray();

        return new
        {
            DocumentTitleAr = "فاتورة مبيعات",
            DocumentTitleEn = "SALES INVOICE",
            InvoiceNumber = invoice.InvoiceCode,
            InvoiceDate = invoice.InvoiceDate.ToString("dd/MM/yyyy"),
            PostingDate = invoice.PostingDate.ToString("dd/MM/yyyy"),
            CustomerCode = invoice.CustomerCode ?? string.Empty,
            CustomerName = invoice.CustomerName ?? string.Empty,
            CustomerOrderNumber = invoice.CustomerOrderCode ?? string.Empty,
            PrescriptionNumber = BuildPrescriptionNumber(invoice.PrescriptionCode, invoice.PrescriptionRevisionNumber),
            JournalEntryNumber = invoice.JournalEntryNumber ?? string.Empty,
            Status = invoice.Status.ToString(),
            StatusText = DescribeSalesInvoiceStatus(invoice.Status),
            SaleType = saleTypeText,
            PaymentTermDays = invoice.PaymentTermDaysSnapshot,
            DueDate = invoice.DueDate?.ToString("dd/MM/yyyy") ?? string.Empty,
            CurrencyCode = invoice.CurrencyCodeSnapshot,
            CurrencyName = currency?.NameAr ?? string.Empty,
            CurrencySymbol = invoice.CurrencySymbolSnapshot ?? currency?.Symbol ?? string.Empty,
            CurrencyDecimalPlaces = invoice.CurrencyDecimalPlacesSnapshot,
            invoice.ExchangeRate,
            ExchangeRateDate = invoice.ExchangeRateDate.ToString("dd/MM/yyyy"),
            TaxCalculationMode = DescribeTaxCalculationMode(invoice.TaxCalculationMode),
            invoice.Subtotal,
            invoice.DiscountAmount,
            TotalAfterDiscount = invoice.Subtotal - invoice.DiscountAmount,
            invoice.TaxAmount,
            invoice.TotalAmount,
            PaidAmount = invoice.PaymentSummary.PaidAmount,
            OutstandingAmount = invoice.PaymentSummary.OutstandingAmount,
            TotalAmountWords = ArabicAmountTextFormatter.Format(invoice.TotalAmount, currency?.NameAr),
            Description = invoice.Description ?? string.Empty,
            ConfirmedAt = invoice.ConfirmedAtUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? string.Empty,
            ConfirmedBy = invoice.ConfirmedBy ?? string.Empty,
            PostedAt = invoice.PostedAtUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? string.Empty,
            PostedBy = invoice.PostedBy ?? string.Empty,
            CreatedBy = invoice.CreatedBy ?? string.Empty,
            CreatedAt = invoice.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            PrintedAt = DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm"),
            Company = Company(),
            Lines = lines,
            Signatures = new
            {
                Cashier = "أمين الصندوق",
                Accountant = "المحاسب",
                SalesManager = "مدير المبيعات",
                Customer = "استلام العميل"
            }
        };
    }

    private static object MapSalesInvoiceLine(SalesInvoiceLineDto line) => new
    {
        line.LineNumber,
        ProductCode = line.ProductCodeSnapshot ?? string.Empty,
        ProductName = line.ProductNameSnapshot,
        Description = BuildSalesLineDescription(line),
        LineType = DescribeSalesLineType(line.LineType),
        Unit = line.UnitSnapshot ?? string.Empty,
        Warehouse = string.IsNullOrWhiteSpace(line.WarehouseName)
            ? line.WarehouseCode ?? string.Empty
            : string.IsNullOrWhiteSpace(line.WarehouseCode)
                ? line.WarehouseName
                : $"{line.WarehouseCode} - {line.WarehouseName}",
        line.Quantity,
        UnitPrice = line.ActualUnitPrice,
        Discount = line.DiscountAmount,
        TaxRate = line.TaxRate ?? 0m,
        Tax = line.TaxAmount,
        Net = line.NetAmount,
        Total = line.FinalAmount,
        Prescription = BuildPrescriptionNumber(line.PrescriptionCode, line.PrescriptionRevisionNumber),
        Eye = DescribeEyeSide(line.PrescriptionEye),
        Notes = line.Notes ?? string.Empty
    };

    private static string BuildSalesLineDescription(SalesInvoiceLineDto line)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(line.DescriptionSnapshot))
            parts.Add(line.DescriptionSnapshot.Trim());

        var prescription = BuildPrescriptionNumber(line.PrescriptionCode, line.PrescriptionRevisionNumber);
        if (!string.IsNullOrWhiteSpace(prescription))
        {
            var eye = DescribeEyeSide(line.PrescriptionEye);
            parts.Add(string.IsNullOrWhiteSpace(eye) ? $"وصفة: {prescription}" : $"وصفة: {prescription} - {eye}");
        }

        if (line.PrescriptionSnapshot is not null)
        {
            var rx = line.PrescriptionSnapshot;
            var measurements = new List<string>();
            if (rx.SPH.HasValue) measurements.Add($"SPH {rx.SPH.Value:0.##}");
            if (rx.CYL.HasValue) measurements.Add($"CYL {rx.CYL.Value:0.##}");
            if (rx.Axis.HasValue) measurements.Add($"AX {rx.Axis.Value}");
            if (rx.ADD.HasValue) measurements.Add($"ADD {rx.ADD.Value:0.##}");
            if (rx.PD.HasValue) measurements.Add($"PD {rx.PD.Value:0.##}");
            if (rx.FittingHeight.HasValue) measurements.Add($"H {rx.FittingHeight.Value:0.##}");
            if (measurements.Count > 0) parts.Add(string.Join(" / ", measurements));
        }

        if (!string.IsNullOrWhiteSpace(line.Notes))
            parts.Add(line.Notes.Trim());

        return string.Join(" • ", parts);
    }

    private static string BuildPrescriptionNumber(string? code, int? revisionNumber)
    {
        if (string.IsNullOrWhiteSpace(code)) return string.Empty;
        return revisionNumber.HasValue ? $"{code} / إصدار {revisionNumber.Value}" : code;
    }

    private static string DescribeSalesInvoiceStatus(SalesInvoiceStatus status) => status switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.Confirmed => "مؤكدة",
        SalesInvoiceStatus.Posted => "مرحّلة",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };

    private static string DescribeTaxCalculationMode(TaxCalculationMode mode) => mode switch
    {
        TaxCalculationMode.Exclusive => "الضريبة غير شاملة",
        TaxCalculationMode.Inclusive => "الضريبة شاملة",
        _ => mode.ToString()
    };

    private static string DescribeSalesLineType(SalesLineType value) => value switch
    {
        SalesLineType.Frame => "إطار",
        SalesLineType.Lens => "عدسة",
        SalesLineType.Accessory => "ملحق",
        SalesLineType.Service => "خدمة",
        SalesLineType.Other => "أخرى",
        _ => value.ToString()
    };

    private static string DescribeEyeSide(EyeSide? value) => value switch
    {
        EyeSide.RightOD => "يمين (OD)",
        EyeSide.LeftOS => "يسار (OS)",
        _ => string.Empty
    };

    private static object MapReceiptLine(
        ReceiptVoucherLineDto line,
        IReadOnlyDictionary<Guid, CashAccountDto> cashAccounts,
        IReadOnlyDictionary<Guid, BankAccountDto> bankAccounts) => new
        {
            line.LineNumber,
            PartyType = DescribePartyType(line.PartyType),
            PartyName = line.PartyNameSnapshot ?? string.Empty,
            AccountName = ResolveLineName(line.PartyNameSnapshot, line.ReferenceType, line.AccountId),
            PaymentMethod = DescribePaymentMethod(line.PaymentMethod),
            SettlementAccountName = ResolveSettlementAccountName(line.CashAccountId, line.BankAccountId, cashAccounts, bankAccounts),
            CurrencyCode = line.CurrencyCodeSnapshot ?? string.Empty,
            CurrencySymbol = line.CurrencySymbolSnapshot ?? string.Empty,
            line.Amount,
            ExchangeRate = line.ExchangeRate ?? 1m,
            BaseAmount = line.BaseAmount ?? line.Amount,
            ReferenceNumber = line.ReferenceNumber ?? string.Empty,
            ReferenceDate = line.ReferenceDate?.ToString("dd/MM/yyyy") ?? string.Empty,
            ReferenceType = line.ReferenceType ?? string.Empty,
            Description = line.Description ?? string.Empty
        };

    private static object MapPaymentLine(
        PaymentVoucherLineDto line,
        IReadOnlyDictionary<Guid, CashAccountDto> cashAccounts,
        IReadOnlyDictionary<Guid, BankAccountDto> bankAccounts) => new
        {
            line.LineNumber,
            PartyType = DescribePartyType(line.PartyType),
            PartyName = line.PartyNameSnapshot ?? string.Empty,
            AccountName = ResolveLineName(line.PartyNameSnapshot, line.ReferenceType, line.AccountId),
            PaymentMethod = DescribePaymentMethod(line.PaymentMethod),
            SettlementAccountName = ResolveSettlementAccountName(line.CashAccountId, line.BankAccountId, cashAccounts, bankAccounts),
            CurrencyCode = line.CurrencyCodeSnapshot ?? string.Empty,
            CurrencySymbol = line.CurrencySymbolSnapshot ?? string.Empty,
            line.Amount,
            ExchangeRate = line.ExchangeRate ?? 1m,
            BaseAmount = line.BaseAmount ?? line.Amount,
            ReferenceNumber = line.ReferenceNumber ?? string.Empty,
            ReferenceDate = line.ReferenceDate?.ToString("dd/MM/yyyy") ?? string.Empty,
            ReferenceType = line.ReferenceType ?? string.Empty,
            Description = line.Description ?? string.Empty
        };

    private object Company() => new
    {
        Name = configuration["Application:Name"] ?? "OAS SYSTEM",
        Address = configuration["Application:Address"] ?? string.Empty,
        Phone = configuration["Application:Phone"] ?? string.Empty,
        CommercialRegistration = configuration["Application:CommercialRegistration"] ?? string.Empty,
        TaxNumber = configuration["Application:TaxNumber"] ?? string.Empty
    };

    private async Task<CurrencyDto?> TryGetCurrencyAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return null;
        try { return await sender.Send(new GetCurrencyByIdQuery(id.Value), cancellationToken); }
        catch { return null; }
    }

    private async Task<IReadOnlyDictionary<Guid, CashAccountDto>> ResolveCashAccountsAsync(
        IEnumerable<Guid?> ids,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, CashAccountDto>();
        foreach (var id in ids.Where(x => x.HasValue).Select(x => x!.Value).Distinct())
        {
            try
            {
                var item = await sender.Send(new GetCashAccountByIdQuery(id), cancellationToken);
                if (item is not null) result[id] = item;
            }
            catch { }
        }
        return result;
    }

    private async Task<IReadOnlyDictionary<Guid, BankAccountDto>> ResolveBankAccountsAsync(
        IEnumerable<Guid?> ids,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, BankAccountDto>();
        foreach (var id in ids.Where(x => x.HasValue).Select(x => x!.Value).Distinct())
        {
            try
            {
                var item = await sender.Send(new GetBankAccountByIdQuery(id), cancellationToken);
                if (item is not null) result[id] = item;
            }
            catch { }
        }
        return result;
    }

    private static object[] BuildReceiptCurrencySummary(IEnumerable<ReceiptVoucherLineDto> lines) =>
        lines.GroupBy(x => x.CurrencyCodeSnapshot ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(g => (object)new
            {
                CurrencyCode = string.IsNullOrWhiteSpace(g.Key) ? "-" : g.Key,
                Amount = g.Sum(x => x.Amount),
                BaseAmount = g.Sum(x => x.BaseAmount ?? x.Amount)
            })
            .ToArray();

    private static object[] BuildPaymentCurrencySummary(IEnumerable<PaymentVoucherLineDto> lines) =>
        lines.GroupBy(x => x.CurrencyCodeSnapshot ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(g => (object)new
            {
                CurrencyCode = string.IsNullOrWhiteSpace(g.Key) ? "-" : g.Key,
                Amount = g.Sum(x => x.Amount),
                BaseAmount = g.Sum(x => x.BaseAmount ?? x.Amount)
            })
            .ToArray();

    private static decimal ResolveReceiptTotal(ReceiptVoucherDto voucher) =>
        voucher.BaseTotalAmount ?? voucher.Lines.Sum(x => x.BaseAmount ?? x.Amount);

    private static decimal ResolvePaymentTotal(PaymentVoucherDto voucher) =>
        voucher.BaseTotalAmount ?? voucher.Lines.Sum(x => x.BaseAmount ?? x.Amount);

    private static string ResolveLineName(string? partyName, string? referenceType, Guid accountId)
    {
        if (!string.IsNullOrWhiteSpace(partyName)) return partyName.Trim();
        if (!string.IsNullOrWhiteSpace(referenceType)) return referenceType.Trim();
        return $"حساب {accountId.ToString("N")[..8]}";
    }

    private static string ResolveSettlementAccountName(
        Guid? cashAccountId,
        Guid? bankAccountId,
        IReadOnlyDictionary<Guid, CashAccountDto> cashAccounts,
        IReadOnlyDictionary<Guid, BankAccountDto> bankAccounts)
    {
        if (cashAccountId is Guid cashId)
            return cashAccounts.TryGetValue(cashId, out var cash) ? cash.Name : "صندوق";

        if (bankAccountId is Guid bankId)
        {
            if (!bankAccounts.TryGetValue(bankId, out var bank)) return "حساب بنكي";
            return string.IsNullOrWhiteSpace(bank.AccountName)
                ? bank.BankName
                : $"{bank.BankName} - {bank.AccountName}";
        }

        return string.Empty;
    }

    private static string ResolveCashName(Guid? id, IReadOnlyDictionary<Guid, CashAccountDto> items) =>
        id is Guid value && items.TryGetValue(value, out var item) ? item.Name : string.Empty;

    private static string ResolveBankName(Guid? id, IReadOnlyDictionary<Guid, BankAccountDto> items) =>
        id is Guid value && items.TryGetValue(value, out var item)
            ? (string.IsNullOrWhiteSpace(item.AccountName) ? item.BankName : $"{item.BankName} - {item.AccountName}")
            : string.Empty;

    private static string JoinDistinct(IEnumerable<string> values) =>
        string.Join("، ", values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));

    private static string DescribePaymentMethods(IEnumerable<PaymentMethod?> methods)
    {
        var values = methods
            .Where(x => x.HasValue)
            .Select(DescribePaymentMethod)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return values.Length switch
        {
            0 => string.Empty,
            1 => values[0],
            _ => "متعدد: " + string.Join("، ", values)
        };
    }

    private static string DescribePaymentMethod(PaymentMethod? method) => method switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.Card => "بطاقة",
        PaymentMethod.BankTransfer => "تحويل بنكي",
        PaymentMethod.Cheque => "شيك",
        PaymentMethod.Other => "أخرى",
        _ => string.Empty
    };

    private static string DescribePartyType(SettlementPartyType? value) => value switch
    {
        SettlementPartyType.Customer => "عميل",
        SettlementPartyType.Supplier => "مورد",
        SettlementPartyType.Employee => "موظف",
        SettlementPartyType.Other => "أخرى",
        _ => string.Empty
    };

    private static string DescribeReceiptStatus(ReceiptVoucherStatus status) => status switch
    {
        ReceiptVoucherStatus.Draft => "مسودة",
        ReceiptVoucherStatus.Approved => "معتمد",
        ReceiptVoucherStatus.Posted => "مرحل",
        ReceiptVoucherStatus.Cancelled => "ملغي",
        _ => status.ToString()
    };

    private static string DescribePaymentStatus(PaymentVoucherStatus status) => status switch
    {
        PaymentVoucherStatus.Draft => "مسودة",
        PaymentVoucherStatus.Approved => "معتمد",
        PaymentVoucherStatus.Posted => "مرحل",
        PaymentVoucherStatus.Cancelled => "ملغي",
        _ => status.ToString()
    };
}
