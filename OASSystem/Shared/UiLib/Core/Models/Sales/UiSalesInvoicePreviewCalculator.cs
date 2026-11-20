namespace OAS.UiLib.Core.Models.Sales;

/// <summary>
/// UI-only preview of a new sales invoice. The authoritative posted totals
/// always come from the Domain sales pricing rules on the API.
/// </summary>
public static class UiSalesInvoicePreviewCalculator
{
    public static bool TryRecalculate(UiSalesInvoiceFormModel invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        if (invoice.Id.HasValue || invoice.CurrencyDecimalPlaces > 6)
            return false; // do not overwrite persisted amounts/approved header discounts

        var decimals = (int)invoice.CurrencyDecimalPlaces;
        var inclusive = invoice.TaxCalculationMode == "Inclusive";
        if (invoice.TaxCalculationMode is not ("Inclusive" or "Exclusive")) return false;
        var calculated = new List<(UiSalesLineModel Line, decimal Gross, decimal Discount, decimal Net, decimal Tax, decimal Total)>();
        foreach (var line in invoice.Lines)
        {
            if (line.Quantity <= 0m || line.ActualUnitPrice < 0m || line.TaxRate is < 0m or > 100m)
                return false;
            var gross = Round(line.Quantity * line.ActualUnitPrice, decimals);
            var discount = line.DiscountType switch
            {
                "None" => 0m,
                "Percentage" when line.DiscountValue is >= 0m and <= 100m => Round(gross * line.DiscountValue.Value / 100m, decimals),
                "FixedAmount" when line.DiscountValue is >= 0m => Round(line.DiscountValue.Value, decimals),
                _ => -1m
            };
            if (discount < 0m || discount > gross) return false;
            var afterDiscount = Round(gross - discount, decimals);
            var rate = line.TaxRate ?? 0m;
            var tax = inclusive
                ? (rate == 0m ? 0m : Round(afterDiscount - afterDiscount / (1m + rate / 100m), decimals))
                : Round(afterDiscount * rate / 100m, decimals);
            var net = inclusive ? Round(afterDiscount - tax, decimals) : afterDiscount;
            var final = inclusive ? afterDiscount : Round(net + tax, decimals);
            calculated.Add((line, gross, discount, net, tax, final));
        }

        // Publish only after every line passed validation, keeping the last valid
        // preview visible while the editor shows an inline invalid-input error.
        foreach (var entry in calculated)
        {
            entry.Line.DiscountAmount = entry.Discount;
            entry.Line.NetAmount = entry.Net;
            entry.Line.TaxAmount = entry.Tax;
            entry.Line.FinalAmount = entry.Total;
        }
        invoice.Subtotal = Round(calculated.Sum(x => x.Gross), decimals);
        invoice.DiscountAmount = Round(calculated.Sum(x => x.Discount), decimals);
        invoice.TaxAmount = Round(calculated.Sum(x => x.Tax), decimals);
        invoice.TotalAmount = Round(calculated.Sum(x => x.Total), decimals);
        return true;
    }

    private static decimal Round(decimal value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
