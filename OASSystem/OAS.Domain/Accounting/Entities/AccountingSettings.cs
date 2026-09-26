using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class AccountingSettings : AuditableEntity<Guid>
{
    public static readonly Guid SingletonId = Guid.Parse("9d171de8-4a54-49f8-8f0d-01b942b7795d");

    private AccountingSettings() { }
    private AccountingSettings(Guid baseCurrencyId, Guid? employeeParentAccountId, Guid? cashParentAccountId, Guid? bankParentAccountId, Guid? exchangeGainAccountId, Guid? exchangeLossAccountId, ExchangeRateType defaultExchangeRateType)
    {
        Id = SingletonId;
        Apply(baseCurrencyId, employeeParentAccountId, cashParentAccountId, bankParentAccountId, exchangeGainAccountId, exchangeLossAccountId, defaultExchangeRateType);
    }

    public Guid BaseCurrencyId { get; private set; }
    public Guid? EmployeeParentAccountId { get; private set; }
    public Guid? CashParentAccountId { get; private set; }
    public Guid? BankParentAccountId { get; private set; }
    public Guid? ExchangeGainAccountId { get; private set; }
    public Guid? ExchangeLossAccountId { get; private set; }
    public ExchangeRateType DefaultExchangeRateType { get; private set; } = ExchangeRateType.Accounting;
    public byte[] RowVersion { get; private set; } = [];

    public static AccountingSettings Create(Guid baseCurrencyId, Guid? employeeParentAccountId = null, Guid? cashParentAccountId = null, Guid? bankParentAccountId = null, Guid? exchangeGainAccountId = null, Guid? exchangeLossAccountId = null, ExchangeRateType defaultExchangeRateType = ExchangeRateType.Accounting)
        => new(baseCurrencyId, employeeParentAccountId, cashParentAccountId, bankParentAccountId, exchangeGainAccountId, exchangeLossAccountId, defaultExchangeRateType);

    public void Update(Guid baseCurrencyId, Guid? employeeParentAccountId, Guid? cashParentAccountId, Guid? bankParentAccountId, Guid? exchangeGainAccountId, Guid? exchangeLossAccountId, ExchangeRateType defaultExchangeRateType)
        => Apply(baseCurrencyId, employeeParentAccountId, cashParentAccountId, bankParentAccountId, exchangeGainAccountId, exchangeLossAccountId, defaultExchangeRateType);

    private void Apply(Guid baseCurrencyId, Guid? employeeParentAccountId, Guid? cashParentAccountId, Guid? bankParentAccountId, Guid? exchangeGainAccountId, Guid? exchangeLossAccountId, ExchangeRateType defaultExchangeRateType)
    {
        if (baseCurrencyId == Guid.Empty) throw new ArgumentException("Base currency is required.", nameof(baseCurrencyId));
        BaseCurrencyId = baseCurrencyId;
        EmployeeParentAccountId = Normalize(employeeParentAccountId);
        CashParentAccountId = Normalize(cashParentAccountId);
        BankParentAccountId = Normalize(bankParentAccountId);
        ExchangeGainAccountId = Normalize(exchangeGainAccountId);
        ExchangeLossAccountId = Normalize(exchangeLossAccountId);
        DefaultExchangeRateType = defaultExchangeRateType;
    }

    private static Guid? Normalize(Guid? value) => value is { } id && id != Guid.Empty ? id : null;
}
