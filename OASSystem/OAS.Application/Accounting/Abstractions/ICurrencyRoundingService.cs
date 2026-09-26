namespace OAS.Application.Accounting.Abstractions;

public interface ICurrencyRoundingService
{
    decimal Round(decimal amount, byte decimalPlaces);
    decimal CalculateBaseAmount(decimal transactionAmount, decimal exchangeRate, byte transactionDecimalPlaces, byte baseDecimalPlaces);
}
