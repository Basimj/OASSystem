using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class SupplierAccount : AuditableEntity<Guid>
{
    private SupplierAccount()
    {
    }

    private SupplierAccount(
        Guid id,
        Guid supplierId,
        Guid accountId,
        Guid controlAccountId,
        bool isActive)
    {
        Id = id;
        SupplierId = supplierId;
        AccountId = accountId;
        ControlAccountId = controlAccountId;
        IsActive = isActive;
    }

    public Guid SupplierId { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid ControlAccountId { get; private set; }

    public bool IsActive { get; private set; }


    public byte[] RowVersion { get; private set; } = [];

    public static SupplierAccount Create(
        Guid id,
        Guid supplierId,
        Guid accountId,
        Guid controlAccountId,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (supplierId == Guid.Empty)
            throw new ArgumentException("Supplier id is required.", nameof(supplierId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        if (controlAccountId == Guid.Empty)
            throw new ArgumentException(
                "Control account id is required.",
                nameof(controlAccountId));

        return new SupplierAccount(
            id,
            supplierId,
            accountId,
            controlAccountId,
            isActive);
    }

    public void UpdateAccounts(
        Guid accountId,
        Guid controlAccountId)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        if (controlAccountId == Guid.Empty)
            throw new ArgumentException(
                "Control account id is required.",
                nameof(controlAccountId));

        AccountId = accountId;
        ControlAccountId = controlAccountId;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
