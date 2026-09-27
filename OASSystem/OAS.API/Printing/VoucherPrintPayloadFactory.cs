using MediatR;
using OAS.Application.Accounting.PaymentVouchers.Queries.GetPaymentVoucherById;
using OAS.Application.Accounting.ReceiptVouchers.Queries.GetReceiptVoucherById;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.PaymentVouchers;
using OAS.Contracts.Accounting.ReceiptVouchers;
using OAS.Contracts.Printing;

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
                BuildReceipt(await sender.Send(
                    new GetReceiptVoucherByIdQuery(documentId),
                    cancellationToken)),

            PrintDocumentTypes.PaymentVoucher =>
                BuildPayment(await sender.Send(
                    new GetPaymentVoucherByIdQuery(documentId),
                    cancellationToken)),

            _ => throw new ArgumentOutOfRangeException(
                nameof(documentType),
                documentType,
                "نوع المستند غير مدعوم للطباعة.")
        };
    }

    private object BuildReceipt(ReceiptVoucherDto voucher)
    {
        var total = ResolveReceiptTotal(voucher);
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

        return new
        {
            voucher.VoucherNumber,
            VoucherDate = voucher.VoucherDate.ToString("yyyy-MM-dd"),
            CustomerName = customerName ?? string.Empty,
            ReceivedFrom = partyNames.Length > 0 ? string.Join("، ", partyNames) : string.Empty,
            PaymentMethod = DescribePaymentMethods(voucher.Lines.Select(x => x.PaymentMethod)),
            CashAccountName = voucher.Lines.Any(x => x.CashAccountId.HasValue) ? "الصندوق" : string.Empty,
            BankAccountName = voucher.Lines.Any(x => x.BankAccountId.HasValue) ? "الحساب البنكي" : string.Empty,
            TotalAmount = total,
            TotalAmountWords = FormatAmountWords(total, voucher.BaseCurrencyCodeSnapshot),
            voucher.Description,
            Status = voucher.Status.ToString(),
            Company = Company(),
            Lines = voucher.Lines.Select(MapReceiptLine).ToArray()
        };
    }

    private object BuildPayment(PaymentVoucherDto voucher)
    {
        var total = ResolvePaymentTotal(voucher);
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

        return new
        {
            voucher.VoucherNumber,
            VoucherDate = voucher.VoucherDate.ToString("yyyy-MM-dd"),
            SupplierName = supplierName ?? string.Empty,
            BeneficiaryName = partyNames.Length > 0 ? string.Join("، ", partyNames) : string.Empty,
            PaymentMethod = DescribePaymentMethods(voucher.Lines.Select(x => x.PaymentMethod)),
            CashAccountName = voucher.Lines.Any(x => x.CashAccountId.HasValue) ? "الصندوق" : string.Empty,
            BankAccountName = voucher.Lines.Any(x => x.BankAccountId.HasValue) ? "الحساب البنكي" : string.Empty,
            TotalAmount = total,
            TotalAmountWords = FormatAmountWords(total, voucher.BaseCurrencyCodeSnapshot),
            voucher.Description,
            Status = voucher.Status.ToString(),
            Company = Company(),
            Lines = voucher.Lines.Select(MapPaymentLine).ToArray()
        };
    }

    private static object MapReceiptLine(ReceiptVoucherLineDto line) => new
    {
        line.LineNumber,
        AccountName = ResolveLineName(line.PartyNameSnapshot, line.ReferenceType, line.AccountId),
        PartyName = line.PartyNameSnapshot ?? string.Empty,
        PaymentMethod = DescribePaymentMethod(line.PaymentMethod),
        CurrencyCode = line.CurrencyCodeSnapshot ?? string.Empty,
        line.Amount,
        BaseAmount = line.BaseAmount,
        line.ReferenceNumber,
        ReferenceDate = line.ReferenceDate?.ToString("yyyy-MM-dd"),
        line.Description
    };

    private static object MapPaymentLine(PaymentVoucherLineDto line) => new
    {
        line.LineNumber,
        AccountName = ResolveLineName(line.PartyNameSnapshot, line.ReferenceType, line.AccountId),
        PartyName = line.PartyNameSnapshot ?? string.Empty,
        PaymentMethod = DescribePaymentMethod(line.PaymentMethod),
        CurrencyCode = line.CurrencyCodeSnapshot ?? string.Empty,
        line.Amount,
        BaseAmount = line.BaseAmount,
        line.ReferenceNumber,
        ReferenceDate = line.ReferenceDate?.ToString("yyyy-MM-dd"),
        line.Description
    };

    private object Company() => new
    {
        Name = configuration["Application:Name"] ?? "OAS",
        Phone = configuration["Application:Phone"] ?? string.Empty
    };

    private static decimal ResolveReceiptTotal(ReceiptVoucherDto voucher) =>
        voucher.BaseTotalAmount ?? voucher.Lines.Sum(x => x.BaseAmount ?? x.Amount);

    private static decimal ResolvePaymentTotal(PaymentVoucherDto voucher) =>
        voucher.BaseTotalAmount ?? voucher.Lines.Sum(x => x.BaseAmount ?? x.Amount);

    private static string ResolveLineName(string? partyName, string? referenceType, Guid accountId)
    {
        if (!string.IsNullOrWhiteSpace(partyName))
            return partyName.Trim();

        if (!string.IsNullOrWhiteSpace(referenceType))
            return referenceType.Trim();

        return $"حساب {accountId.ToString("N")[..8]}";
    }

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

    private static string FormatAmountWords(decimal amount, string? currencyCode)
    {
        var currency = string.IsNullOrWhiteSpace(currencyCode) ? string.Empty : $" {currencyCode}";
        return $"فقط {amount:N2}{currency} لا غير";
    }
}
