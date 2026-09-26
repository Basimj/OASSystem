using NUnit.Framework;
using OAS.Application.Accounting.MultiCurrency;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Tests.Accounting.Domain;

[TestFixture]
public sealed class MultiCurrencyDomainTests
{
    [Test]
    public void Currency_Normalizes_Code_And_Stores_Precision()
    {
        var currency = Currency.Create(
            Guid.NewGuid(),
            " usd ",
            "دولار",
            "US Dollar",
            "$",
            2,
            true);

        Assert.That(currency.Code, Is.EqualTo("USD"));
        Assert.That(currency.DecimalPlaces, Is.EqualTo((byte)2));
        Assert.That(currency.IsActive, Is.True);
    }

    [Test]
    public void ExchangeRate_Rejects_Non_Positive_Rate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExchangeRate.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 9, 26),
                0m));
    }

    [Test]
    public void Rounding_Calculates_Base_Amount_From_Rounded_Transaction_Amount()
    {
        var service = new CurrencyRoundingService();

        var firstResult = service.CalculateBaseAmount(
            100m,
            550m,
            2,
            2);

        var secondResult = service.CalculateBaseAmount(
            12.345m,
            5.5m,
            2,
            2);

        Assert.That(
            firstResult,
            Is.EqualTo(55_000m));

        Assert.That(
            secondResult,
            Is.EqualTo(67.93m));
    }

    [Test]
    public void Settlement_Line_Preserves_Party_Currency_And_Account_Snapshots()
    {
        var customerId = Guid.NewGuid();
        var counterpartyAccountId = Guid.NewGuid();
        var settlementAccountId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var cashAccountId = Guid.NewGuid();

        var line = ReceiptVoucherLine.CreateSettlement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            SettlementPartyType.Customer,
            customerId,
            null,
            null,
            "عميل اختبار",
            counterpartyAccountId,
            PaymentMethod.Cash,
            cashAccountId,
            null,
            settlementAccountId,
            currencyId,
            "USD",
            "$",
            2,
            100m,
            550m,
            new DateOnly(2026, 9, 26),
            ExchangeRateType.Accounting,
            ExchangeRateSource.System,
            55_000m,
            null,
            null,
            null,
            null,
            "تحصيل");

        Assert.That(
            line.CustomerId,
            Is.EqualTo(customerId));

        Assert.That(
            line.CounterpartyAccountId,
            Is.EqualTo(counterpartyAccountId));

        Assert.That(
            line.SettlementAccountId,
            Is.EqualTo(settlementAccountId));

        Assert.That(
            line.CurrencyId,
            Is.EqualTo(currencyId));

        Assert.That(
            line.BaseAmount,
            Is.EqualTo(55_000m));
    }
}