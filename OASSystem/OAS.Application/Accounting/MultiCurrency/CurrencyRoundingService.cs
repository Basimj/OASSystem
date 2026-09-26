using OAS.Application.Accounting.Abstractions;

namespace OAS.Application.Accounting.MultiCurrency;

public sealed class CurrencyRoundingService : ICurrencyRoundingService
{
    public decimal Round(decimal amount, byte decimalPlaces)
        => Math.Round(amount, decimalPlaces, MidpointRounding.AwayFromZero);

    public decimal CalculateBaseAmount(decimal transactionAmount, decimal exchangeRate, byte transactionDecimalPlaces, byte baseDecimalPlaces)
    {
        if (exchangeRate <= 0) throw new ArgumentOutOfRangeException(nameof(exchangeRate));
        var transaction = Round(transactionAmount, transactionDecimalPlaces);
        return Round(transaction * exchangeRate, baseDecimalPlaces);
    }
}
