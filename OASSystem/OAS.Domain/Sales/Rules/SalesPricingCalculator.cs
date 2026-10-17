using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Domain.Sales.Rules;

public static class SalesPricingCalculator
{
    public static SalesLineAmounts Calculate(
        decimal quantity,
        decimal actualUnitPrice,
        SalesDiscountType discountType,
        decimal? discountValue,
        decimal? taxRate,
        TaxCalculationMode taxCalculationMode,
        byte currencyDecimalPlaces)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (actualUnitPrice < 0)
            throw new DomainException("Actual unit price cannot be negative.");
        if (currencyDecimalPlaces > 6)
            throw new DomainException("Currency decimal places cannot exceed 6.");
        if (!Enum.IsDefined(discountType))
            throw new DomainException("Discount type is invalid.");
        if (!Enum.IsDefined(taxCalculationMode))
            throw new DomainException("Tax calculation mode is invalid.");
        if (taxRate is < 0)
            throw new DomainException("Tax rate cannot be negative.");

        var gross = Round(quantity * actualUnitPrice, currencyDecimalPlaces);
        var discount = CalculateDiscount(gross, discountType, discountValue, currencyDecimalPlaces);
        if (discount < 0 || discount > gross)
            throw new DomainException("Discount amount must be between zero and the gross amount.");

        var rate = taxRate ?? 0m;
        if (taxCalculationMode == TaxCalculationMode.Exclusive)
        {
            var net = Round(gross - discount, currencyDecimalPlaces);
            var tax = Round(net * rate / 100m, currencyDecimalPlaces);
            return new SalesLineAmounts(gross, discount, net, tax, Round(net + tax, currencyDecimalPlaces));
        }

        var finalInclusive = Round(gross - discount, currencyDecimalPlaces);
        var taxInclusive = rate == 0m ? 0m : Round(finalInclusive - finalInclusive / (1m + rate / 100m), currencyDecimalPlaces);
        return new SalesLineAmounts(gross, discount, Round(finalInclusive - taxInclusive, currencyDecimalPlaces), taxInclusive, finalInclusive);
    }

    public static IReadOnlyList<decimal> AllocateHeaderDiscount(
        IReadOnlyList<decimal> eligibleAmounts,
        SalesDiscountType discountType,
        decimal? discountValue,
        byte currencyDecimalPlaces)
    {
        if (eligibleAmounts is null) throw new ArgumentNullException(nameof(eligibleAmounts));
        if (eligibleAmounts.Any(x => x < 0m)) throw new DomainException("Header discount eligible amounts cannot be negative.");
        if (discountType == SalesDiscountType.None || eligibleAmounts.Count == 0)
            return Enumerable.Repeat(0m, eligibleAmounts.Count).ToArray();

        var total = Round(eligibleAmounts.Sum(), currencyDecimalPlaces);
        if (total <= 0m) throw new DomainException("Header discount requires a positive eligible amount.");
        var discount = CalculateDiscount(total, discountType, discountValue, currencyDecimalPlaces);
        if (discount > total) throw new DomainException("Header discount cannot exceed the eligible amount.");

        var result = new decimal[eligibleAmounts.Count];
        var positive = eligibleAmounts.Select((amount, index) => (amount, index)).Where(x => x.amount > 0m).ToArray();
        decimal allocated = 0m;
        for (var i = 0; i < positive.Length; i++)
        {
            var item = positive[i];
            var value = i == positive.Length - 1
                ? Round(discount - allocated, currencyDecimalPlaces)
                : Round(discount * item.amount / total, currencyDecimalPlaces);
            result[item.index] = value;
            allocated = Round(allocated + value, currencyDecimalPlaces);
        }
        return result;
    }

    public static decimal ConvertToBase(decimal transactionAmount, decimal exchangeRate, byte baseCurrencyDecimalPlaces)
    {
        if (transactionAmount < 0) throw new DomainException("Transaction amount cannot be negative.");
        if (exchangeRate <= 0) throw new DomainException("Exchange rate must be greater than zero.");
        if (baseCurrencyDecimalPlaces > 6) throw new DomainException("Base currency decimal places cannot exceed 6.");
        return Round(transactionAmount * exchangeRate, baseCurrencyDecimalPlaces);
    }

    public static decimal ConvertFromBase(decimal baseAmount, decimal exchangeRate, byte transactionCurrencyDecimalPlaces)
    {
        if (baseAmount < 0) throw new DomainException("Base amount cannot be negative.");
        if (exchangeRate <= 0) throw new DomainException("Exchange rate must be greater than zero.");
        if (transactionCurrencyDecimalPlaces > 6) throw new DomainException("Transaction currency decimal places cannot exceed 6.");
        return Round(baseAmount / exchangeRate, transactionCurrencyDecimalPlaces);
    }

    public static decimal ConvertBetweenCurrencies(decimal amount, decimal fromExchangeRate, decimal toExchangeRate, byte targetCurrencyDecimalPlaces)
    {
        if (amount < 0) throw new DomainException("Amount cannot be negative.");
        if (fromExchangeRate <= 0 || toExchangeRate <= 0) throw new DomainException("Exchange rate must be greater than zero.");
        if (targetCurrencyDecimalPlaces > 6) throw new DomainException("Transaction currency decimal places cannot exceed 6.");
        return Round(amount * fromExchangeRate / toExchangeRate, targetCurrencyDecimalPlaces);
    }

    public static decimal Round(decimal value, byte decimalPlaces) => Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);

    private static decimal CalculateDiscount(decimal gross, SalesDiscountType discountType, decimal? discountValue, byte currencyDecimalPlaces)
    {
        if (discountType == SalesDiscountType.None) return 0m;
        if (!discountValue.HasValue || discountValue.Value < 0) throw new DomainException("Discount value is required and cannot be negative.");
        return discountType switch
        {
            SalesDiscountType.Percentage when discountValue.Value <= 100m => Round(gross * discountValue.Value / 100m, currencyDecimalPlaces),
            SalesDiscountType.Percentage => throw new DomainException("Percentage discount cannot exceed 100."),
            SalesDiscountType.FixedAmount => Round(discountValue.Value, currencyDecimalPlaces),
            _ => throw new DomainException("Discount type is invalid.")
        };
    }
}
