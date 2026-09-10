using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class JobTitle : AuditableEntity<Guid>
{
    private JobTitle() { }

    private JobTitle(Guid id, string name, bool isActive)
    {
        if (id == Guid.Empty) throw new DomainException("Job title id is required.");
        Id = id;
        Rename(name);
        IsActive = isActive;
    }

    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static JobTitle Create(Guid id, string name, bool isActive = true) =>
        new(id, name, isActive);

    public void Update(string name, bool isActive)
    {
        Rename(name);
        IsActive = isActive;
    }

    private void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Job title name is required.");

        name = name.Trim();
        if (name.Length > 100)
            throw new DomainException("Job title name cannot exceed 100 characters.");

        Name = name;
    }
}
