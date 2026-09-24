using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class CustomerAccount : AuditableEntity<Guid>
{
    private CustomerAccount()
    {
    }

    private CustomerAccount(
        Guid id,
        Guid customerId,
        Guid accountId,
        Guid controlAccountId,
        bool isActive)
    {
        Id = id;
        CustomerId = customerId;
        AccountId = accountId;
        ControlAccountId = controlAccountId;
        IsActive = isActive;
    }

    public Guid CustomerId { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid ControlAccountId { get; private set; }

    public bool IsActive { get; private set; }


    public byte[] RowVersion { get; private set; } = [];

    public static CustomerAccount Create(
        Guid id,
        Guid customerId,
        Guid accountId,
        Guid controlAccountId,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(customerId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        if (controlAccountId == Guid.Empty)
            throw new ArgumentException(
                "Control account id is required.",
                nameof(controlAccountId));

        return new CustomerAccount(
            id,
            customerId,
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
