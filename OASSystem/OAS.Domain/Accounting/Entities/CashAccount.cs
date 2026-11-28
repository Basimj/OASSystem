using OAS.Domain.Common.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Domain.Accounting.Entities;

public sealed class CashAccount : AuditableEntity<Guid>
{
    private CashAccount()
    {
    }

    private CashAccount(
        Guid id,
        string code,
        string name,
        Guid accountId,
        Guid? currencyId,
        bool isDefault,
        bool isActive)
    {
        Id = id;
        Code = code;
        Name = name;
        AccountId = accountId;
        CurrencyId = currencyId;
        IsDefault = isDefault;
        IsActive = isActive;
        CashBoxId = id;
        CashBox = new CashBox(id, code, name);
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public Guid CashBoxId { get; private set; }
    public CashBox? CashBox { get; private set; }

    // Only a newly created currency account may join an existing box.
    public void JoinCashBox(Guid cashBoxId)
    {
        if (cashBoxId == Guid.Empty) throw new ArgumentException("Cash box id is required.", nameof(cashBoxId));
        if (CashBoxId != Id || RowVersion.Length != 0 || CashBox is null)
            throw new InvalidOperationException("Only a new cash account can join a cash box.");
        CashBoxId = cashBoxId;
        CashBox = null;
    }

    public Guid AccountId { get; private set; }

    public Guid? CurrencyId { get; private set; }

    public CashAccountMovementType MovementType { get; private set; } = CashAccountMovementType.ReceiptAndPayment;

    public bool AllowsReceipt => MovementType is CashAccountMovementType.ReceiptOnly or CashAccountMovementType.ReceiptAndPayment;
    public bool AllowsPayment => MovementType is CashAccountMovementType.PaymentOnly or CashAccountMovementType.ReceiptAndPayment;

    public void SetMovementType(CashAccountMovementType value)
    {
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
        MovementType = value;
    }

    public bool RequiresOpenShift { get; private set; }

    public void SetRequiresOpenShift(bool value) => RequiresOpenShift = value;

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    // Compatibility overload for pre-currency tests/data factories.
    public static CashAccount Create(
        Guid id,
        string code,
        string name,
        Guid accountId,
        bool isDefault,
        bool isActive) =>
        Create(id, code, name, accountId, null, isDefault, isActive);

    public static CashAccount Create(
        Guid id,
        string code,
        string name,
        Guid accountId,
        Guid? currencyId,
        bool isDefault,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        return new CashAccount(
            id,
            code.Trim(),
            name.Trim(),
            accountId,
            currencyId,
            isDefault,
            isActive);
    }

    public void UpdateDetails(
        string code,
        string name,
        Guid accountId,
        Guid? currencyId,
        bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        Code = code.Trim();
        Name = name.Trim();
        if (CashBoxId == Id) CashBox?.Rename(Name);
        AccountId = accountId;
        CurrencyId = currencyId;
        IsDefault = isDefault;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
