using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class EmployeeAccount : AuditableEntity<Guid>
{
    private EmployeeAccount() { }
    private EmployeeAccount(Guid id, Guid employeeId, Guid accountId, bool isActive)
    {
        Id = id;
        EmployeeId = employeeId;
        AccountId = accountId;
        IsActive = isActive;
    }

    public Guid EmployeeId { get; private set; }
    public Guid AccountId { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static EmployeeAccount Create(Guid id, Guid employeeId, Guid accountId, bool isActive = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Employee account id is required.", nameof(id));
        if (employeeId == Guid.Empty) throw new ArgumentException("Employee id is required.", nameof(employeeId));
        if (accountId == Guid.Empty) throw new ArgumentException("Account id is required.", nameof(accountId));
        return new EmployeeAccount(id, employeeId, accountId, isActive);
    }

    public void SetActive(bool active) => IsActive = active;
}
