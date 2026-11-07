namespace OAS.UiLib.Core.Models.Purchasing;

/// <summary>
/// Non-posting, four-decimal UI estimate; persisted totals are calculated by the domain.
/// </summary>
public static class UiPurchaseOrderAmountPreview
{
    public sealed record Totals(decimal Subtotal, decimal Discount, decimal Tax, decimal Total);

    public static Totals FromModel(UiPurchaseOrderEditorModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        decimal subtotal = 0m, discount = 0m, tax = 0m, total = 0m;
        foreach (var line in model.Lines)
        {
            var gross = Round(line.OrderedQuantity * line.UnitPrice);
            var lineDiscount = Round(line.DiscountAmount);
            var net = Round(Math.Max(0m, gross - lineDiscount));
            var lineTax = line.TaxRate <= 0m ? 0m : model.TaxCalculationMode == 1
                ? Round(net * line.TaxRate / 100m)
                : Round(net - net / (1m + line.TaxRate / 100m));
            subtotal += gross;
            discount += lineDiscount;
            tax += lineTax;
            total += model.TaxCalculationMode == 1 ? Round(net + lineTax) : net;
        }
        return new(Round(subtotal), Round(discount), Round(tax), Round(total));
    }

    private static decimal Round(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
