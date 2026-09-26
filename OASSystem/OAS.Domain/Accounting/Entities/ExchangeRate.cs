using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class ExchangeRate : AuditableEntity<Guid>
{
    private ExchangeRate() { }
    private ExchangeRate(Guid id, Guid currencyId, DateOnly rateDate, decimal rate, ExchangeRateType rateType, bool isActive)
    {
        Id = id;
        CurrencyId = currencyId;
        RateDate = rateDate;
        SetRate(rate);
        RateType = rateType;
        IsActive = isActive;
    }

    public Guid CurrencyId { get; private set; }
    public DateOnly RateDate { get; private set; }
    public decimal Rate { get; private set; }
    public ExchangeRateType RateType { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static ExchangeRate Create(Guid id, Guid currencyId, DateOnly rateDate, decimal rate, ExchangeRateType rateType = ExchangeRateType.Accounting, bool isActive = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Exchange rate id is required.", nameof(id));
        if (currencyId == Guid.Empty) throw new ArgumentException("Currency id is required.", nameof(currencyId));
        return new ExchangeRate(id, currencyId, rateDate, rate, rateType, isActive);
    }

    public void Update(DateOnly rateDate, decimal rate, ExchangeRateType rateType, bool isActive)
    {
        RateDate = rateDate;
        SetRate(rate);
        RateType = rateType;
        IsActive = isActive;
    }

    private void SetRate(decimal rate)
    {
        if (rate <= 0) throw new ArgumentOutOfRangeException(nameof(rate), "Exchange rate must be greater than zero.");
        Rate = rate;
    }
}
